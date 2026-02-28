using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;

namespace HanZombiePlagueS2;

/// <summary>
/// Authoritative ammo-pack state manager.
/// Designed after ShopCore-style flow:
/// 1) load authoritative balance on connect (with retries),
/// 2) keep in-memory state for live gameplay,
/// 3) write-through persistence after state is loaded.
/// </summary>
public class AmmoPacksService
{
    private sealed class SlotState
    {
        public int Generation;
        public ulong SteamId;
        public int Balance;
        public bool Loaded;
        public int PendingDelta;
        public bool LoadInProgress;
    }

    private readonly ISwiftlyCore _core;
    private readonly ILogger<AmmoPacksService> _logger;
    private readonly HZPGlobals _globals;
    private readonly IOptionsMonitor<HZPMainCFG> _mainCFG;
    private readonly IOptionsMonitor<HZPExtraItemsCFG> _extraItemsCFG;
    private readonly AmmoPacksBackendResolver _backendResolver;

    private readonly Dictionary<int, SlotState> _slots = new();

    public AmmoPacksService(
        ISwiftlyCore core,
        ILogger<AmmoPacksService> logger,
        HZPGlobals globals,
        IOptionsMonitor<HZPMainCFG> mainCFG,
        IOptionsMonitor<HZPExtraItemsCFG> extraItemsCFG,
        AmmoPacksBackendResolver backendResolver)
    {
        _core = core;
        _logger = logger;
        _globals = globals;
        _mainCFG = mainCFG;
        _extraItemsCFG = extraItemsCFG;
        _backendResolver = backendResolver;
    }

    public int GetBalance(int playerId)
    {
        if (_slots.TryGetValue(playerId, out var slot))
            return slot.Balance;

        _globals.AmmoPacks.TryGetValue(playerId, out int ap);
        return ap;
    }

    public void SetBalance(int playerId, int amount, bool persist = true)
    {
        var slot = GetOrCreateSlot(playerId);
        int clamped = Math.Max(0, amount);
        if (slot.Balance == clamped)
            return;

        int delta = clamped - slot.Balance;
        slot.Balance = clamped;
        if (!slot.Loaded)
            slot.PendingDelta += delta;

        _globals.AmmoPacks[playerId] = slot.Balance;

        if (persist)
            TryPersist(playerId, slot);
    }

    public void AddBalance(int playerId, int amount, bool persist = true)
    {
        if (amount == 0) return;
        SetBalance(playerId, GetBalance(playerId) + amount, persist);
    }

    public bool SpendBalance(int playerId, int cost, bool persist = true)
    {
        if (cost <= 0) return true;
        int current = GetBalance(playerId);
        if (current < cost)
            return false;

        SetBalance(playerId, current - cost, persist);
        return true;
    }

    public void OnClientConnected(int playerId)
    {
        var slot = GetOrCreateSlot(playerId);
        slot.Generation++;
        slot.SteamId = 0;
        slot.Balance = 0;
        slot.Loaded = false;
        slot.PendingDelta = 0;
        slot.LoadInProgress = false;

        _globals.AmmoPacks[playerId] = 0;
        _globals.AmmoPacksLoaded.Remove(playerId);
        _globals.PlayerSteamIdCache.Remove(playerId);

        StartLoad(playerId, slot.Generation, 24, 0.5f);
    }

    public void OnPlayerSteamIdObserved(int playerId, ulong steamId)
    {
        if (steamId == 0)
            return;

        var slot = GetOrCreateSlot(playerId);
        slot.SteamId = steamId;
        _globals.PlayerSteamIdCache[playerId] = steamId;

        if (!_mainCFG.CurrentValue.AmmoPacksEnabled || slot.Loaded || slot.LoadInProgress)
            return;

        StartLoad(playerId, slot.Generation, 8, 0.5f);
    }

    public void OnClientDisconnected(int playerId)
    {
        if (!_slots.TryGetValue(playerId, out var slot))
        {
            _globals.AmmoPacks.Remove(playerId);
            _globals.AmmoPacksLoaded.Remove(playerId);
            _globals.PlayerSteamIdCache.Remove(playerId);
            return;
        }

        // If this slot is already occupied, this disconnect is stale.
        var now = _core.PlayerManager.GetPlayer(playerId);
        if (now.IsValid && !now.IsFakeClient && now.SteamID != 0)
            return;

        TryPersist(playerId, slot);

        _slots.Remove(playerId);
        _globals.AmmoPacks.Remove(playerId);
        _globals.AmmoPacksLoaded.Remove(playerId);
        _globals.PlayerSteamIdCache.Remove(playerId);
    }

