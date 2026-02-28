using Cookies.Contract;
using Economy.Contract;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HanZombiePlagueS2;

// ─────────────────────────────────────────────────────────────────────────────
//  Backend abstraction
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Abstraction over the three supported Ammo Packs persistence backends:
/// MySQL (built-in), Economy plugin, and Cookies plugin.
/// </summary>
public interface IAmmoPacksBackend
{
    /// <summary>Called once on plugin load / map load to prepare the backend (e.g. create tables).</summary>
    Task EnsureReadyAsync();

    /// <summary>Loads the AP balance for a player. Returns null when unavailable.</summary>
    Task<int?> LoadAsync(ulong steamId);

    /// <summary>Saves the AP balance for a single player.</summary>
    Task SaveAsync(ulong steamId, int ammoPacks);

    /// <summary>Saves AP balances for a batch of players (map change / unload failsafe).</summary>
    Task SaveAllAsync(IEnumerable<(ulong steamId, int ammoPacks)> players);
}

// ─────────────────────────────────────────────────────────────────────────────
//  MySQL backend  (wraps the existing HZPDatabase)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Ammo Packs backend that uses the built-in MySQL/MariaDB connection.</summary>
public class MySqlAmmoPacksBackend : IAmmoPacksBackend
{
    private readonly HZPDatabase _db;

    public MySqlAmmoPacksBackend(HZPDatabase db)
    {
        _db = db;
    }

    public Task EnsureReadyAsync() => _db.EnsureTableAsync();

    public Task<int?> LoadAsync(ulong steamId) => _db.LoadAmmoPacksAsync(steamId);

    public Task SaveAsync(ulong steamId, int ammoPacks) => _db.SaveAmmoPacksAsync(steamId, ammoPacks);

    public Task SaveAllAsync(IEnumerable<(ulong steamId, int ammoPacks)> players) =>
        _db.SaveAllPlayersAsync(players);
}

// ─────────────────────────────────────────────────────────────────────────────
//  Economy plugin backend
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Ammo Packs backend that delegates to the Economy plugin
/// (https://github.com/DeadPoolCS2/Economy) via <see cref="IEconomyAPIv1"/>.
/// </summary>
public class EconomyAmmoPacksBackend : IAmmoPacksBackend
{
    private readonly ILogger<EconomyAmmoPacksBackend> _logger;
    private readonly IOptionsMonitor<HZPMainCFG> _mainCFG;
    private IEconomyAPIv1? _api;

    public EconomyAmmoPacksBackend(
        ILogger<EconomyAmmoPacksBackend> logger,
        IOptionsMonitor<HZPMainCFG> mainCFG)
    {
        _logger = logger;
        _mainCFG = mainCFG;
    }

    /// <summary>Called by the plugin after the Economy shared interface is resolved.</summary>
    public void SetApi(IEconomyAPIv1 api) => _api = api;

    public Task EnsureReadyAsync()
    {
        if (_api == null)
        {
            _logger.LogWarning("[HZP-Economy] Economy API not available – AmmoPacksStorageBackend=Economy requires the Economy plugin to be loaded.");
            return Task.CompletedTask;
        }

        var walletKind = _mainCFG.CurrentValue.EconomyWalletKind;
        if (!_api.WalletKindExists(walletKind))
        {
            _api.EnsureWalletKind(walletKind);
            _logger.LogInformation("[HZP-Economy] Registered wallet kind '{Kind}'.", walletKind);
        }

        return Task.CompletedTask;
    }

    public Task<int?> LoadAsync(ulong steamId)
    {
        if (_api == null)
            return Task.FromResult<int?>(null);

        try
        {
            var walletKind = _mainCFG.CurrentValue.EconomyWalletKind;
            decimal balance = _api.GetPlayerBalance(steamId, walletKind);
            int ap = (int)Math.Max(0, Math.Truncate(balance));

            if (_mainCFG.CurrentValue.EnableCommandDebugLogs)
                _logger.LogInformation("[HZP-Economy] Load: steamid={SteamId} ap={AP}", steamId, ap);

            return Task.FromResult<int?>(ap);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[HZP-Economy] LoadAsync({SteamId}) failed: {Ex}", steamId, ex.Message);
            return Task.FromResult<int?>(null);
        }
    }

    public Task SaveAsync(ulong steamId, int ammoPacks)
    {
        if (_api == null)
            return Task.CompletedTask;

        try
        {
            var walletKind = _mainCFG.CurrentValue.EconomyWalletKind;
            _api.SetPlayerBalance(steamId, walletKind, ammoPacks);
            _api.SaveData(steamId);

            if (_mainCFG.CurrentValue.EnableCommandDebugLogs)
                _logger.LogInformation("[HZP-Economy] Save: steamid={SteamId} ap={AP}", steamId, ammoPacks);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[HZP-Economy] SaveAsync({SteamId},{AP}) failed: {Ex}", steamId, ammoPacks, ex.Message);
        }

        return Task.CompletedTask;
    }

