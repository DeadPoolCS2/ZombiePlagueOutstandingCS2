using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace HanZombiePlagueS2;

/// <summary>Handles async MySQL persistence for Ammo Packs.</summary>
public class HZPDatabase
{
    private readonly ILogger<HZPDatabase> _logger;
    private readonly IOptionsMonitor<HZPDatabaseCFG> _dbCFG;

    // Only alphanumeric and underscores are allowed in the table name to prevent SQL injection.
    private static readonly Regex _safeTableName = new(@"^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    public HZPDatabase(ILogger<HZPDatabase> logger, IOptionsMonitor<HZPDatabaseCFG> dbCFG)
    {
        _logger = logger;
        _dbCFG = dbCFG;
    }

    private string BuildConnectionString()
    {
        var cfg = _dbCFG.CurrentValue;
        return new MySqlConnectionStringBuilder
        {
            Server = cfg.Host,
            Port = (uint)cfg.Port,
            Database = cfg.Database,
            UserID = cfg.User,
            Password = cfg.Password,
            ConnectionTimeout = 5,
            AllowPublicKeyRetrieval = false
        }.ConnectionString;
    }

    private bool TryGetSafeTableName(out string tableName)
    {
        tableName = _dbCFG.CurrentValue.TableName;
        if (!_safeTableName.IsMatch(tableName))
        {
            _logger.LogWarning("[HZP-DB] Invalid table name '{Table}' in config (only alphanumeric and underscores allowed). DB operations skipped.", tableName);
            return false;
        }
        return true;
    }

    /// <summary>Creates the AP table if it does not exist.</summary>
    public async Task EnsureTableAsync()
    {
        var cfg = _dbCFG.CurrentValue;
        if (!cfg.Enabled) return;
        if (!TryGetSafeTableName(out var table)) return;
        try
        {
            await using var conn = new MySqlConnection(BuildConnectionString());
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"""
                CREATE TABLE IF NOT EXISTS `{table}` (
                    `steamid`    BIGINT UNSIGNED NOT NULL,
                    `ammopacks`  INT NOT NULL DEFAULT 0,
                    PRIMARY KEY (`steamid`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
                """;
            await cmd.ExecuteNonQueryAsync();
            _logger.LogInformation("[HZP-DB] Table `{Table}` ready.", table);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[HZP-DB] Could not ensure table: {Ex}", ex.Message);
        }
    }

    /// <summary>Loads AP for a player. Returns null when DB is disabled or unavailable.</summary>
    public async Task<int?> LoadAmmoPacksAsync(ulong steamId)
    {
        var cfg = _dbCFG.CurrentValue;
        if (!cfg.Enabled) return null;
        if (!TryGetSafeTableName(out var table)) return null;
        try
        {
            await using var conn = new MySqlConnection(BuildConnectionString());
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT `ammopacks` FROM `{table}` WHERE `steamid`=@sid LIMIT 1;";
            cmd.Parameters.AddWithValue("@sid", steamId);
            var result = await cmd.ExecuteScalarAsync();
            if (result is null || result is DBNull) return null;
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[HZP-DB] LoadAmmoPacks({SteamId}) failed: {Ex}", steamId, ex.Message);
            return null;
        }
    }

    /// <summary>Saves AP for a player. Fire-and-forget safe to call from game thread.</summary>
    public async Task SaveAmmoPacksAsync(ulong steamId, int ammoPacks)
    {
        var cfg = _dbCFG.CurrentValue;
        if (!cfg.Enabled) return;
        if (!TryGetSafeTableName(out var table)) return;
        try
        {
            await using var conn = new MySqlConnection(BuildConnectionString());
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"""
                INSERT INTO `{table}` (`steamid`, `ammopacks`)
                VALUES (@sid, @ap)
                ON DUPLICATE KEY UPDATE `ammopacks` = @ap;
                """;
            cmd.Parameters.AddWithValue("@sid", steamId);
            cmd.Parameters.AddWithValue("@ap", ammoPacks);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[HZP-DB] SaveAmmoPacks({SteamId},{AP}) failed: {Ex}", steamId, ammoPacks, ex.Message);
        }
    }
}
