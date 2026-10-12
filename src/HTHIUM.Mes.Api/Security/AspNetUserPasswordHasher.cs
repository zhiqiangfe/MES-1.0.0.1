using HTHIUM.Mes.Application.Identity;
using HTHIUM.Mes.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace HTHIUM.Mes.Api.Security;

public sealed class AspNetUserPasswordHasher : IUserPasswordHasher
{
    private readonly PasswordHasher<UserAccount> _hasher = new();

    public string Hash(UserAccount user, string password) =>
        _hasher.HashPassword(user, password);

    public bool Verify(UserAccount user, string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(user, passwordHash, password) !=
        PasswordVerificationResult.Failed;
}
