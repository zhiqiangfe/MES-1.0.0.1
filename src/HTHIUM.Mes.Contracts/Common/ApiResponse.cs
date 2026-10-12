namespace HTHIUM.Mes.Contracts.Common;

public sealed record ApiResponse<T>
{
    public bool Success { get; init; } = true;
    public string Code { get; init; } = "OK";
    public string Message { get; init; } = "成功";
    public string TraceId { get; init; } = string.Empty;
    public DateTimeOffset ServerTime { get; init; } = DateTimeOffset.Now;
    public T? Data { get; init; }
}
