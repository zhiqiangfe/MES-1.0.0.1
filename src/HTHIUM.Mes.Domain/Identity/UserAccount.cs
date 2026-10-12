namespace HTHIUM.Mes.Domain.Identity;

public static class UserRoles
{
    public const string Administrator = "Administrator";
    public const string Operator = "Operator";

    public static bool IsSupported(string role) =>
        role is Administrator or Operator;
}

public sealed record UserAccount
{
    public required Guid Id { get; init; }
    public required string Username { get; init; }
    public required string DisplayName { get; init; }
    public required string PasswordHash { get; init; }
    public required string Role { get; init; }
    public bool IsEnabled { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
}
