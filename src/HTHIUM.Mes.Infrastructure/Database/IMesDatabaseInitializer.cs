namespace HTHIUM.Mes.Infrastructure.Database;

public interface IMesDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
