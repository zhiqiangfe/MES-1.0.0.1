using HTHIUM.Mes.Application.Devices;
using HTHIUM.Mes.Domain.Devices;
using HTHIUM.Mes.Infrastructure.Database;
using MySqlConnector;

namespace HTHIUM.Mes.Infrastructure.Devices;

public sealed class MySqlDeviceStateRepository : IDeviceStateRepository
{
    private readonly MesDatabaseOptions _options;

    public MySqlDeviceStateRepository(MesDatabaseOptions options)
    {
        _options = options;
    }

    public async Task<DeviceState> UpsertAsync(
        DeviceState heartbeatState,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO mes_device_state (
                    device_code, device_name, line_code, device_status, plc_connected,
                    client_version, device_time_utc, last_seen_time_utc, is_online,
                    heartbeat_count, updated_at_utc)
                VALUES (
                    @deviceCode, @deviceName, @lineCode, @deviceStatus, @plcConnected,
                    @clientVersion, @deviceTime, @lastSeenTime, 1, 1, UTC_TIMESTAMP(6))
                ON DUPLICATE KEY UPDATE
                    device_name = VALUES(device_name),
                    line_code = VALUES(line_code),
                    device_status = VALUES(device_status),
                    plc_connected = VALUES(plc_connected),
                    client_version = VALUES(client_version),
                    device_time_utc = VALUES(device_time_utc),
                    last_seen_time_utc = VALUES(last_seen_time_utc),
                    is_online = 1,
                    heartbeat_count = heartbeat_count + 1,
                    updated_at_utc = UTC_TIMESTAMP(6);
                """;
            command.Parameters.AddWithValue("@deviceCode", heartbeatState.DeviceCode);
            command.Parameters.AddWithValue("@deviceName", heartbeatState.DeviceName);
            command.Parameters.AddWithValue("@lineCode", heartbeatState.LineCode);
            command.Parameters.AddWithValue("@deviceStatus", heartbeatState.DeviceStatus);
            command.Parameters.AddWithValue("@plcConnected", heartbeatState.PlcConnected);
            command.Parameters.AddWithValue("@clientVersion", heartbeatState.ClientVersion);
            command.Parameters.AddWithValue("@deviceTime", heartbeatState.DeviceTime.UtcDateTime);
            command.Parameters.AddWithValue("@lastSeenTime", heartbeatState.LastSeenTime.UtcDateTime);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        var result = await GetByCodeAsync(
            connection,
            transaction,
            heartbeatState.DeviceCode,
            cancellationToken) ?? throw new InvalidOperationException("心跳保存后未找到设备状态");
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<IReadOnlyList<DeviceState>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = SelectColumns + " ORDER BY is_online DESC, line_code, device_code;";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var result = new List<DeviceState>();
        while (await reader.ReadAsync(cancellationToken)) result.Add(Map(reader));
        return result;
    }

    public async Task<DeviceState?> GetAsync(
        string deviceCode,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return await GetByCodeAsync(connection, null, deviceCode, cancellationToken);
    }

    public async Task<IReadOnlyList<DeviceState>> MarkOfflineBeforeAsync(
        DateTimeOffset cutoffTime,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new MySqlConnection(_options.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var expired = new List<DeviceState>();
        await using (var selectCommand = connection.CreateCommand())
        {
            selectCommand.Transaction = transaction;
            selectCommand.CommandText = SelectColumns +
                " WHERE is_online = 1 AND last_seen_time_utc < @cutoff FOR UPDATE;";
            selectCommand.Parameters.AddWithValue("@cutoff", cutoffTime.UtcDateTime);
            await using var reader = await selectCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                expired.Add(Map(reader) with
                {
                    IsOnline = false,
                    DeviceStatus = "Offline",
                    PlcConnected = false,
                });
            }
        }

        if (expired.Count > 0)
        {
            await using var updateCommand = connection.CreateCommand();
            updateCommand.Transaction = transaction;
            updateCommand.CommandText = """
                UPDATE mes_device_state
                SET is_online = 0, device_status = 'Offline', plc_connected = 0,
                    updated_at_utc = UTC_TIMESTAMP(6)
                WHERE is_online = 1 AND last_seen_time_utc < @cutoff;
                """;
            updateCommand.Parameters.AddWithValue("@cutoff", cutoffTime.UtcDateTime);
            await updateCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return expired;
    }

    private static async Task<DeviceState?> GetByCodeAsync(
        MySqlConnection connection,
        MySqlTransaction? transaction,
        string deviceCode,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = SelectColumns + " WHERE device_code = @deviceCode LIMIT 1;";
        command.Parameters.AddWithValue("@deviceCode", deviceCode);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    private const string SelectColumns = """
        SELECT device_code, device_name, line_code, device_status, plc_connected,
               client_version, device_time_utc, last_seen_time_utc, is_online,
               heartbeat_count
        FROM mes_device_state
        """;

    private static DeviceState Map(MySqlDataReader reader) => new()
    {
        DeviceCode = reader.GetString("device_code"),
        DeviceName = reader.GetString("device_name"),
        LineCode = reader.GetString("line_code"),
        DeviceStatus = reader.GetString("device_status"),
        PlcConnected = reader.GetBoolean("plc_connected"),
        ClientVersion = reader.GetString("client_version"),
        DeviceTime = AsUtc(reader.GetDateTime("device_time_utc")),
        LastSeenTime = AsUtc(reader.GetDateTime("last_seen_time_utc")),
        IsOnline = reader.GetBoolean("is_online"),
        HeartbeatCount = reader.GetInt64("heartbeat_count"),
    };

    private static DateTimeOffset AsUtc(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
