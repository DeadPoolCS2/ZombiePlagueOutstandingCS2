private void Event_OnClientDisconnected(SwiftlyS2.Shared.Events.IOnClientDisconnectedEvent @event)
    {
        if (_globals.GameStart)
        {
            _service.CheckRoundWinConditions();
        }

        var id = @event.PlayerId;
        _ammoPacksLoadInProgress.Remove(id);

        if (_ammoPacksLoadGeneration.TryGetValue(id, out int previousGeneration))
            _ammoPacksLoadGeneration[id] = previousGeneration + 1;
        else
            _ammoPacksLoadGeneration[id] = 1;

        // Defensive guard: if the slot is already occupied again, this disconnect event
        // belongs to an older session and must not wipe the new player's AP/state.
        var player = _core.PlayerManager.GetPlayer(id);
        ulong steamId = 0;
        if (player != null && player.IsValid && !player.IsFakeClient)
            steamId = player.SteamID;

        if (steamId == 0)
            _globals.PlayerSteamIdCache.TryGetValue(id, out steamId);

        // ── Save AP BEFORE removing from memory ──────────────────────────────
        if (steamId != 0)
        {
            var cfg = _mainCFG.CurrentValue;
            if (cfg.AmmoPacksEnabled)
            {
                int currentAP = _extraItemsMenu.GetAmmoPacks(id);
                _ = _backendResolver.Active.SaveAsync(steamId, currentAP);
            }
        }
        // ────────────────────────────────────────────────────────────────────

        _helpers.ClearPlayerBurn(id);
        _globals.IsZombie.Remove(id);
        _globals.IsMother.Remove(id);
        _globals.IsSurvivor.Remove(id);
        _globals.IsSniper.Remove(id);
        _globals.IsNemesis.Remove(id);
        _globals.IsAssassin.Remove(id);
        _globals.IsHero.Remove(id);

        _globals.ScbaSuit.Remove(id);
        _globals.GodState.Remove(id);
        _globals.InfiniteAmmoState.Remove(id);
        _globals.CanBuyWeaponsThisRound.Remove(id);

        _globals.g_ZombieIdleStates.Remove(id);
        _globals.g_ZombieRegenStates.Remove(id);
        _globals.StopZombieTimers.Remove(id);
        _globals.g_IsInvisible.Remove(id);
        _globals.ThrowerIsZombie.Remove(id);

        // Extra items cleanup
        _globals.AmmoPacks.Remove(id);
        _globals.PlayerSteamIdCache.Remove(id);
        _ammoPacksLoadGeneration.Remove(id);
        _globals.DamageAccumulator.Remove(id);
        _globals.ExtraJumps.Remove(id);
        _globals.JumpsUsed.Remove(id);
        _globals.KnifeBlinkCharges.Remove(id);
        _globals.KnifeBlinkCooldownEnd.Remove(id);
        _globals.ZombieMadnessActive.Remove(id);
        _globals.PrevJumpPressed.Remove(id);
        // Jetpack / Trip Mine / Revive Token
        _extraItemsMenu.CleanupJetpack(id);
        _extraItemsMenu.CleanupTripMinesForPlayer(id);
        _globals.HasReviveToken.Remove(id);

        _globals.InSwing[id] = false;

        _core.Scheduler.DelayBySeconds(1.0f, () =>
        {
            var playerCount = _helpers.ServerPlayerCount();
            if (playerCount <= 0 && !_globals.ServerIsEmpty)
            {
                _globals.ServerIsEmpty = true;
                _helpers.restartgame();
            }
        });

        if (player != null && player.IsValid)
        {
            _helpers.RemoveGlow(player);
        }
    }
