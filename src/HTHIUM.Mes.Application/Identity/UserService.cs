using HTHIUM.Mes.Contracts.Identity;
using HTHIUM.Mes.Domain.Identity;

namespace HTHIUM.Mes.Application.Identity;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly IUserPasswordHasher _passwordHasher;

    public UserService(IUserRepository repository, IUserPasswordHasher passwordHasher)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserDto?> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedUsername = NormalizeUsername(username);
        var user = await _repository.FindByUsernameAsync(normalizedUsername, cancellationToken);
        if (user is null || !user.IsEnabled ||
            !_passwordHasher.Verify(user, user.PasswordHash, password))
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        await _repository.UpdateLastLoginAsync(user.Id, now, cancellationToken);
        return Map(user with { LastLoginAt = now });
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(
        CancellationToken cancellationToken = default) =>
        (await _repository.GetAllAsync(cancellationToken)).Select(Map).ToArray();

    public async Task<UserDto> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var username = NormalizeUsername(request.Username);
        if (username.Length is < 3 or > 64)
        {
            throw new ArgumentException("用户名长度必须为3到64个字符");
        }

        if (request.Password.Length < 8 ||
            !request.Password.Any(char.IsUpper) ||
            !request.Password.Any(char.IsLower) ||
            !request.Password.Any(char.IsDigit))
        {
            throw new ArgumentException("密码至少8位，并且必须包含大写字母、小写字母和数字");
        }

        if (!UserRoles.IsSupported(request.Role))
        {
            throw new ArgumentException("账号角色不受支持");
        }

        if (await _repository.FindByUsernameAsync(username, cancellationToken) is not null)
        {
            throw new InvalidOperationException("用户名已经存在");
        }

        var user = new UserAccount
        {
            Id = Guid.NewGuid(),
            Username = username,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName)
                ? username
                : request.DisplayName.Trim(),
            PasswordHash = string.Empty,
            Role = request.Role,
            IsEnabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        user = user with { PasswordHash = _passwordHasher.Hash(user, request.Password) };
        await _repository.CreateAsync(user, cancellationToken);
        return Map(user);
    }

    public Task<bool> HasAnyUserAsync(CancellationToken cancellationToken = default) =>
        _repository.AnyAsync(cancellationToken);

    public Task<UserDeletionResult> DeleteUserAsync(
        Guid userId,
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        if (userId == currentUserId)
        {
            return Task.FromResult(UserDeletionResult.CurrentUser);
        }

        return _repository.DeleteAsync(userId, cancellationToken);
    }

    private static string NormalizeUsername(string username) =>
        (username ?? string.Empty).Trim().ToLowerInvariant();

    private static UserDto Map(UserAccount user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        DisplayName = user.DisplayName,
        Role = user.Role,
        IsEnabled = user.IsEnabled,
        CreatedAt = user.CreatedAt,
        LastLoginAt = user.LastLoginAt,
    };
}
