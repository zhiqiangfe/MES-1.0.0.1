using System.Collections.Concurrent;
using HTHIUM.Mes.Application.Devices;
using HTHIUM.Mes.Domain.Devices;

namespace HTHIUM.Mes.Infrastructure.Devices;

public sealed class InMemoryDeviceStateRepository : IDeviceStateRepository
{
    private readonly ConcurrentDictionary<string, DeviceState> _devices =
        new(StringComparer.OrdinalIgnoreCase);

    public Task<DeviceState> UpsertAsync(
        DeviceState heartbeatState,
        CancellationToken cancellationToken = default)
    {
        var result = _devices.AddOrUpdate(
            heartbeatState.DeviceCode,
            heartbeatState,
            (_, previous) => heartbeatState with
            {
                HeartbeatCount = previous.HeartbeatCount + 1,
            });
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<DeviceState>> GetAllAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DeviceState>>(_devices.Values.ToArray());

    public Task<DeviceState?> GetAsync(
        string deviceCode,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_devices.TryGetValue(deviceCode, out var state) ? state : null);

    public Task<IReadOnlyList<DeviceState>> MarkOfflineBeforeAsync(
        DateTimeOffset cutoffTime,
        CancellationToken cancellationToken = default)
    {
        var changed = new List<DeviceState>();
        foreach (var pair in _devices)
        {
            if (!pair.Value.IsOnline || pair.Value.LastSeenTime >= cutoffTime)
            {
                continue;
            }

            var offline = pair.Value with
            {
                IsOnline = false,
                DeviceStatus = "Offline",
                PlcConnected = false,
            };
            if (_devices.TryUpdate(pair.Key, offline, pair.Value))
            {
                changed.Add(offline);
            }
        }

        return Task.FromResult<IReadOnlyList<DeviceState>>(changed);
    }
}
