
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.DTOs;
using SchoolAdmission.Api.Models;
using SchoolAdmission.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<AdmissionService>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<HoldExpirationService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseCors("Frontend");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options => options.SerializeAsV2 = true);
    app.UseSwaggerUI(options =>
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SchoolAdmission.Api v1"));
}

static string PublicStatus(AdmissionHold hold)
{
    if (hold.Status == "Confirmed" || hold.Status == "CONFIRMED")
        return "CONFIRMED";

    if (hold.Status == "Cancelled" || hold.Status == "CANCELLED")
        return "CANCELLED";

    if (hold.Status == "Expired" || hold.Status == "EXPIRED" ||
        hold.ExpiresAtUtc <= DateTime.UtcNow)
        return "EXPIRED";

    return "HELD";
}

app.MapPost("/classes", async (CreateClassRequest request, AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || request.TotalSeats <= 0)
        return Results.BadRequest("Invalid class information.");

    var schoolClass = new SchoolClass
    {
        Name = request.Name.Trim(),
        Shift = request.Shift?.Trim() ?? "",
        TotalSeats = request.TotalSeats
    };

    db.SchoolClasses.Add(schoolClass);
    await db.SaveChangesAsync();

    return Results.Created($"/classes/{schoolClass.Id}", new
    {
        id = schoolClass.Id,
        name = schoolClass.Name,
        totalSeats = schoolClass.TotalSeats
    });
});

app.MapGet("/classes", async (AppDbContext db) =>
{
    var classes = await db.SchoolClasses.OrderBy(c => c.Id).ToListAsync();
    var now = DateTime.UtcNow;
    var result = new List<object>();

    foreach (var c in classes)
    {
        var held = await db.AdmissionHolds.CountAsync(h =>
            h.SchoolClassId == c.Id &&
            (h.Status == "HELD" || h.Status == "Active") &&
            h.ExpiresAtUtc > now);

        var confirmed = await db.AdmissionHolds.CountAsync(h =>
            h.SchoolClassId == c.Id &&
            (h.Status == "CONFIRMED" || h.Status == "Confirmed"));

        result.Add(new
        {
            id = c.Id,
            name = c.Name,
            totalSeats = c.TotalSeats,
            heldSeats = held,
            confirmedSeats = confirmed,
            availableSeats = Math.Max(0, c.TotalSeats - held - confirmed)
        });
    }

    return Results.Ok(result);
});

app.MapGet("/classes/{classId:int}", async (int classId, AppDbContext db) =>
{
    var c = await db.SchoolClasses.FindAsync(classId);

    if (c == null)
        return Results.NotFound();

    var now = DateTime.UtcNow;

    var held = await db.AdmissionHolds.CountAsync(h =>
        h.SchoolClassId == c.Id &&
        (h.Status == "HELD" || h.Status == "Active") &&
        h.ExpiresAtUtc > now);

    var confirmed = await db.AdmissionHolds.CountAsync(h =>
        h.SchoolClassId == c.Id &&
        (h.Status == "CONFIRMED" || h.Status == "Confirmed"));

    return Results.Ok(new
    {
        id = c.Id,
        name = c.Name,
        totalSeats = c.TotalSeats,
        heldSeats = held,
        confirmedSeats = confirmed,
        availableSeats = Math.Max(0, c.TotalSeats - held - confirmed)
    });
});

app.MapPost("/classes/{classId:int}/holds", async (
    int classId,
    [FromHeader(Name = "X-Student-Id")] string? studentId,
    AdmissionService service,
    AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(studentId) ||
        studentId.Length != 17 ||
        !studentId.All(char.IsDigit))
        return Results.BadRequest("A valid 17-digit X-Student-Id header is required.");

    if (!await db.SchoolClasses.AnyAsync(c => c.Id == classId))
        return Results.NotFound("Class not found.");

    var hold = await service.CreateHoldAsync(classId, studentId);

    if (hold == null)
        return Results.Conflict("No seats available or an application already exists.");

    return Results.Created($"/holds/{hold.Id}", new
    {
        holdId = hold.Id,
        classId = hold.SchoolClassId,
        studentId = hold.StudentId,
        status = "HELD",
        expiresAt = hold.ExpiresAtUtc
    });
});

