using HTHIUM.Mes.Application.Identity;
using HTHIUM.Mes.Domain.Identity;
using HTHIUM.Mes.Infrastructure.Database;
using MySqlConnector;

namespace HTHIUM.Mes.Infrastructure.Identity;

public sealed class MySqlUserRepository : IUserRepository
{
    private readonly MesDatabaseOptions _options;

    public MySqlUserRepository(MesDatabaseOptions options)
    {
        _options = options;
    }

    public async Task<UserAccount?> FindByUsernameAsync(
        string normalizedUsername,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = SelectColumns + " WHERE username = @username LIMIT 1;";
        command.Parameters.AddWithValue("@username", normalizedUsername);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<UserAccount>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = SelectColumns + " ORDER BY created_at_utc, username;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<UserAccount>();
        while (await reader.ReadAsync(cancellationToken)) result.Add(Map(reader));
        return result;
    }

    public async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT EXISTS(SELECT 1 FROM mes_user LIMIT 1);";
        return Convert.ToBoolean(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task CreateAsync(
        UserAccount user,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO mes_user (
                id, username, display_name, password_hash, role, is_enabled,
                created_at_utc, last_login_at_utc)
            VALUES (
                @id, @username, @displayName, @passwordHash, @role, @isEnabled,
                @createdAt, @lastLoginAt);
            """;
        command.Parameters.AddWithValue("@id", user.Id.ToString("D"));
        command.Parameters.AddWithValue("@username", user.Username);
        command.Parameters.AddWithValue("@displayName", user.DisplayName);
        command.Parameters.AddWithValue("@passwordHash", user.PasswordHash);
        command.Parameters.AddWithValue("@role", user.Role);
        command.Parameters.AddWithValue("@isEnabled", user.IsEnabled);
        command.Parameters.AddWithValue("@createdAt", user.CreatedAt.UtcDateTime);
        command.Parameters.AddWithValue("@lastLoginAt", user.LastLoginAt?.UtcDateTime);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateLastLoginAsync(
        Guid userId,
        DateTimeOffset loginTime,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            "UPDATE mes_user SET last_login_at_utc = @loginTime WHERE id = @id;";
        command.Parameters.AddWithValue("@loginTime", loginTime.UtcDateTime);
        command.Parameters.AddWithValue("@id", userId.ToString("D"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<UserDeletionResult> DeleteAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        string? role;
        await using (var targetCommand = connection.CreateCommand())
        {
            targetCommand.Transaction = transaction;
            targetCommand.CommandText = "SELECT role FROM mes_user WHERE id = @id FOR UPDATE;";
            targetCommand.Parameters.AddWithValue("@id", userId.ToString("D"));
            role = Convert.ToString(await targetCommand.ExecuteScalarAsync(cancellationToken));
        }

        if (role is null)
        {
            await transaction.CommitAsync(cancellationToken);
            return UserDeletionResult.NotFound;
        }

        if (string.Equals(role, UserRoles.Administrator, StringComparison.Ordinal))
        {
            var administratorCount = 0;
            await using var administratorCommand = connection.CreateCommand();
            administratorCommand.Transaction = transaction;
            administratorCommand.CommandText =
                "SELECT id FROM mes_user WHERE role = @role FOR UPDATE;";
            administratorCommand.Parameters.AddWithValue("@role", UserRoles.Administrator);
            await using var reader = await administratorCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) administratorCount++;
            await reader.CloseAsync();

            if (administratorCount <= 1)
            {
                await transaction.CommitAsync(cancellationToken);
                return UserDeletionResult.LastAdministrator;
            }
        }

        await using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.Transaction = transaction;
            deleteCommand.CommandText = "DELETE FROM mes_user WHERE id = @id;";
            deleteCommand.Parameters.AddWithValue("@id", userId.ToString("D"));
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return UserDeletionResult.Deleted;
    }

    private const string SelectColumns = """
        SELECT id, username, display_name, password_hash, role, is_enabled,
               created_at_utc, last_login_at_utc
        FROM mes_user
        """;

    private static UserAccount Map(MySqlDataReader reader) => new()
    {
        Id = reader.GetGuid("id"),
        Username = reader.GetString("username"),
        DisplayName = reader.GetString("display_name"),
        PasswordHash = reader.GetString("password_hash"),
        Role = reader.GetString("role"),
        IsEnabled = reader.GetBoolean("is_enabled"),
        CreatedAt = AsUtc(reader.GetDateTime("created_at_utc")),
        LastLoginAt = reader.IsDBNull(reader.GetOrdinal("last_login_at_utc"))
            ? null
            : AsUtc(reader.GetDateTime("last_login_at_utc")),
    };

    private static DateTimeOffset AsUtc(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
