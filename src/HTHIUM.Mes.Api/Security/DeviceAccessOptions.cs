namespace HTHIUM.Mes.Api.Security;

public sealed class DeviceAccessOptions
{
    public bool RequireApiKey { get; init; }

    public string ApiKey { get; init; } = string.Empty;
}
