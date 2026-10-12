using HTHIUM.Mes.Contracts.Devices;

namespace HTHIUM.Mes.Application.Devices;

public interface IDeviceRealtimeNotifier
{
    Task DeviceStatusChangedAsync(
        DeviceStatusDto device,
        CancellationToken cancellationToken = default);
}
