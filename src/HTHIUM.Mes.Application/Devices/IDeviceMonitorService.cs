using HTHIUM.Mes.Contracts.Devices;

namespace HTHIUM.Mes.Application.Devices;

public interface IDeviceMonitorService
{
    Task<DeviceStatusDto> ReceiveHeartbeatAsync(
        DeviceHeartbeatRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeviceStatusDto>> GetDevicesAsync(
        CancellationToken cancellationToken = default);

    Task<DeviceStatusDto?> GetDeviceAsync(
        string deviceCode,
        CancellationToken cancellationToken = default);

    Task<DeviceMonitorSummaryDto> GetSummaryAsync(
        CancellationToken cancellationToken = default);

    Task<int> MarkExpiredDevicesOfflineAsync(
        CancellationToken cancellationToken = default);
}