    public Task SaveAllAsync(IEnumerable<(ulong steamId, int ammoPacks)> players)
    {
        if (_api == null)
            return Task.CompletedTask;

        var walletKind = _mainCFG.CurrentValue.EconomyWalletKind;
        foreach (var (steamId, ammoPacks) in players)
        {
            try
            {
                _api.SetPlayerBalance(steamId, walletKind, ammoPacks);
                _api.SaveData(steamId);

                if (_mainCFG.CurrentValue.EnableCommandDebugLogs)
                    _logger.LogInformation("[HZP-Economy] SaveAll: steamid={SteamId} ap={AP}", steamId, ammoPacks);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[HZP-Economy] SaveAllAsync({SteamId},{AP}) failed: {Ex}", steamId, ammoPacks, ex.Message);
            }
        }

        return Task.CompletedTask;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Cookies plugin backend
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Ammo Packs backend that delegates to the Cookies plugin
/// (https://github.com/DeadPoolCS2/Cookies) via <see cref="IPlayerCookiesAPIv1"/>.
/// </summary>
public class CookiesAmmoPacksBackend : IAmmoPacksBackend
{
    private readonly ILogger<CookiesAmmoPacksBackend> _logger;
    private readonly IOptionsMonitor<HZPMainCFG> _mainCFG;
    private IPlayerCookiesAPIv1? _api;

    public CookiesAmmoPacksBackend(
        ILogger<CookiesAmmoPacksBackend> logger,
        IOptionsMonitor<HZPMainCFG> mainCFG)
    {
        _logger = logger;
        _mainCFG = mainCFG;
    }

    /// <summary>Called by the plugin after the Cookies shared interface is resolved.</summary>
    public void SetApi(IPlayerCookiesAPIv1 api) => _api = api;

    public Task EnsureReadyAsync()
    {
        if (_api == null)
            _logger.LogWarning("[HZP-Cookies] Cookies API not available – AmmoPacksStorageBackend=Cookies requires the Cookies plugin to be loaded.");

        return Task.CompletedTask;
    }

    public Task<int?> LoadAsync(ulong steamId)
    {
        if (_api == null)
            return Task.FromResult<int?>(null);

        try
        {
            var key = _mainCFG.CurrentValue.CookiesAmmoPacksKey;
            long sid = (long)steamId;

            if (!_api.Has(sid, key))
                return Task.FromResult<int?>(null);

            int? ap = _api.Get<int>(sid, key);

            if (_mainCFG.CurrentValue.EnableCommandDebugLogs)
                _logger.LogInformation("[HZP-Cookies] Load: steamid={SteamId} ap={AP}", steamId, ap);

            return Task.FromResult(ap);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[HZP-Cookies] LoadAsync({SteamId}) failed: {Ex}", steamId, ex.Message);
            return Task.FromResult<int?>(null);
        }
    }

    public Task SaveAsync(ulong steamId, int ammoPacks)
    {
        if (_api == null)
            return Task.CompletedTask;

        try
        {
            var key = _mainCFG.CurrentValue.CookiesAmmoPacksKey;
            long sid = (long)steamId;
            _api.Set(sid, key, ammoPacks);
            _api.Save(sid);

            if (_mainCFG.CurrentValue.EnableCommandDebugLogs)
                _logger.LogInformation("[HZP-Cookies] Save: steamid={SteamId} ap={AP}", steamId, ammoPacks);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("[HZP-Cookies] SaveAsync({SteamId},{AP}) failed: {Ex}", steamId, ammoPacks, ex.Message);
        }

        return Task.CompletedTask;
    }

    public Task SaveAllAsync(IEnumerable<(ulong steamId, int ammoPacks)> players)
    {
        if (_api == null)
            return Task.CompletedTask;

        var key = _mainCFG.CurrentValue.CookiesAmmoPacksKey;
        foreach (var (steamId, ammoPacks) in players)
        {
            try
            {
                long sid = (long)steamId;
                _api.Set(sid, key, ammoPacks);
                _api.Save(sid);

                if (_mainCFG.CurrentValue.EnableCommandDebugLogs)
                    _logger.LogInformation("[HZP-Cookies] SaveAll: steamid={SteamId} ap={AP}", steamId, ammoPacks);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[HZP-Cookies] SaveAllAsync({SteamId},{AP}) failed: {Ex}", steamId, ammoPacks, ex.Message);
            }
        }

        return Task.CompletedTask;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Backend resolver  (selects the active backend based on config)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Resolves and exposes the active <see cref="IAmmoPacksBackend"/> based on
/// <see cref="HZPMainCFG.AmmoPacksStorageBackend"/>.
/// </summary>
public class AmmoPacksBackendResolver
{
    private readonly IOptionsMonitor<HZPMainCFG> _mainCFG;
    private readonly MySqlAmmoPacksBackend _mysql;
    private readonly EconomyAmmoPacksBackend _economy;
    private readonly CookiesAmmoPacksBackend _cookies;

    public AmmoPacksBackendResolver(
        IOptionsMonitor<HZPMainCFG> mainCFG,
        MySqlAmmoPacksBackend mysql,
        EconomyAmmoPacksBackend economy,
        CookiesAmmoPacksBackend cookies)
    {
        _mainCFG = mainCFG;
        _mysql = mysql;
        _economy = economy;
        _cookies = cookies;
    }

    /// <summary>Returns the currently configured backend.</summary>
    public IAmmoPacksBackend Active => _mainCFG.CurrentValue.AmmoPacksStorageBackend switch
    {
        AmmoPacksBackend.Economy => _economy,
        AmmoPacksBackend.Cookies => _cookies,
        _ => _mysql
    };

    /// <summary>Provides direct access to the Economy backend for API injection.</summary>
    public EconomyAmmoPacksBackend Economy => _economy;

    /// <summary>Provides direct access to the Cookies backend for API injection.</summary>
    public CookiesAmmoPacksBackend Cookies => _cookies;
}
