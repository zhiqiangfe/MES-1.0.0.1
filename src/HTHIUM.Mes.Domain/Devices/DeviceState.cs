namespace HTHIUM.Mes.Domain.Devices;

public sealed record DeviceState
{
    public required string DeviceCode { get; init; }
    public required string DeviceName { get; init; }
    public required string LineCode { get; init; }
    public required string DeviceStatus { get; init; }
    public bool PlcConnected { get; init; }
    public required string ClientVersion { get; init; }
    public DateTimeOffset DeviceTime { get; init; }
    public DateTimeOffset LastSeenTime { get; init; }
    public bool IsOnline { get; init; }
    public long HeartbeatCount { get; init; }
}
