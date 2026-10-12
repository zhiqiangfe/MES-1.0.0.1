using HTHIUM.Mes.Domain.Identity;

namespace HTHIUM.Mes.Application.Identity;

public interface IUserRepository
{
    Task<UserAccount?> FindByUsernameAsync(
        string normalizedUsername,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserAccount>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(CancellationToken cancellationToken = default);

    Task CreateAsync(
        UserAccount user,
        CancellationToken cancellationToken = default);

    Task UpdateLastLoginAsync(
        Guid userId,
        DateTimeOffset loginTime,
        CancellationToken cancellationToken = default);

    Task<UserDeletionResult> DeleteAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public enum UserDeletionResult
{
    Deleted,
    NotFound,
    LastAdministrator,
    CurrentUser,
}
