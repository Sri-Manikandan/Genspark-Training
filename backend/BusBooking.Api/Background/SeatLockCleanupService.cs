using BusBooking.Api.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BusBooking.Api.Background;

public class SeatLockCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(60);
    private readonly IServiceProvider _sp;
    private readonly ILogger<SeatLockCleanupService> _log;

    public SeatLockCleanupService(IServiceProvider sp, ILogger<SeatLockCleanupService> log)
    {
        _sp = sp;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var time = scope.ServiceProvider.GetRequiredService<TimeProvider>();
                var cutoff = time.GetUtcNow().UtcDateTime;
                var removed = await db.SeatLocks
                    .Where(l => l.ExpiresAt < cutoff)
                    .ExecuteDeleteAsync(stoppingToken);
                if (removed > 0)
                    _log.LogInformation("SeatLockCleanup removed {Count} expired locks", removed);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _log.LogError(ex, "SeatLockCleanup tick failed");
            }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }
}

