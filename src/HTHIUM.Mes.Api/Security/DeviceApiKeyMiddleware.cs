using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace HTHIUM.Mes.Api.Security;

public sealed class DeviceApiKeyMiddleware
{
    private const string HeaderName = "X-Device-Api-Key";
    private readonly RequestDelegate _next;

    public DeviceApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IOptions<DeviceAccessOptions> options)
    {
        var settings = options.Value;
        if (!settings.RequireApiKey ||
            !context.Request.Path.StartsWithSegments("/api/v1/edge"))
        {
            await _next(context);
            return;
        }

        var suppliedBytes = Encoding.UTF8.GetBytes(
            context.Request.Headers[HeaderName].ToString());
        var expectedBytes = Encoding.UTF8.GetBytes(settings.ApiKey);
        if (suppliedBytes.Length == expectedBytes.Length &&
            CryptographicOperations.FixedTimeEquals(suppliedBytes, expectedBytes))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "设备认证失败",
            extensions: new Dictionary<string, object?>
            {
                ["code"] = "DEVICE_AUTH_FAILED",
                ["traceId"] = context.TraceIdentifier,
            }).ExecuteAsync(context);
    }
}
