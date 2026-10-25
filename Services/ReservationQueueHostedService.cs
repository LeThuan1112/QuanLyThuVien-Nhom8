using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ThuVien.Services;

/// <summary>
/// Refreshes expired pickup holds and promotes waiting readers periodically.
/// Uses a scoped ReservationService so each cycle receives a fresh DbContext.
/// </summary>
public sealed class ReservationQueueHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationQueueHostedService> _logger;

    public ReservationQueueHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<ReservationQueueHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give application startup/database bootstrap a moment to finish before the first refresh.
        try { await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken); }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var reservations = scope.ServiceProvider.GetRequiredService<ReservationService>();
                await reservations.RefreshQueueAsync();
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Không thể làm mới hàng chờ đặt trước.");
            }

            try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
