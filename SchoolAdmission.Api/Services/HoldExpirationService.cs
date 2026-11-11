
using Microsoft.EntityFrameworkCore;
using SchoolAdmission.Api.Data;

namespace SchoolAdmission.Api.Services;

public class HoldExpirationService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public HoldExpirationService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();

            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;

            await db.AdmissionHolds
                .Where(h =>
                    (h.Status == "Active" || h.Status == "HELD") &&
                    h.ExpiresAtUtc <= now)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(h => h.Status, "EXPIRED"),
                    stoppingToken);

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