    public async Task SaveAllConnectedPlayersAsync()
    {
        if (!_mainCFG.CurrentValue.AmmoPacksEnabled)
            return;

        var batch = new List<(ulong steamId, int ammoPacks)>();
        foreach (var player in _core.PlayerManager.GetAllPlayers())
        {
            if (!player!.IsValid || player.IsFakeClient)
                continue;

            if (!_slots.TryGetValue(player.PlayerID, out var slot) || !slot.Loaded)
                continue;

            ulong steamId = slot.SteamId != 0 ? slot.SteamId : player.SteamID;
            if (steamId == 0)
                continue;

            batch.Add((steamId, slot.Balance));
        }

        if (batch.Count > 0)
            await _backendResolver.Active.SaveAllAsync(batch);
    }

    private SlotState GetOrCreateSlot(int playerId)
    {
        if (_slots.TryGetValue(playerId, out var slot))
            return slot;

        slot = new SlotState();
        _slots[playerId] = slot;
        return slot;
    }

    private void StartLoad(int playerId, int generation, int attemptsLeft, float retrySeconds)
    {
        if (!_slots.TryGetValue(playerId, out var slot))
            return;

        if (slot.LoadInProgress)
            return;

        slot.LoadInProgress = true;
        _ = LoadLoopAsync(playerId, generation, attemptsLeft, retrySeconds);
    }

    private async Task LoadLoopAsync(int playerId, int generation, int attemptsLeft, float retrySeconds)
    {
        if (!_slots.TryGetValue(playerId, out var slot) || slot.Generation != generation)
            return;

        try
        {
            // Resolve steamid for this slot.
            ulong steamId = slot.SteamId;
            if (steamId == 0)
            {
                var player = _core.PlayerManager.GetPlayer(playerId);
                if (player!.IsValid && !player.IsFakeClient && player.SteamID != 0)
                {
                    steamId = player.SteamID;
                    slot.SteamId = steamId;
                    _globals.PlayerSteamIdCache[playerId] = steamId;
                }
                else if (_globals.PlayerSteamIdCache.TryGetValue(playerId, out ulong cached) && cached != 0)
                {
                    steamId = cached;
                    slot.SteamId = steamId;
                }
            }

            if (!_mainCFG.CurrentValue.AmmoPacksEnabled)
            {
                int starting = Math.Max(0, _extraItemsCFG.CurrentValue.StartingAmmoPacks);
                slot.Balance = Math.Max(slot.Balance, starting);
                slot.Loaded = true;
                slot.PendingDelta = 0;
                _globals.AmmoPacks[playerId] = slot.Balance;
                _globals.AmmoPacksLoaded.Add(playerId);
                return;
            }

            if (steamId == 0)
            {
                if (attemptsLeft > 0)
                {
                    slot.LoadInProgress = false;
                    _core.Scheduler.DelayBySeconds(retrySeconds, () => StartLoad(playerId, generation, attemptsLeft - 1, retrySeconds));
                    return;
                }

                // SteamID unavailable; keep local balance but mark loaded to avoid deadlock in gameplay.
                slot.Loaded = true;
                _globals.AmmoPacksLoaded.Add(playerId);
                return;
            }

            int? loaded = null;
            try
            {
                loaded = await _backendResolver.Active.LoadAsync(steamId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[HZP-AP] LoadAsync failed for steamid={SteamId}: {Ex}", steamId, ex.Message);
            }

            // Backend unavailable: retry.
            if (!loaded.HasValue && attemptsLeft > 0)
            {
                slot.LoadInProgress = false;
                _core.Scheduler.DelayBySeconds(retrySeconds, () => StartLoad(playerId, generation, attemptsLeft - 1, retrySeconds));
                return;
            }

            int startingAp = Math.Max(0, _extraItemsCFG.CurrentValue.StartingAmmoPacks);
            int baseBalance = loaded ?? startingAp;
            int finalBalance = Math.Max(0, baseBalance + slot.PendingDelta);

            slot.Balance = finalBalance;
            slot.Loaded = true;
            slot.PendingDelta = 0;
            _globals.AmmoPacks[playerId] = finalBalance;
            _globals.AmmoPacksLoaded.Add(playerId);

            // Ensure backend has the resolved balance (new player row/wallet etc).
            TryPersist(playerId, slot);
        }
        finally
        {
            if (_slots.TryGetValue(playerId, out var s) && s.Generation == generation)
                s.LoadInProgress = false;
        }
    }

    private void TryPersist(int playerId, SlotState slot)
    {
        if (!_mainCFG.CurrentValue.AmmoPacksEnabled)
            return;
        if (!slot.Loaded)
            return;

        ulong steamId = slot.SteamId;
        if (steamId == 0)
            _globals.PlayerSteamIdCache.TryGetValue(playerId, out steamId);

        if (steamId == 0)
        {
            var player = _core.PlayerManager.GetPlayer(playerId);
            if (player.IsValid && !player.IsFakeClient)
                steamId = player.SteamID;
        }

        if (steamId == 0)
            return;

        slot.SteamId = steamId;
        _globals.PlayerSteamIdCache[playerId] = steamId;
        _ = _backendResolver.Active.SaveAsync(steamId, slot.Balance);
    }
}

