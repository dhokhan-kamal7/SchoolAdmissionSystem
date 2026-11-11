
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;
using SchoolAdmission.Api.Models;

namespace SchoolAdmission.Api.Services;

public class AdmissionService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _configuration;

    public AdmissionService(AppDbContext db, IConfiguration configuration)
    {
        _db = db;
        _configuration = configuration;
    }

    public async Task<AdmissionHold?> CreateHoldAsync(
        int classId,
        string studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId) ||
            studentId.Length != 17 ||
            !studentId.All(char.IsDigit))
            return null;

        await using var transaction =
            await _db.Database.BeginTransactionAsync();

        await _db.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock({classId})");

        var schoolClass = await _db.SchoolClasses
            .FirstOrDefaultAsync(c => c.Id == classId);

        if (schoolClass == null)
            return null;

        var now = DateTime.UtcNow;

        var existing = await _db.AdmissionHolds.AnyAsync(h =>
            h.SchoolClassId == classId &&
            h.StudentId == studentId &&
            (h.Status == "Confirmed" ||
             h.Status == "CONFIRMED" ||
             ((h.Status == "Active" || h.Status == "HELD") &&
              h.ExpiresAtUtc > now)));

        if (existing)
            return null;

        var activeHolds = await _db.AdmissionHolds.CountAsync(h =>
            h.SchoolClassId == classId &&
            (h.Status == "Confirmed" ||
             h.Status == "CONFIRMED" ||
             ((h.Status == "Active" || h.Status == "HELD") &&
              h.ExpiresAtUtc > now)));

        if (activeHolds >= schoolClass.TotalSeats)
            return null;

        var duration =
            _configuration.GetValue<int?>("HOLD_DURATION_SECONDS") ?? 600;

        var hold = new AdmissionHold
        {
            Id = Guid.NewGuid(),
            SchoolClassId = classId,
            StudentId = studentId,
            Status = "HELD",
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddSeconds(duration)
        };

        _db.AdmissionHolds.Add(hold);
        await _db.SaveChangesAsync();

        await transaction.CommitAsync();

        return hold;
    }
}
