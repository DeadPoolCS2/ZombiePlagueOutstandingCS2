using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;
using SwiftlyS2.Shared;

namespace HanZombiePlagueS2;

/// <summary>Handles async MySQL persistence for Ammo Packs.</summary>
public class HZPDatabase
{
    private readonly ILogger<HZPDatabase> _logger;
    private readonly IOptionsMonitor<HZPMainCFG> _mainCFG;
    private readonly ISwiftlyCore _core;

    // Only alphanumeric and underscores are allowed in the table name to prevent SQL injection.
    private static readonly System.Text.RegularExpressions.Regex _safeTableName =
        new(@"^[A-Za-z0-9_]+$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public HZPDatabase(ILogger<HZPDatabase> logger, IOptionsMonitor<HZPMainCFG> mainCFG, ISwiftlyCore core)
    {
        _logger = logger;
        _mainCFG = mainCFG;
        _core = core;
    }

    /// <summary>
    /// Resolves the MySQL connection string via the SwiftlyS2 database service.
    /// SwiftlyS2 reads <c>configs/database.jsonc</c> and supports both object-style
    /// entries and DSN strings (e.g. <c>mysql://user:pass@host/db</c>).
    /// When <paramref name="connectionName"/> is empty it falls back to the
    /// <c>default_connection</c> defined in that file — no manual parsing required.
    /// </summary>
    private string? ResolveConnectionString(string connectionName)
    {
        try
        {
            var connStr = _core.Database.GetConnectionString(connectionName);
            return string.IsNullOrWhiteSpace(connStr) ? null : connStr;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[HZP-DB] Could not resolve connection '{Name}' from configs/database.jsonc: {Ex}",
                string.IsNullOrWhiteSpace(connectionName) ? "(default)" : connectionName, ex.Message);
            return null;
        }
    }

