using HTHIUM.Mes.Application.Devices;

namespace HTHIUM.Mes.Api.BackgroundServices;

public sealed class DeviceOfflineMonitorService : BackgroundService
{
    private readonly IDeviceMonitorService _deviceMonitorService;
    private readonly ILogger<DeviceOfflineMonitorService> _logger;

    public DeviceOfflineMonitorService(
        IDeviceMonitorService deviceMonitorService,
        ILogger<DeviceOfflineMonitorService> logger)
    {
        _deviceMonitorService = deviceMonitorService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(2));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            var changedCount = await _deviceMonitorService
                .MarkExpiredDevicesOfflineAsync(stoppingToken);
            if (changedCount > 0)
            {
                _logger.LogInformation(
                    "Marked {DeviceCount} device(s) offline after heartbeat timeout",
                    changedCount);
            }
        }
    }
}
