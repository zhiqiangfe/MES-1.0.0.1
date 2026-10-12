using HTHIUM.Mes.Application.Devices;
using HTHIUM.Mes.Contracts.Common;
using HTHIUM.Mes.Contracts.Devices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HTHIUM.Mes.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/mes/devices")]
public sealed class MesDeviceController : ControllerBase
{
    private readonly IDeviceMonitorService _deviceMonitorService;

    public MesDeviceController(IDeviceMonitorService deviceMonitorService)
    {
        _deviceMonitorService = deviceMonitorService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DeviceStatusDto>>>> GetDevices(
        CancellationToken cancellationToken) =>
        Ok(Wrap(await _deviceMonitorService.GetDevicesAsync(cancellationToken)));

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DeviceMonitorSummaryDto>>> GetSummary(
        CancellationToken cancellationToken) =>
        Ok(Wrap(await _deviceMonitorService.GetSummaryAsync(cancellationToken)));

    [HttpGet("{deviceCode}")]
    public async Task<ActionResult<ApiResponse<DeviceStatusDto>>> GetDevice(
        string deviceCode,
        CancellationToken cancellationToken)
    {
        var device = await _deviceMonitorService.GetDeviceAsync(deviceCode, cancellationToken);
        return device is null ? NotFound() : Ok(Wrap(device));
    }

    private ApiResponse<T> Wrap<T>(T data) => new()
    {
        TraceId = HttpContext.TraceIdentifier,
        Data = data,
    };
}
