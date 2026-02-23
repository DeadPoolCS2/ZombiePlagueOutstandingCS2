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
        // Values may be either an object (host/port/database/user/password) or a string DSN
        // (e.g. "mysql://user:password@host:3306/database") – both formats are supported.
        [JsonPropertyName("connections")] public Dictionary<string, JsonElement> Connections { get; set; } = new();
    }

    /// <summary>
    /// Parses a MySQL DSN string of the form <c>mysql://user:password@host:port/database</c>
    /// into a <see cref="DbConnectionEntry"/>. Returns <c>null</c> on parse failure.
    /// </summary>
    private static DbConnectionEntry? ParseDsn(string? dsn)
    {
        if (string.IsNullOrWhiteSpace(dsn)) return null;
        try
        {
            var uri = new Uri(dsn);
            if (!uri.Scheme.Equals("mysql", StringComparison.OrdinalIgnoreCase)) return null;

            var userInfo = uri.UserInfo.Split(':', 2);
            // Empty user/password is intentionally allowed – some MySQL setups
            // accept connections without credentials (e.g. unix socket auth).
            var user = Uri.UnescapeDataString(userInfo[0]);
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";

            return new DbConnectionEntry
            {
                Host = uri.Host,
                Port = uri.Port > 0 ? uri.Port : 3306,
                Database = uri.AbsolutePath.TrimStart('/'),
                User = user,
                Password = password
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to load a named connection from configs/database.jsonc (Swiftly shared registry).
    /// Supports both object-style entries and string DSN values (e.g. <c>mysql://…</c>).
    /// When <paramref name="connectionName"/> is empty, falls back to <c>default_connection</c>.
    /// Returns <c>null</c> if the file or connection cannot be found.
    /// </summary>
    private DbConnectionEntry? TryLoadFromRegistry(string connectionName)
    {
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

                // Use the explicit connection name, or fall back to default_connection.
                var key = string.IsNullOrWhiteSpace(connectionName)
                    ? registry.DefaultConnection
                    : connectionName;

                if (!string.IsNullOrWhiteSpace(key) &&
                    registry.Connections.TryGetValue(key, out var element))
                {
                    // Support both string DSN ("mysql://…") and object-style connection entries.
                    DbConnectionEntry? entry = element.ValueKind == JsonValueKind.String
                        ? ParseDsn(element.GetString())
                        : element.Deserialize<DbConnectionEntry>();

                    if (entry is not null)
                    {
                        _logger.LogInformation("[HZP-DB] Using connection '{Key}' from {File}.", key, candidate);
                        return entry;
                    }
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
        var cfg = _mainCFG.CurrentValue;
        if (!cfg.AmmoPacksEnabled) return;
        if (!TryGetSafeTableName(out var table)) return;
        var connStr = BuildConnectionString();
        if (string.IsNullOrEmpty(connStr)) return;

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
        var connStr = BuildConnectionString();
        if (string.IsNullOrEmpty(connStr)) return null;

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
        var connStr = BuildConnectionString();
        if (string.IsNullOrEmpty(connStr)) return;

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
        var connStr = BuildConnectionString();
        if (string.IsNullOrEmpty(connStr)) return;

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

