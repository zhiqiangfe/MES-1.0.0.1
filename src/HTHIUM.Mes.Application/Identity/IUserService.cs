using HTHIUM.Mes.Contracts.Identity;

namespace HTHIUM.Mes.Application.Identity;

public interface IUserService
{
    Task<UserDto?> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserDto>> GetUsersAsync(
        CancellationToken cancellationToken = default);

    Task<UserDto> CreateUserAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<UserDeletionResult> DeleteUserAsync(
        Guid userId,
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task<bool> HasAnyUserAsync(CancellationToken cancellationToken = default);
}
