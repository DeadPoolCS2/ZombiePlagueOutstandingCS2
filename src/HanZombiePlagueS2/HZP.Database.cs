using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySqlConnector;

namespace HanZombiePlagueS2;

/// <summary>Handles async MySQL persistence for Ammo Packs.</summary>
public class HZPDatabase
{
    private readonly ILogger<HZPDatabase> _logger;
    private readonly IOptionsMonitor<HZPMainCFG> _mainCFG;

    // Only alphanumeric and underscores are allowed in the table name to prevent SQL injection.
    private static readonly Regex _safeTableName = new(@"^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    public HZPDatabase(ILogger<HZPDatabase> logger, IOptionsMonitor<HZPMainCFG> mainCFG)
    {
        _logger = logger;
        _mainCFG = mainCFG;
    }

    // ── database.jsonc support ────────────────────────────────────────────────

    private sealed class DbConnectionEntry
    {
        [JsonPropertyName("host")] public string Host { get; set; } = "127.0.0.1";
        [JsonPropertyName("port")] public int Port { get; set; } = 3306;
        [JsonPropertyName("database")] public string Database { get; set; } = "";
        [JsonPropertyName("user")] public string User { get; set; } = "root";
        [JsonPropertyName("password")] public string Password { get; set; } = "";
    }

    private sealed class DbRegistry
    {
        [JsonPropertyName("default_connection")] public string DefaultConnection { get; set; } = "";
        [JsonPropertyName("connections")] public Dictionary<string, DbConnectionEntry> Connections { get; set; } = new();
    }

    /// <summary>
    /// Attempts to load a named connection from configs/database.jsonc (Swiftly shared registry).
    /// Returns null if the file or connection cannot be found.
    /// </summary>
    private DbConnectionEntry? TryLoadFromRegistry(string connectionName)
    {
        // Probe a few candidate paths relative to the current working directory,
        // matching the directory layout expected on a Swiftly-based CS2 server.
        // Walk up the directory tree from the current working directory looking for
        // a configs/database.jsonc file. This is more robust than hard-coded relative paths.
        string? dir = Directory.GetCurrentDirectory();
        const int maxLevels = 6;
        for (int i = 0; i < maxLevels && dir is not null; i++, dir = Directory.GetParent(dir)?.FullName)
        {
            string candidate = Path.Combine(dir, "configs", "database.jsonc");
            if (!File.Exists(candidate))
                continue;

            try
            {
                var json = File.ReadAllText(candidate);
                // Strip JSONC comments: both single-line // ... and block /* ... */ comments.
                var stripped = Regex.Replace(json, @"/\*.*?\*/|//[^\r\n]*", "",
                    RegexOptions.Singleline);
                var registry = JsonSerializer.Deserialize<DbRegistry>(stripped,
                    new JsonSerializerOptions { AllowTrailingCommas = true });
                if (registry is null) continue;

                var key = string.IsNullOrWhiteSpace(connectionName)
                    ? registry.DefaultConnection
                    : connectionName;

                if (!string.IsNullOrWhiteSpace(key) &&
                    registry.Connections.TryGetValue(key, out var entry))
                {
                    _logger.LogInformation("[HZP-DB] Using connection '{Key}' from {File}.", key, candidate);
                    return entry;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[HZP-DB] Could not parse {File}: {Ex}", candidate, ex.Message);
            }
        }

        return null;
    }

    private string BuildConnectionString()
    {
        var cfg = _mainCFG.CurrentValue;
        var entry = TryLoadFromRegistry(cfg.AmmoPacksConnectionName);

        if (entry is not null)
        {
            return new MySqlConnectionStringBuilder
            {
                Server = entry.Host,
                Port = (uint)entry.Port,
                Database = entry.Database,
                UserID = entry.User,
                Password = entry.Password,
                ConnectionTimeout = 5,
                AllowPublicKeyRetrieval = false
            }.ConnectionString;
        }

        // Fall back: no database.jsonc found – DB operations will fail gracefully.
        _logger.LogWarning("[HZP-DB] configs/database.jsonc not found or connection '{Name}' missing. " +
            "Set AmmoPacksConnectionName in HZPMainCFG.jsonc and ensure configs/database.jsonc exists.",
            cfg.AmmoPacksConnectionName);
        return string.Empty;
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
        if (!_mainCFG.CurrentValue.AmmoPacksEnabled) return;
        if (!TryGetSafeTableName(out var table)) return;
        var connStr = BuildConnectionString();
        if (string.IsNullOrEmpty(connStr)) return;
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
        if (!_mainCFG.CurrentValue.AmmoPacksEnabled) return null;
        if (!TryGetSafeTableName(out var table)) return null;
        var connStr = BuildConnectionString();
        if (string.IsNullOrEmpty(connStr)) return null;
        try
        {
            await using var conn = new MySqlConnection(connStr);
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
        if (!_mainCFG.CurrentValue.AmmoPacksEnabled) return;
        if (!TryGetSafeTableName(out var table)) return;
        var connStr = BuildConnectionString();
        if (string.IsNullOrEmpty(connStr)) return;
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
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[HZP-DB] SaveAmmoPacks({SteamId},{AP}) failed: {Ex}", steamId, ammoPacks, ex.Message);
        }
    }
}

