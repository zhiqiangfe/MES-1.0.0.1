using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace HTHIUM.Mes.Api.Logging;

public sealed class RollingFileLoggerProvider : ILoggerProvider
{
    private readonly Lock _writeLock = new();
    private readonly string _directory;
    private readonly LogLevel _minimumLevel;
    private readonly int _retainedDays;
    private readonly long _maxFileSizeBytes;

    public RollingFileLoggerProvider(
        string directory,
        LogLevel minimumLevel,
        int retainedDays,
        int maxFileSizeMb)
    {
        _directory = directory;
        _minimumLevel = minimumLevel;
        _retainedDays = Math.Clamp(retainedDays, 1, 3650);
        _maxFileSizeBytes = Math.Max(1, maxFileSizeMb) * 1024L * 1024L;

        Directory.CreateDirectory(_directory);
        DeleteExpiredFiles();
    }

    public ILogger CreateLogger(string categoryName) =>
        new RollingFileLogger(this, categoryName);

    public void Dispose()
    {
    }

    private bool IsEnabled(LogLevel logLevel) =>
        logLevel != LogLevel.None && logLevel >= _minimumLevel;

    private void Write(
        LogLevel logLevel,
        EventId eventId,
        string category,
        string message,
        Exception? exception)
    {
        if (!IsEnabled(logLevel)) return;

        var timestamp = DateTimeOffset.Now;
        var line = string.Create(
            CultureInfo.InvariantCulture,
            $"{timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{GetLevelName(logLevel)}] [{category}] [Event:{eventId.Id}] {message}");

        if (exception is not null)
        {
            line += Environment.NewLine + exception;
        }

        try
        {
            lock (_writeLock)
            {
                var path = ResolveLogPath(timestamp);
                File.AppendAllText(path, line + Environment.NewLine, System.Text.Encoding.UTF8);
            }
        }
        catch (Exception writeException)
        {
            Debug.WriteLine($"MES file logging failed: {writeException}");
        }
    }

    private string ResolveLogPath(DateTimeOffset timestamp)
    {
        var prefix = $"mes-api-{timestamp:yyyyMMdd}";
        var path = Path.Combine(_directory, prefix + ".log");
        if (!File.Exists(path) || new FileInfo(path).Length < _maxFileSizeBytes)
        {
            return path;
        }

        for (var index = 1; index < 1000; index++)
        {
            path = Path.Combine(_directory, $"{prefix}.{index}.log");
            if (!File.Exists(path) || new FileInfo(path).Length < _maxFileSizeBytes)
            {
                return path;
            }
        }

        return Path.Combine(_directory, $"{prefix}.{Guid.NewGuid():N}.log");
    }

    private void DeleteExpiredFiles()
    {
        var cutoff = DateTime.Now.Date.AddDays(-_retainedDays);
        foreach (var path in System.IO.Directory.EnumerateFiles(_directory, "mes-api-*.log"))
        {
            try
            {
                if (File.GetLastWriteTime(path) < cutoff) File.Delete(path);
            }
            catch (Exception exception)
            {
                Debug.WriteLine($"MES log retention cleanup failed for {path}: {exception}");
            }
        }
    }

    private static string GetLevelName(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRC",
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        LogLevel.Critical => "FTL",
        _ => "NON",
    };

    private sealed class RollingFileLogger(
        RollingFileLoggerProvider provider,
        string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull =>
            NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => provider.IsEnabled(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var message = formatter(state, exception);
            if (string.IsNullOrWhiteSpace(message) && exception is null) return;
            provider.Write(logLevel, eventId, categoryName, message, exception);
        }
    }

    private sealed class NullScope : IDisposable
    {
        public static NullScope Instance { get; } = new();

        public void Dispose()
        {
        }
    }
}
