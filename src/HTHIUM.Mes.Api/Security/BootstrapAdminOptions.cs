namespace HTHIUM.Mes.Api.Security;

public sealed class BootstrapAdminOptions
{
    public string Username { get; init; } = "admin";
    public string DisplayName { get; init; } = "系统管理员";
    public string Password { get; init; } = string.Empty;
}