    private bool TryGetSafeTableName(out string tableName)
    {
        tableName = _mainCFG.CurrentValue.AmmoPacksTableName;
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
        var cfg = _mainCFG.CurrentValue;
        if (!cfg.AmmoPacksEnabled) return;
        if (!TryGetSafeTableName(out var table)) return;

        var connStr = ResolveConnectionString(cfg.AmmoPacksConnectionName);
        if (connStr is null)
        {
            _logger.LogWarning("[HZP-DB] Connection '{Name}' could not be resolved. " +
                "Set AmmoPacksConnectionName in HZPMainCFG.jsonc to a key in configs/database.jsonc, " +
                "or leave empty to use default_connection.",
                string.IsNullOrWhiteSpace(cfg.AmmoPacksConnectionName) ? "(default)" : cfg.AmmoPacksConnectionName);
            return;
        }

        var connName = string.IsNullOrWhiteSpace(cfg.AmmoPacksConnectionName) ? "(default)" : cfg.AmmoPacksConnectionName;
        if (cfg.EnableCommandDebugLogs)
            _logger.LogInformation("[HZP-DB] EnsureTable: connection='{Conn}' table='{Table}'", connName, table);

        try
        {
            await using var conn = new MySqlConnection(connStr);
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
        var cfg = _mainCFG.CurrentValue;
        if (!cfg.AmmoPacksEnabled) return null;
        if (!TryGetSafeTableName(out var table)) return null;

        var connStr = ResolveConnectionString(cfg.AmmoPacksConnectionName);
        if (connStr is null) return null;

        var connName = string.IsNullOrWhiteSpace(cfg.AmmoPacksConnectionName) ? "(default)" : cfg.AmmoPacksConnectionName;
        if (cfg.EnableCommandDebugLogs)
            _logger.LogInformation("[HZP-DB] LoadAmmoPacks: connection='{Conn}' table='{Table}' steamid={SteamId}", connName, table, steamId);

        try
        {
            await using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"SELECT `ammopacks` FROM `{table}` WHERE `steamid`=@sid LIMIT 1;";
            cmd.Parameters.AddWithValue("@sid", steamId);
            var result = await cmd.ExecuteScalarAsync();
            if (result is null || result is DBNull)
            {
                if (cfg.EnableCommandDebugLogs)
                    _logger.LogInformation("[HZP-DB] LoadAmmoPacks: no row found for steamid={SteamId} (new player).", steamId);
                return null;
            }
            int ap = Convert.ToInt32(result);
            if (cfg.EnableCommandDebugLogs)
                _logger.LogInformation("[HZP-DB] LoadAmmoPacks: steamid={SteamId} ap={AP} SUCCESS.", steamId, ap);
            return ap;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[HZP-DB] LoadAmmoPacks({SteamId}) failed: {Ex}", steamId, ex.Message);
            return null;
        }
    }

    /// <summary>Saves AP for a player (UPSERT). Fire-and-forget safe to call from game thread.</summary>
    public async Task SaveAmmoPacksAsync(ulong steamId, int ammoPacks)
    {
        var cfg = _mainCFG.CurrentValue;
        if (!cfg.AmmoPacksEnabled) return;
        if (!TryGetSafeTableName(out var table)) return;

        var connStr = ResolveConnectionString(cfg.AmmoPacksConnectionName);
        if (connStr is null) return;

        var connName = string.IsNullOrWhiteSpace(cfg.AmmoPacksConnectionName) ? "(default)" : cfg.AmmoPacksConnectionName;
        if (cfg.EnableCommandDebugLogs)
            _logger.LogInformation("[HZP-DB] SaveAmmoPacks: connection='{Conn}' table='{Table}' steamid={SteamId} ap={AP}", connName, table, steamId, ammoPacks);

        try
        {
            await using var conn = new MySqlConnection(connStr);
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
            if (cfg.EnableCommandDebugLogs)
                _logger.LogInformation("[HZP-DB] SaveAmmoPacks: steamid={SteamId} ap={AP} SUCCESS.", steamId, ammoPacks);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[HZP-DB] SaveAmmoPacks({SteamId},{AP}) failed: {Ex}", steamId, ammoPacks, ex.Message);
        }
    }

    /// <summary>
    /// Saves ammo packs for a batch of players (failsafe on map change / plugin unload).
    /// Each element is (SteamID64, ammoPacks).
    /// </summary>
    public async Task SaveAllPlayersAsync(IEnumerable<(ulong steamId, int ammoPacks)> players)
    {
        var cfg = _mainCFG.CurrentValue;
        if (!cfg.AmmoPacksEnabled) return;
        if (!TryGetSafeTableName(out var table)) return;

        var connStr = ResolveConnectionString(cfg.AmmoPacksConnectionName);
        if (connStr is null) return;

        var connName = string.IsNullOrWhiteSpace(cfg.AmmoPacksConnectionName) ? "(default)" : cfg.AmmoPacksConnectionName;
        var playerList = players.ToList();
        if (playerList.Count == 0) return;

        try
        {
            await using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();

            foreach (var (steamId, ammoPacks) in playerList)
            {
                if (cfg.EnableCommandDebugLogs)
                    _logger.LogInformation("[HZP-DB] SaveAllPlayers: connection='{Conn}' table='{Table}' steamid={SteamId} ap={AP}", connName, table, steamId, ammoPacks);
                try
                {
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"""
                        INSERT INTO `{table}` (`steamid`, `ammopacks`)
                        VALUES (@sid, @ap)
                        ON DUPLICATE KEY UPDATE `ammopacks` = @ap;
                        """;
                    cmd.Parameters.AddWithValue("@sid", steamId);
                    cmd.Parameters.AddWithValue("@ap", ammoPacks);
                    await cmd.ExecuteNonQueryAsync();
                    if (cfg.EnableCommandDebugLogs)
                        _logger.LogInformation("[HZP-DB] SaveAllPlayers: steamid={SteamId} ap={AP} SUCCESS.", steamId, ammoPacks);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("[HZP-DB] SaveAllPlayers({SteamId},{AP}) failed: {Ex}", steamId, ammoPacks, ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[HZP-DB] SaveAllPlayers: could not open connection: {Ex}", ex.Message);
        }
    }
}

