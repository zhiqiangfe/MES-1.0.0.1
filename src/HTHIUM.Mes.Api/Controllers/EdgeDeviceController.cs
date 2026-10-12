using HTHIUM.Mes.Application.Devices;
using HTHIUM.Mes.Contracts.Common;
using HTHIUM.Mes.Contracts.Devices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HTHIUM.Mes.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/edge/devices")]
public sealed class EdgeDeviceController : ControllerBase
{
    private readonly IDeviceMonitorService _deviceMonitorService;

    public EdgeDeviceController(IDeviceMonitorService deviceMonitorService)
    {
        _deviceMonitorService = deviceMonitorService;
    }

    [HttpPost("heartbeat")]
    [ProducesResponseType(typeof(ApiResponse<HeartbeatAcceptedDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<HeartbeatAcceptedDto>>> ReceiveHeartbeat(
        DeviceHeartbeatRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var device = await _deviceMonitorService
                .ReceiveHeartbeatAsync(request, cancellationToken);
            return Ok(new ApiResponse<HeartbeatAcceptedDto>
            {
                Code = "HEARTBEAT_ACCEPTED",
                Message = "心跳已接收",
                TraceId = HttpContext.TraceIdentifier,
                Data = new HeartbeatAcceptedDto
                {
                    DeviceCode = device.DeviceCode,
                    ServerTime = DateTimeOffset.Now,
                },
            });
        }
        catch (ArgumentException exception)
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "心跳数据不合法",
                detail: exception.Message,
                extensions: new Dictionary<string, object?>
                {
                    ["code"] = "VALIDATION_FAILED",
                    ["traceId"] = HttpContext.TraceIdentifier,
                });
        }
    }
}
