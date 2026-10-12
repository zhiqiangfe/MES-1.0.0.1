namespace HTHIUM.Mes.Contracts.Devices;

public sealed record DeviceHeartbeatRequest
{
    public string DeviceCode { get; init; } = string.Empty;
    public string DeviceName { get; init; } = string.Empty;
    public string LineCode { get; init; } = string.Empty;
    public string DeviceStatus { get; init; } = "Unknown";
    public bool PlcConnected { get; init; }
    public string ClientVersion { get; init; } = string.Empty;
    public DateTimeOffset EventTime { get; init; } = DateTimeOffset.Now;
}

public sealed record DeviceStatusDto
{
    public string DeviceCode { get; init; } = string.Empty;
    public string DeviceName { get; init; } = string.Empty;
    public string LineCode { get; init; } = string.Empty;
    public string DeviceStatus { get; init; } = string.Empty;
    public bool PlcConnected { get; init; }
    public string ClientVersion { get; init; } = string.Empty;
    public DateTimeOffset DeviceTime { get; init; }
    public DateTimeOffset LastSeenTime { get; init; }
    public bool IsOnline { get; init; }
    public long HeartbeatCount { get; init; }
}

public sealed record DeviceMonitorSummaryDto
{
    public int TotalCount { get; init; }
    public int OnlineCount { get; init; }
    public int OfflineCount { get; init; }
    public int PlcConnectedCount { get; init; }
    public DateTimeOffset ServerTime { get; init; } = DateTimeOffset.Now;
}

public sealed record HeartbeatAcceptedDto
{
    public string DeviceCode { get; init; } = string.Empty;
    public DateTimeOffset ServerTime { get; init; }
}
