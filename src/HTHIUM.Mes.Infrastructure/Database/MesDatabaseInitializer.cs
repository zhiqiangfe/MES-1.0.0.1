using System.Text.RegularExpressions;
using MySqlConnector;

namespace HTHIUM.Mes.Infrastructure.Database;

public sealed partial class MesDatabaseInitializer : IMesDatabaseInitializer
{
    private readonly MesDatabaseOptions _options;

    public MesDatabaseInitializer(MesDatabaseOptions options)
    {
        _options = options;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var databaseBuilder = new MySqlConnectionStringBuilder(_options.ConnectionString);
        var databaseName = databaseBuilder.Database;
        if (string.IsNullOrWhiteSpace(databaseName) || !DatabaseNameRegex().IsMatch(databaseName))
        {
            throw new InvalidOperationException("MES数据库名称无效");
        }

        var serverBuilder = new MySqlConnectionStringBuilder(_options.ConnectionString)
        {
            Database = string.Empty,
        };
        await using (var serverConnection = new MySqlConnection(serverBuilder.ConnectionString))
        {
            await serverConnection.OpenAsync(cancellationToken);
            await using var createDatabaseCommand = serverConnection.CreateCommand();
            createDatabaseCommand.CommandText =
                $"CREATE DATABASE IF NOT EXISTS `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
            await createDatabaseCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var connection = new MySqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var schemaCommand = connection.CreateCommand();
        schemaCommand.CommandText = SchemaSql;
        await schemaCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private const string SchemaSql = """
        CREATE TABLE IF NOT EXISTS mes_schema_version (
            version_no INT NOT NULL PRIMARY KEY,
            description VARCHAR(200) NOT NULL,
            applied_at_utc DATETIME(6) NOT NULL
        );

        INSERT IGNORE INTO mes_schema_version(version_no, description, applied_at_utc)
        VALUES (1, 'Initial online device monitoring and identity schema', UTC_TIMESTAMP(6));

        CREATE TABLE IF NOT EXISTS mes_device_state (
            device_code VARCHAR(100) NOT NULL PRIMARY KEY,
            device_name VARCHAR(200) NOT NULL,
            line_code VARCHAR(100) NOT NULL,
            device_status VARCHAR(50) NOT NULL,
            plc_connected TINYINT(1) NOT NULL,
            client_version VARCHAR(50) NOT NULL,
            device_time_utc DATETIME(6) NOT NULL,
            last_seen_time_utc DATETIME(6) NOT NULL,
            is_online TINYINT(1) NOT NULL,
            heartbeat_count BIGINT NOT NULL,
            updated_at_utc DATETIME(6) NOT NULL,
            INDEX ix_mes_device_state_online_seen (is_online, last_seen_time_utc),
            INDEX ix_mes_device_state_line (line_code, device_code)
        );

        CREATE TABLE IF NOT EXISTS mes_user (
            id CHAR(36) NOT NULL PRIMARY KEY,
            username VARCHAR(64) NOT NULL,
            display_name VARCHAR(100) NOT NULL,
            password_hash VARCHAR(512) NOT NULL,
            role VARCHAR(32) NOT NULL,
            is_enabled TINYINT(1) NOT NULL,
            created_at_utc DATETIME(6) NOT NULL,
            last_login_at_utc DATETIME(6) NULL,
            UNIQUE KEY ux_mes_user_username (username)
        );
        """;

    [GeneratedRegex("^[A-Za-z0-9_]+$")]
    private static partial Regex DatabaseNameRegex();
}
