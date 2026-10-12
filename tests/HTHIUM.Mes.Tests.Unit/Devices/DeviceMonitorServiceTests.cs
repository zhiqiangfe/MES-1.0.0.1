using HTHIUM.Mes.Application.Devices;
using HTHIUM.Mes.Contracts.Devices;
using HTHIUM.Mes.Domain.Devices;
using HTHIUM.Mes.Infrastructure.Devices;

namespace HTHIUM.Mes.Tests.Unit.Devices;

public sealed class DeviceMonitorServiceTests
{
    [Fact]
    public async Task ReceiveHeartbeat_UpsertsDeviceAndIncrementsHeartbeatCount()
    {
        var repository = new InMemoryDeviceStateRepository();
        var notifier = new RecordingNotifier();
        var service = CreateService(repository, notifier);
        var request = new DeviceHeartbeatRequest
        {
            DeviceCode = "DEVICE-01",
            DeviceName = "测试设备",
            LineCode = "LINE-01",
            DeviceStatus = "Running",
            PlcConnected = true,
            ClientVersion = "1.0.0",
        };

        await service.ReceiveHeartbeatAsync(request);
        var second = await service.ReceiveHeartbeatAsync(request);

        Assert.True(second.IsOnline);
        Assert.Equal(2, second.HeartbeatCount);
        Assert.Equal(2, notifier.Received.Count);
        Assert.Equal(1, (await service.GetSummaryAsync()).OnlineCount);
    }

    [Fact]
    public async Task MarkExpiredDevicesOffline_ChangesExpiredDeviceAndNotifiesClients()
    {
        var repository = new InMemoryDeviceStateRepository();
        await repository.UpsertAsync(new DeviceState
        {
            DeviceCode = "DEVICE-OFFLINE",
            DeviceName = "离线设备",
            LineCode = "LINE-01",
            DeviceStatus = "Running",
            PlcConnected = true,
            ClientVersion = "1.0.0",
            DeviceTime = DateTimeOffset.Now.AddMinutes(-1),
            LastSeenTime = DateTimeOffset.Now.AddMinutes(-1),
            IsOnline = true,
            HeartbeatCount = 1,
        });
        var notifier = new RecordingNotifier();
        var service = CreateService(repository, notifier);

        var changedCount = await service.MarkExpiredDevicesOfflineAsync();

        Assert.Equal(1, changedCount);
        var device = Assert.Single(await service.GetDevicesAsync());
        Assert.False(device.IsOnline);
        Assert.False(device.PlcConnected);
        Assert.Single(notifier.Received);
    }

    private static DeviceMonitorService CreateService(
        IDeviceStateRepository repository,
        IDeviceRealtimeNotifier notifier) =>
        new(repository, notifier, new DeviceMonitoringOptions
        {
            OnlineTimeoutSeconds = 15,
        });

    private sealed class RecordingNotifier : IDeviceRealtimeNotifier
    {
        public List<DeviceStatusDto> Received { get; } = [];

        public Task DeviceStatusChangedAsync(
            DeviceStatusDto device,
            CancellationToken cancellationToken = default)
        {
            Received.Add(device);
            return Task.CompletedTask;
        }
    }
}
