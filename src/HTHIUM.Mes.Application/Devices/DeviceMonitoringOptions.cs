namespace HTHIUM.Mes.Application.Devices;

public sealed class DeviceMonitoringOptions
{
    public int OnlineTimeoutSeconds { get; init; } = 15;

    public TimeSpan OnlineTimeout =>
        TimeSpan.FromSeconds(Math.Clamp(OnlineTimeoutSeconds, 5, 300));
}
