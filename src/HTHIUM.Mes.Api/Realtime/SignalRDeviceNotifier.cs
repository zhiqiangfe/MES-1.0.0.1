using HTHIUM.Mes.Application.Devices;
using HTHIUM.Mes.Contracts.Devices;
using Microsoft.AspNetCore.SignalR;

namespace HTHIUM.Mes.Api.Realtime;

public sealed class SignalRDeviceNotifier : IDeviceRealtimeNotifier
{
    private readonly IHubContext<OperationsHub> _hubContext;

    public SignalRDeviceNotifier(IHubContext<OperationsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task DeviceStatusChangedAsync(
        DeviceStatusDto device,
        CancellationToken cancellationToken = default) =>
        _hubContext.Clients.All.SendAsync(
            "deviceStatusChanged",
            device,
            cancellationToken);
}
