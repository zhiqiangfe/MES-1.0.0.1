using HTHIUM.Mes.Contracts.Devices;
using HTHIUM.Mes.Domain.Devices;

namespace HTHIUM.Mes.Application.Devices;

public sealed class DeviceMonitorService : IDeviceMonitorService
{
    private readonly IDeviceStateRepository _repository;
    private readonly IDeviceRealtimeNotifier _notifier;
    private readonly DeviceMonitoringOptions _options;

    public DeviceMonitorService(
        IDeviceStateRepository repository,
        IDeviceRealtimeNotifier notifier,
        DeviceMonitoringOptions options)
    {
        _repository = repository;
        _notifier = notifier;
        _options = options;
    }

    public async Task<DeviceStatusDto> ReceiveHeartbeatAsync(
        DeviceHeartbeatRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceCode))
        {
            throw new ArgumentException("设备编码不能为空", nameof(request));
        }

        var now = DateTimeOffset.Now;
        var normalizedCode = request.DeviceCode.Trim();
        var state = await _repository.UpsertAsync(new DeviceState
        {
            DeviceCode = normalizedCode,
            DeviceName = string.IsNullOrWhiteSpace(request.DeviceName)
                ? normalizedCode
                : request.DeviceName.Trim(),
            LineCode = request.LineCode.Trim(),
            DeviceStatus = string.IsNullOrWhiteSpace(request.DeviceStatus)
                ? "Unknown"
                : request.DeviceStatus.Trim(),
            PlcConnected = request.PlcConnected,
            ClientVersion = request.ClientVersion.Trim(),
            DeviceTime = request.EventTime == default ? now : request.EventTime,
            LastSeenTime = now,
            IsOnline = true,
            HeartbeatCount = 1,
        }, cancellationToken);

        var result = Map(state);
        await _notifier.DeviceStatusChangedAsync(result, cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<DeviceStatusDto>> GetDevicesAsync(
        CancellationToken cancellationToken = default) =>
        (await _repository.GetAllAsync(cancellationToken))
            .Select(Map)
            .OrderByDescending(item => item.IsOnline)
            .ThenBy(item => item.LineCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.DeviceCode, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public async Task<DeviceStatusDto?> GetDeviceAsync(
        string deviceCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceCode))
        {
            return null;
        }

        var state = await _repository.GetAsync(deviceCode.Trim(), cancellationToken);
        return state is null ? null : Map(state);
    }

    public async Task<DeviceMonitorSummaryDto> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        var devices = await _repository.GetAllAsync(cancellationToken);
        return new DeviceMonitorSummaryDto
        {
            TotalCount = devices.Count,
            OnlineCount = devices.Count(item => item.IsOnline),
            OfflineCount = devices.Count(item => !item.IsOnline),
            PlcConnectedCount = devices.Count(item => item.IsOnline && item.PlcConnected),
            ServerTime = DateTimeOffset.Now,
        };
    }

    public async Task<int> MarkExpiredDevicesOfflineAsync(
        CancellationToken cancellationToken = default)
    {
        var cutoff = DateTimeOffset.Now - _options.OnlineTimeout;
        var changed = await _repository.MarkOfflineBeforeAsync(cutoff, cancellationToken);
        foreach (var device in changed)
        {
            await _notifier.DeviceStatusChangedAsync(Map(device), cancellationToken);
        }

        return changed.Count;
    }

    private static DeviceStatusDto Map(DeviceState state) => new()
    {
        DeviceCode = state.DeviceCode,
        DeviceName = state.DeviceName,
        LineCode = state.LineCode,
        DeviceStatus = state.DeviceStatus,
        PlcConnected = state.PlcConnected,
        ClientVersion = state.ClientVersion,
        DeviceTime = state.DeviceTime,
        LastSeenTime = state.LastSeenTime,
        IsOnline = state.IsOnline,
        HeartbeatCount = state.HeartbeatCount,
    };
}
