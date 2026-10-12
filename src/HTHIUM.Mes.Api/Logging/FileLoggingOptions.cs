using Microsoft.Extensions.Logging;

namespace HTHIUM.Mes.Api.Logging;

public sealed class FileLoggingOptions
{
    public bool Enabled { get; init; } = true;

    public string Directory { get; init; } = "../../logs";

    public LogLevel MinimumLevel { get; init; } = LogLevel.Information;

    public int RetainedDays { get; init; } = 30;

    public int MaxFileSizeMb { get; init; } = 20;
}
