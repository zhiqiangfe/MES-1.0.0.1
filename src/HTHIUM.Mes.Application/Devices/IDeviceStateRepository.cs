using HTHIUM.Mes.Domain.Devices;

namespace HTHIUM.Mes.Application.Devices;

public interface IDeviceStateRepository
{
    Task<DeviceState> UpsertAsync(
        DeviceState heartbeatState,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeviceState>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<DeviceState?> GetAsync(
        string deviceCode,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeviceState>> MarkOfflineBeforeAsync(
        DateTimeOffset cutoffTime,
        CancellationToken cancellationToken = default);
}
