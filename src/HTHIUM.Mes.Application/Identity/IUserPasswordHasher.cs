using HTHIUM.Mes.Domain.Identity;

namespace HTHIUM.Mes.Application.Identity;

public interface IUserPasswordHasher
{
    string Hash(UserAccount user, string password);

    bool Verify(UserAccount user, string passwordHash, string password);
}