app.MapGet("/holds/{holdId:guid}", async (Guid holdId, AppDbContext db) =>
{
    var hold = await db.AdmissionHolds.FindAsync(holdId);

    if (hold == null)
        return Results.NotFound();

    if ((hold.Status == "Active" || hold.Status == "HELD") &&
        hold.ExpiresAtUtc <= DateTime.UtcNow)
    {
        hold.Status = "EXPIRED";
        await db.SaveChangesAsync();
    }

    return Results.Ok(new
    {
        holdId = hold.Id,
        classId = hold.SchoolClassId,
        status = PublicStatus(hold),
        expiresAt = hold.ExpiresAtUtc
    });
});

app.MapGet("/me/holds", async (
    [FromHeader(Name = "X-Student-Id")] string? studentId,
    AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(studentId) ||
        studentId.Length != 17 ||
        !studentId.All(char.IsDigit))
        return Results.BadRequest("A valid 17-digit X-Student-Id header is required.");

    var holds = await db.AdmissionHolds
        .Include(h => h.SchoolClass)
        .Where(h => h.StudentId == studentId)
        .OrderByDescending(h => h.CreatedAtUtc)
        .ToListAsync();

    return Results.Ok(holds.Select(h => new
    {
        holdId = h.Id,
        classId = h.SchoolClassId,
        className = h.SchoolClass!.Name,
        status = PublicStatus(h),
        expiresAt = h.ExpiresAtUtc
    }));
});

app.MapPost("/holds/{holdId:guid}/confirm", async (
    Guid holdId,
    [FromHeader(Name = "X-Student-Id")] string? studentId,
    AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(studentId) ||
        studentId.Length != 17 ||
        !studentId.All(char.IsDigit))
        return Results.BadRequest("A valid 17-digit X-Student-Id header is required.");

    var info = await db.AdmissionHolds
        .Where(h => h.Id == holdId)
        .Select(h => new { h.SchoolClassId })
        .FirstOrDefaultAsync();

    if (info == null)
        return Results.NotFound("Hold not found.");

    await using var transaction = await db.Database.BeginTransactionAsync();

    await db.Database.ExecuteSqlInterpolatedAsync(
        $"SELECT pg_advisory_xact_lock({info.SchoolClassId})");

    var hold = await db.AdmissionHolds.FirstOrDefaultAsync(h => h.Id == holdId);

    if (hold == null)
        return Results.NotFound("Hold not found.");

    if (hold.StudentId != studentId)
        return Results.Forbid();

    if (hold.Status == "CONFIRMED" || hold.Status == "Confirmed")
    {
        await transaction.CommitAsync();
        return Results.Ok(new { holdId = hold.Id, status = "CONFIRMED" });
    }

    if ((hold.Status != "HELD" && hold.Status != "Active") ||
        hold.ExpiresAtUtc <= DateTime.UtcNow)
    {
        hold.Status = "EXPIRED";
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return Results.Conflict("Hold has expired or is no longer active.");
    }

    hold.Status = "CONFIRMED";
    hold.ConfirmedAtUtc = DateTime.UtcNow;

    await db.SaveChangesAsync();
    await transaction.CommitAsync();

    return Results.Ok(new { holdId = hold.Id, status = "CONFIRMED" });
});

app.MapDelete("/holds/{holdId:guid}", async (
    Guid holdId,
    [FromHeader(Name = "X-Student-Id")] string? studentId,
    AppDbContext db) =>
{
    if (string.IsNullOrWhiteSpace(studentId) ||
        studentId.Length != 17 ||
        !studentId.All(char.IsDigit))
        return Results.BadRequest("A valid 17-digit X-Student-Id header is required.");

    var hold = await db.AdmissionHolds.FirstOrDefaultAsync(h => h.Id == holdId);

    if (hold == null)
        return Results.NotFound();

    if (hold.StudentId != studentId)
        return Results.Forbid();

    if (hold.Status != "HELD" && hold.Status != "Active")
        return Results.Conflict("Only active holds can be cancelled.");

    if (hold.ExpiresAtUtc <= DateTime.UtcNow)
    {
        hold.Status = "EXPIRED";
        await db.SaveChangesAsync();
        return Results.Conflict("Hold has expired.");
    }

    hold.Status = "CANCELLED";
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.Run();
