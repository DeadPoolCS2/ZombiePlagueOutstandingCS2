using System.Drawing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Helpers;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using DrawingColor = System.Drawing.Color;

namespace HanZombiePlagueS2;

public class HZPExtraItemsMenu
{
    private readonly ILogger<HZPExtraItemsMenu> _logger;
    private readonly ISwiftlyCore _core;
    private readonly HZPGlobals _globals;
    private readonly HZPHelpers _helpers;
    private readonly HZPMenuHelper _menuHelper;
    private readonly IOptionsMonitor<HZPExtraItemsCFG> _extraItemsCFG;
    private readonly IOptionsMonitor<HZPMainCFG> _mainCFG;
    private readonly IOptionsMonitor<HanMineS2CFG> _mineCFG;

    public HZPExtraItemsMenu(
        ISwiftlyCore core,
        ILogger<HZPExtraItemsMenu> logger,
        HZPGlobals globals,
        HZPHelpers helpers,
        HZPMenuHelper menuHelper,
        IOptionsMonitor<HZPExtraItemsCFG> extraItemsCFG,
        IOptionsMonitor<HZPMainCFG> mainCFG,
        IOptionsMonitor<HanMineS2CFG> mineCFG)
    {
        _core = core;
        _logger = logger;
        _globals = globals;
        _helpers = helpers;
        _menuHelper = menuHelper;
        _extraItemsCFG = extraItemsCFG;
        _mainCFG = mainCFG;
        _mineCFG = mineCFG;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Ammo-pack helpers
    // ─────────────────────────────────────────────────────────────────────────

    public int GetAmmoPacks(int playerId)
    {
        _globals.AmmoPacks.TryGetValue(playerId, out int ap);
        return ap;
    }

    public void SetAmmoPacks(int playerId, int amount)
    {
        _globals.AmmoPacks[playerId] = Math.Max(0, amount);
    }

    public bool SpendAmmoPacks(int playerId, int cost)
    {
        int current = GetAmmoPacks(playerId);
        if (current < cost) return false;
        SetAmmoPacks(playerId, current - cost);
        return true;
    }

    public void AddAmmoPacks(int playerId, int amount)
    {
        int current = GetAmmoPacks(playerId);
        SetAmmoPacks(playerId, current + amount);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Team / eligibility helpers
    // ─────────────────────────────────────────────────────────────────────────

    private bool IsZombie(int playerId)
    {
        _globals.IsZombie.TryGetValue(playerId, out bool v);
        return v;
    }

    private bool IsSpecialRole(int playerId)
    {
        _globals.IsNemesis.TryGetValue(playerId, out bool isNemesis);
        _globals.IsAssassin.TryGetValue(playerId, out bool isAssassin);
        _globals.IsSurvivor.TryGetValue(playerId, out bool isSurvivor);
        _globals.IsSniper.TryGetValue(playerId, out bool isSniper);
        _globals.IsHero.TryGetValue(playerId, out bool isHero);
        return isNemesis || isAssassin || isSurvivor || isSniper || isHero;
    }

    /// <summary>Parses a "R,G,B,A" string (0–255 each) into a SwiftlyS2 Color.</summary>
    private static SwiftlyS2.Shared.Natives.Color ParseColor(string rgba, byte defaultR, byte defaultG, byte defaultB, byte defaultA)
    {
        try
        {
            var parts = rgba.Split(',');
            if (parts.Length >= 4)
                return new SwiftlyS2.Shared.Natives.Color(
                    byte.Parse(parts[0].Trim()),
                    byte.Parse(parts[1].Trim()),
                    byte.Parse(parts[2].Trim()),
                    byte.Parse(parts[3].Trim()));
        }
        catch { }
        return new SwiftlyS2.Shared.Natives.Color(defaultR, defaultG, defaultB, defaultA);
    }

    private bool ItemAllowedForPlayer(ExtraItemEntry item, int playerId)
    {
        bool zombie = IsZombie(playerId);
        return item.Team switch
        {
            ExtraItemTeam.Human => !zombie,
            ExtraItemTeam.Zombie => zombie,
            ExtraItemTeam.Both => true,
            _ => false
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Menu
    // ─────────────────────────────────────────────────────────────────────────

    public void OpenExtraItemsMenu(IPlayer player)
    {
        if (player == null || !player.IsValid) return;

        var controller = player.Controller;
        if (controller == null || !controller.IsValid || !controller.PawnIsAlive)
        {
            player.SendMessage(MessageType.Chat, _helpers.T(player, "ExtraItemsMustBeAlive"));
            return;
        }

        if (!_globals.GameStart)
        {
            player.SendMessage(MessageType.Chat, _helpers.T(player, "ExtraItemsRoundNotActive"));
            return;
        }

        var cfg = _extraItemsCFG.CurrentValue;
        int playerId = player.PlayerID;
        int ap = GetAmmoPacks(playerId);

        IMenuAPI menu = _menuHelper.CreateMenu(_helpers.T(player, "ExtraItemsMenuTitle"));

        menu.AddOption(new TextMenuOption(
            HtmlGradient.GenerateGradientText(
                string.Format(_helpers.T(player, "ExtraItemsMenuAP"), ap),
                DrawingColor.Gold, DrawingColor.LightGoldenrodYellow, DrawingColor.Gold),
            updateIntervalMs: 800, pauseIntervalMs: 100)
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop
        });

        bool anyVisible = false;
        foreach (var item in cfg.Items)
        {
            if (!item.Enable) continue;
            if (!ItemAllowedForPlayer(item, playerId)) continue;

            anyVisible = true;
            string label = $"{item.Name}  [{item.Price} AP]";

            var btn = new ButtonMenuOption(label)
            {
                TextStyle = MenuOptionTextStyle.ScrollLeftLoop,
                CloseAfterClick = false
            };
            btn.Tag = "extend";

            // Capture loop variables
            var capturedItem = item;

            btn.Click += async (_, args) =>
            {
                var clicker = args.Player;
                _core.Scheduler.NextTick(() => HandleItemPurchase(clicker, capturedItem));
            };

            menu.AddOption(btn);
        }

        if (!anyVisible)
        {
            menu.AddOption(new TextMenuOption(_helpers.T(player, "ExtraItemsNoneAvailable")));
        }

        _core.MenusAPI.OpenMenuForPlayer(player, menu);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Purchase handling
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleItemPurchase(IPlayer player, ExtraItemEntry item)
    {
        if (player == null || !player.IsValid) return;

        var controller = player.Controller;
        if (controller == null || !controller.IsValid || !controller.PawnIsAlive)
        {
            player.SendMessage(MessageType.Chat, _helpers.T(player, "ExtraItemsMustBeAlive"));
            return;
        }

        if (!_globals.GameStart)
        {
            player.SendMessage(MessageType.Chat, _helpers.T(player, "ExtraItemsRoundNotActive"));
            return;
        }

        int playerId = player.PlayerID;

        if (!ItemAllowedForPlayer(item, playerId))
        {
            player.SendMessage(MessageType.Chat, _helpers.T(player, "ExtraItemsWrongTeam"));
            return;
        }

        int ap = GetAmmoPacks(playerId);
        if (ap < item.Price)
        {
            player.SendMessage(MessageType.Chat,
                string.Format(_helpers.T(player, "ExtraItemsNotEnoughAP"), item.Price, ap));
            return;
        }

        if (!SpendAmmoPacks(playerId, item.Price))
        {
            player.SendMessage(MessageType.Chat,
                string.Format(_helpers.T(player, "ExtraItemsNotEnoughAP"), item.Price, ap));
            return;
        }

        int newAp = GetAmmoPacks(playerId);

        switch (item.Key)
        {
            case "armor":
                ApplyArmor(player, newAp);
                break;
            case "he_grenade":
                ApplyHEGrenade(player, newAp);
                break;
            case "flash_grenade":
                ApplyFlashGrenade(player, newAp);
                break;
            case "smoke_grenade":
                ApplySmokeGrenade(player, newAp);
                break;
            case "antidote":
                ApplyAntidote(player, newAp);
                break;
            case "zombie_madness":
                ApplyZombieMadness(player, newAp);
                break;
            case "multijump":
                ApplyMultijump(player, newAp);
                break;
            case "knife_blink":
                ApplyKnifeBlink(player, newAp);
                break;
            case "jetpack":
                ApplyJetpack(player, newAp);
                break;
            case "trip_mine":
                ApplyTripMine(player, newAp);
                break;
            case "revive_token":
                ApplyReviveToken(player, newAp);
                break;
            default:
                // Unknown item – refund
                AddAmmoPacks(playerId, item.Price);
                _logger.LogWarning("[HZPExtraItems] Unknown item key: {Key}", item.Key);
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Item effects
    // ─────────────────────────────────────────────────────────────────────────

    private void ApplyArmor(IPlayer player, int remainingAP)
    {
        var cfg = _extraItemsCFG.CurrentValue;
        var pawn = player.PlayerPawn;
        if (pawn == null || !pawn.IsValid) return;

        pawn.ArmorValue = cfg.ArmorAmount;
        pawn.ArmorValueUpdated();

        player.SendMessage(MessageType.Chat,
            string.Format(_helpers.T(player, "ExtraItemsArmorSuccess"), cfg.ArmorAmount, remainingAP));
    }

    private void ApplyHEGrenade(IPlayer player, int remainingAP)
    {
        _helpers.GiveFireGrenade(player);
        player.SendMessage(MessageType.Chat,
            string.Format(_helpers.T(player, "ExtraItemsGrenadeSuccess"),
                _helpers.T(player, "ItemFireGrenade"), remainingAP));
    }

    private void ApplyFlashGrenade(IPlayer player, int remainingAP)
    {
        _helpers.GiveLightGrenade(player);
        player.SendMessage(MessageType.Chat,
            string.Format(_helpers.T(player, "ExtraItemsGrenadeSuccess"),
                _helpers.T(player, "ItemLightGrenade"), remainingAP));
    }

    private void ApplySmokeGrenade(IPlayer player, int remainingAP)
    {
        _helpers.GiveFreezeGrenade(player);
        player.SendMessage(MessageType.Chat,
            string.Format(_helpers.T(player, "ExtraItemsGrenadeSuccess"),
                _helpers.T(player, "ItemFreezeGrenade"), remainingAP));
    }

    private void ApplyAntidote(IPlayer player, int remainingAP)
    {
        int playerId = player.PlayerID;
        if (!IsZombie(playerId))
        {
            AddAmmoPacks(playerId, _extraItemsCFG.CurrentValue.Items
                .FirstOrDefault(i => i.Key == "antidote")?.Price ?? 0);
            player.SendMessage(MessageType.Chat, _helpers.T(player, "ExtraItemsAntidoteNotZombie"));
            return;
        }

        if (IsSpecialRole(playerId))
        {
            AddAmmoPacks(playerId, _extraItemsCFG.CurrentValue.Items
                .FirstOrDefault(i => i.Key == "antidote")?.Price ?? 0);
            player.SendMessage(MessageType.Chat, _helpers.T(player, "ItemTVaccineError"));
            return;
        }

        var mainCFG = _mainCFG.CurrentValue;
        string defaultModel = string.IsNullOrEmpty(mainCFG.HumandefaultModel)
            ? "characters/models/ctm_st6/ctm_st6_variante.vmdl"
            : mainCFG.HumandefaultModel;

        _helpers.TVaccine(player, mainCFG.HumanMaxHealth, mainCFG.HumanInitialSpeed,
            defaultModel, mainCFG.TVaccineSound, 1.0f);

        player.SendMessage(MessageType.Chat,
            string.Format(_helpers.T(player, "ExtraItemsAntidoteSuccess"), remainingAP));
        _core.PlayerManager.SendMessage(MessageType.Chat,
            string.Format(_helpers.T(player, "ExtraItemsAntidoteSuccessToAll"), player.Name));
    }

    private void ApplyZombieMadness(IPlayer player, int remainingAP)
    {
        int playerId = player.PlayerID;
        if (!IsZombie(playerId))
        {
            AddAmmoPacks(playerId, _extraItemsCFG.CurrentValue.Items
                .FirstOrDefault(i => i.Key == "zombie_madness")?.Price ?? 0);
            player.SendMessage(MessageType.Chat, _helpers.T(player, "ItemHumanCantUse"));
            return;
        }

        _globals.ZombieMadnessActive.TryGetValue(playerId, out bool alreadyActive);
        if (alreadyActive)
        {
            AddAmmoPacks(playerId, _extraItemsCFG.CurrentValue.Items
                .FirstOrDefault(i => i.Key == "zombie_madness")?.Price ?? 0);
            player.SendMessage(MessageType.Chat, _helpers.T(player, "ExtraItemsMadnessAlready"));
            return;
        }

        float duration = _extraItemsCFG.CurrentValue.MadnessDuration;
        _globals.ZombieMadnessActive[playerId] = true;

        player.SendMessage(MessageType.Chat,
            string.Format(_helpers.T(player, "ExtraItemsMadnessSuccess"), duration, remainingAP));

        _core.Scheduler.DelayBySeconds(duration, () =>
        {
            if (player == null || !player.IsValid) return;
            _globals.ZombieMadnessActive[playerId] = false;
            player.SendMessage(MessageType.Chat, _helpers.T(player, "ExtraItemsMadnessEnd"));
        });
    }

    private void ApplyMultijump(IPlayer player, int remainingAP)
    {
        int playerId = player.PlayerID;
        var cfg = _extraItemsCFG.CurrentValue;

        _globals.ExtraJumps.TryGetValue(playerId, out int currentExtra);
        if (currentExtra >= cfg.MultijumpMax)
        {
            AddAmmoPacks(playerId, cfg.Items.FirstOrDefault(i => i.Key == "multijump")?.Price ?? 0);
            player.SendMessage(MessageType.Chat,
                string.Format(_helpers.T(player, "ExtraItemsMultijumpMax"), cfg.MultijumpMax));
            return;
        }

        int newExtra = Math.Min(currentExtra + cfg.MultijumpIncrement, cfg.MultijumpMax);
        _globals.ExtraJumps[playerId] = newExtra;

        player.SendMessage(MessageType.Chat,
            string.Format(_helpers.T(player, "ExtraItemsMultijumpSuccess"), newExtra, remainingAP));
    }

    private void ApplyKnifeBlink(IPlayer player, int remainingAP)
    {
        int playerId = player.PlayerID;
        var cfg = _extraItemsCFG.CurrentValue;

        _globals.KnifeBlinkCharges.TryGetValue(playerId, out int currentCharges);
        int newCharges = currentCharges + cfg.KnifeBlinkCharges;
        _globals.KnifeBlinkCharges[playerId] = newCharges;

        player.SendMessage(MessageType.Chat,
            string.Format(_helpers.T(player, "ExtraItemsKnifeBlinkSuccess"), cfg.KnifeBlinkCharges, newCharges, remainingAP));
    }

    private void ApplyJetpack(IPlayer player, int remainingAP)
    {
        int playerId = player.PlayerID;
        var cfg = _extraItemsCFG.CurrentValue;

        _globals.HasJetpack.TryGetValue(playerId, out bool alreadyHas);
        if (alreadyHas)
        {
            // Refuel instead of blocking purchase
            _globals.JetpackFuel[playerId] = cfg.JetpackMaxFuel;
            player.SendMessage(MessageType.Chat,
                string.Format(_helpers.T(player, "ExtraItemsJetpackRefueled"), cfg.JetpackMaxFuel, remainingAP));
            return;
        }

        _globals.HasJetpack[playerId] = true;
        _globals.JetpackFuel[playerId] = cfg.JetpackMaxFuel;
        _globals.JetpackLastFuelTime[playerId] = 0f;
        _globals.JetpackRocketCooldownEnd[playerId] = 0f;

        player.SendMessage(MessageType.Chat,
            string.Format(_helpers.T(player, "ExtraItemsJetpackSuccess"), cfg.JetpackMaxFuel, remainingAP));
    }

    private void ApplyTripMine(IPlayer player, int remainingAP)
    {
        int playerId = player.PlayerID;

        _globals.TripMineCharges.TryGetValue(playerId, out int currentCharges);
        int newCharges = currentCharges + 1;
        _globals.TripMineCharges[playerId] = newCharges;

        player.SendMessage(MessageType.Chat,
            string.Format(_helpers.T(player, "ExtraItemsTripMineSuccess"), newCharges, remainingAP));
    }

    private void ApplyReviveToken(IPlayer player, int remainingAP)
    {
        int playerId = player.PlayerID;

        _globals.HasReviveToken.TryGetValue(playerId, out bool alreadyHas);
        if (alreadyHas)
        {
            AddAmmoPacks(playerId, _extraItemsCFG.CurrentValue.Items
                .FirstOrDefault(i => i.Key == "revive_token")?.Price ?? 0);
            player.SendMessage(MessageType.Chat, _helpers.T(player, "ExtraItemsReviveTokenAlready"));
            return;
        }

        _globals.HasReviveToken[playerId] = true;

        player.SendMessage(MessageType.Chat,
            string.Format(_helpers.T(player, "ExtraItemsReviveTokenSuccess"), remainingAP));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Knife-blink execution (called from weapon-fire hook or OnTick)
    // ─────────────────────────────────────────────────────────────────────────

    public void TryExecuteKnifeBlink(IPlayer player)
    {
        if (player == null || !player.IsValid) return;

        var controller = player.Controller;
        if (controller == null || !controller.IsValid || !controller.PawnIsAlive) return;

        int playerId = player.PlayerID;

        _globals.KnifeBlinkCharges.TryGetValue(playerId, out int charges);
        if (charges <= 0) return;

        // Use Environment.TickCount64 (ms) for cooldown tracking
        long nowMs = Environment.TickCount64;
        _globals.KnifeBlinkCooldownEnd.TryGetValue(playerId, out long cooldownEndMs);
        if (nowMs < cooldownEndMs) return;

        var cfg = _extraItemsCFG.CurrentValue;
        var pawn = player.PlayerPawn;
        if (pawn == null || !pawn.IsValid) return;

        var origin = pawn.AbsOrigin;
        if (origin == null) return;

        // Compute destination: eye forward direction * distance
        QAngle eyeAngles = pawn.EyeAngles;
        float yawRad = eyeAngles.Y * MathF.PI / 180f;
        float pitchRad = eyeAngles.X * MathF.PI / 180f;

        float cosPitch = MathF.Cos(pitchRad);
        float fwdX = cosPitch * MathF.Cos(yawRad);
        float fwdY = cosPitch * MathF.Sin(yawRad);
        float fwdZ = -MathF.Sin(pitchRad);

        var dest = new Vector(
            origin.Value.X + fwdX * cfg.KnifeBlinkDistance,
            origin.Value.Y + fwdY * cfg.KnifeBlinkDistance,
            origin.Value.Z + fwdZ * cfg.KnifeBlinkDistance
        );

        pawn.Teleport(dest, eyeAngles, Vector.Zero);

        _globals.KnifeBlinkCharges[playerId] = charges - 1;
        _globals.KnifeBlinkCooldownEnd[playerId] = nowMs + (long)(cfg.KnifeBlinkCooldown * 1000);

        player.SendMessage(MessageType.Chat,
            string.Format(_helpers.T(player, "ExtraItemsKnifeBlinkUsed"),
                charges - 1, cfg.KnifeBlinkCooldown));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Jetpack – thrust & rocket (called from OnTick)
    // ─────────────────────────────────────────────────────────────────────────

    // Named constants for internal physics values
    private const float PlayerEyeHeight   = 64f;   // eye is ~64 units above AbsOrigin
    private const float MaxDeltaTime      = 0.15f; // clamp dt to avoid large jumps
    private const float DefaultDeltaTime  = 0.05f; // fallback dt on first tick / long gap
    private const float ForwardPushMultiplier = 0.4f; // fraction of thrust applied horizontally

    public void TryExecuteJetpackThrust(IPlayer player)
    {
        if (player == null || !player.IsValid) return;

        int id = player.PlayerID;
        if (!_globals.HasJetpack.TryGetValue(id, out bool hasJetpack) || !hasJetpack) return;

        var pawn = player.PlayerPawn;
        if (pawn == null || !pawn.IsValid) return;

        bool duckPressed  = (player.PressedButtons & GameButtonFlags.Ctrl)  != 0;
        bool spacePressed = (player.PressedButtons & GameButtonFlags.Space) != 0;

        if (!duckPressed || !spacePressed) return;

        _globals.JetpackFuel.TryGetValue(id, out float fuel);
        if (fuel <= 0) return;

        float now = _core.Engine.GlobalVars.CurrentTime;
        _globals.JetpackLastFuelTime.TryGetValue(id, out float lastTime);

        float dt = now - lastTime;
        if (dt <= 0f || dt > MaxDeltaTime) dt = DefaultDeltaTime; // clamp: first tick / long gap

        var cfg = _extraItemsCFG.CurrentValue;
        float fuelUsed = cfg.JetpackFuelConsumeRate * dt;
        _globals.JetpackFuel[id] = Math.Max(0f, fuel - fuelUsed);
        _globals.JetpackLastFuelTime[id] = now;

        // Apply thrust: fixed upward velocity + gentle forward push
        QAngle eyeAngles = pawn.EyeAngles;
        eyeAngles.ToDirectionVectors(out Vector fwd, out _, out _);

        var vel = pawn.AbsVelocity;
        float force = cfg.JetpackThrustForce;
        float fwdPush = force * ForwardPushMultiplier * dt;

        var newVel = new Vector(
            vel.X + fwd.X * fwdPush,
            vel.Y + fwd.Y * fwdPush,
            force  // fixed upward velocity override (counters gravity + lifts)
        );

        pawn.Teleport(null, null, newVel);
    }

    public void TryFireJetpackRocket(IPlayer player)
    {
        if (player == null || !player.IsValid) return;

        int id = player.PlayerID;
        if (!_globals.HasJetpack.TryGetValue(id, out bool hasJetpack) || !hasJetpack) return;

        // Rising-edge detection for Mouse2 (right-click)
        bool attack2Now = (player.PressedButtons & GameButtonFlags.Mouse2) != 0;
        _globals.PrevAttack2Pressed.TryGetValue(id, out bool prevAttack2);
        _globals.PrevAttack2Pressed[id] = attack2Now;

        if (!attack2Now || prevAttack2) return; // only on fresh press

        float now = _core.Engine.GlobalVars.CurrentTime;
        _globals.JetpackRocketCooldownEnd.TryGetValue(id, out float cooldownEnd);
        if (now < cooldownEnd) return;

        var cfg = _extraItemsCFG.CurrentValue;
        _globals.JetpackRocketCooldownEnd[id] = now + cfg.JetpackRocketCooldown;

        var pawn = player.PlayerPawn;
        if (pawn == null || !pawn.IsValid) return;

        var origin = pawn.AbsOrigin;
        if (origin == null) return;

        QAngle eyeAngles = pawn.EyeAngles;
        eyeAngles.ToDirectionVectors(out Vector fwd, out _, out _);

        // Eye position (player eye is above AbsOrigin by PlayerEyeHeight)
        var eyePos = new Vector(origin.Value.X, origin.Value.Y, origin.Value.Z + PlayerEyeHeight);

        var impactPos = new Vector(
            eyePos.X + fwd.X * cfg.JetpackRocketRange,
            eyePos.Y + fwd.Y * cfg.JetpackRocketRange,
            eyePos.Z + fwd.Z * cfg.JetpackRocketRange
        );

        // Simulate rocket flight then explode
        _core.Scheduler.DelayBySeconds(cfg.JetpackRocketFlightTime, () =>
        {
            ExplodeJetpackRocket(player, id, impactPos, cfg.JetpackRocketDamage, cfg.JetpackRocketRadius);
        });

        player.SendMessage(MessageType.Chat, _helpers.T(player, "ExtraItemsJetpackRocketFired"));
    }

    private void ExplodeJetpackRocket(IPlayer shooter, int shooterId, Vector pos, int damage, float radius)
    {
        if (!_globals.GameStart) return;

        float radiusSqr = radius * radius;
        foreach (var target in _core.PlayerManager.GetAlive())
        {
            if (target == null || !target.IsValid) continue;

            _globals.IsZombie.TryGetValue(target.PlayerID, out bool isZombie);
            if (!isZombie) continue;

            var targetPawn = target.PlayerPawn;
            if (targetPawn?.IsValid != true) continue;

            var targetPos = targetPawn.AbsOrigin;
            if (targetPos == null) continue;

            float distSqr = _helpers.DistanceSquared(pos, targetPos.Value);
            if (distSqr > radiusSqr) continue;

            float dist = MathF.Sqrt(distSqr);
            float falloff = radius > 0f ? 1f - (dist / radius) : 1f;
            float actualDmg = damage * Math.Max(0.1f, falloff);

            var shooterNow = _core.PlayerManager.GetPlayer(shooterId);
            if (shooterNow != null && shooterNow.IsValid)
                _helpers.ApplyDamage(shooterNow, target, actualDmg, DamageTypes_t.DMG_BLAST);
        }

        _helpers.DrawExpandingRing(pos, radius, 255, 120, 0, 180, 0.3f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Trip Mine – plant, take & tick check
    // ─────────────────────────────────────────────────────────────────────────

    public void TryPlantTripMine(IPlayer player)
    {
        if (player == null || !player.IsValid) return;

        var controller = player.Controller;
        if (controller == null || !controller.IsValid || !controller.PawnIsAlive) return;

        if (!_globals.GameStart) return;

        int id = player.PlayerID;

        _globals.IsZombie.TryGetValue(id, out bool isZombie);
        if (isZombie)
        {
            player.SendMessage(MessageType.Chat, _helpers.T(player, "ItemZombieCantUse"));
            return;
        }

        if (IsSpecialRole(id))
        {
            player.SendMessage(MessageType.Chat, _helpers.T(player, "ItemSpecialRoleCantUse"));
            return;
        }

        _globals.TripMineCharges.TryGetValue(id, out int charges);
        if (charges <= 0)
        {
            player.SendMessage(MessageType.Chat, _helpers.T(player, "TripMineNoCharges"));
            return;
        }

        var mineCfg = _mineCFG.CurrentValue;
        var cfg = _extraItemsCFG.CurrentValue;

        int limit = mineCfg.Limit > 0 ? mineCfg.Limit : cfg.TripMineMaxPerPlayer;
        int activeMines = _globals.AllMines.Count(m => m.OwnerId == id && !m.Exploded);
        if (activeMines >= limit)
        {
            player.SendMessage(MessageType.Chat,
                string.Format(_helpers.T(player, "TripMineMaxReached"), limit));
            return;
        }

        var pawn = player.PlayerPawn;
        if (pawn == null || !pawn.IsValid) return;

        var origin = pawn.AbsOrigin;
        if (origin == null) return;

        QAngle angles = pawn.EyeAngles;
        angles.ToDirectionVectors(out Vector fwd, out _, out _);

        float plantDist = mineCfg.PlantDistance;
        float beamLen   = mineCfg.BeamLength;

        // Place mine at PlantDistance ahead of eye (simulates placing on a wall)
        var minePos = new Vector(
            origin.Value.X + fwd.X * plantDist,
            origin.Value.Y + fwd.Y * plantDist,
            origin.Value.Z + PlayerEyeHeight + fwd.Z * plantDist
        );

        // Laser beam extends further in the same forward direction
        var beamEnd = new Vector(
            minePos.X + fwd.X * beamLen,
            minePos.Y + fwd.Y * beamLen,
            minePos.Z + fwd.Z * beamLen
        );

        _globals.TripMineCharges[id] = charges - 1;

        var mine = new TripMineData
        {
            OwnerId      = id,
            MinePosition = minePos,
            BeamEnd      = beamEnd,
            Health       = mineCfg.MineHealth > 0 ? mineCfg.MineHealth : cfg.TripMineHealth,
            Exploded     = false
        };
        _globals.AllMines.Add(mine);

        // Parse configurable colors (defaults: green beam, green glow)
        var beamColor = ParseColor(mineCfg.LaserColor, 0, 255, 0, 220);
        if (!float.TryParse(mineCfg.LaserSize, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out float beamWidth))
            beamWidth = 2.5f;

        // Create entities on the next world update
        _core.Scheduler.NextWorldUpdate(() =>
        {
            // Configurable-color laser beam
            var beamEnt = _core.EntitySystem.CreateEntityByDesignerName<CBeam>("beam");
            if (beamEnt != null && beamEnt.IsValid && beamEnt.IsValidEntity)
            {
                beamEnt.Render = beamColor;
                beamEnt.Width = beamWidth;
                beamEnt.HaloScale = 1.5f;
                beamEnt.Teleport(minePos, new QAngle(), new Vector(0, 0, 0));
                beamEnt.EndPos.X = beamEnd.X;
                beamEnt.EndPos.Y = beamEnd.Y;
                beamEnt.EndPos.Z = beamEnd.Z;
                beamEnt.DispatchSpawn();
                mine.Beam = beamEnt;
            }

            // Smoke-trail particle as mine visual
            var particleEnt = _core.EntitySystem.CreateEntityByDesignerName<CParticleSystem>("info_particle_system");
            if (particleEnt != null && particleEnt.IsValid && particleEnt.IsValidEntity)
            {
                particleEnt.StartActive = true;
                particleEnt.EffectName = "particles/survival_fx/danger_trail_spores_world.vpcf";
                particleEnt.AcceptInput("Start", "");
                particleEnt.DispatchSpawn();
                particleEnt.Teleport(minePos, new QAngle(), new Vector(0, 0, 0));
                mine.Visual = particleEnt;
            }
        });

        player.SendMessage(MessageType.Chat,
            string.Format(_helpers.T(player, "TripMinePlanted"), activeMines + 1, limit));
    }

    /// <summary>Recovers the owner's nearest planted mine, returning one charge.</summary>
    public void TryTakeTripMine(IPlayer player)
    {
        if (player == null || !player.IsValid) return;

        var controller = player.Controller;
        if (controller == null || !controller.IsValid || !controller.PawnIsAlive) return;

        if (!_globals.GameStart) return;

        int id = player.PlayerID;

        _globals.IsZombie.TryGetValue(id, out bool isZombie);
        if (isZombie)
        {
            player.SendMessage(MessageType.Chat, _helpers.T(player, "ItemZombieCantUse"));
            return;
        }

        if (IsSpecialRole(id))
        {
            player.SendMessage(MessageType.Chat, _helpers.T(player, "ItemSpecialRoleCantUse"));
            return;
        }

        var mineCfg = _mineCFG.CurrentValue;

        // Find the closest active mine belonging to this player
        var pawn = player.PlayerPawn;
        if (pawn == null || !pawn.IsValid) return;
        var origin = pawn.AbsOrigin;
        if (origin == null) return;

        TripMineData? nearest = null;
        float nearestDistSqr = float.MaxValue;
        foreach (var mine in _globals.AllMines)
        {
            if (mine.Exploded) continue;
            if (mineCfg.OwnerOnlyPickup && mine.OwnerId != id) continue;

            float distSqr = _helpers.DistanceSquared(origin.Value, mine.MinePosition);
            if (distSqr < nearestDistSqr)
            {
                nearestDistSqr = distSqr;
                nearest = mine;
            }
        }

        if (nearest == null)
        {
            player.SendMessage(MessageType.Chat, _helpers.T(player, "TripMineNoneToTake"));
            return;
        }

        // Remove the mine and return the charge
        nearest.Exploded = true;
        DestroyMineEntities(nearest);
        _globals.AllMines.RemoveAll(m => m.Exploded);

        _globals.TripMineCharges.TryGetValue(id, out int currentCharges);
        _globals.TripMineCharges[id] = currentCharges + 1;

        player.SendMessage(MessageType.Chat,
            string.Format(_helpers.T(player, "TripMineTaken"), currentCharges + 1));
    }

    /// <summary>Called every tick from HZPEvents – checks whether any zombie crosses an active beam.</summary>
    public void CheckTripMines()
    {
        if (_globals.AllMines.Count == 0) return;

        var mineCfg = _mineCFG.CurrentValue;
        var cfg = _extraItemsCFG.CurrentValue;
        float tripRadius = mineCfg.TripRadius > 0f ? mineCfg.TripRadius : cfg.TripMineTripRadius;

        bool anyExploded = false;
        foreach (var mine in _globals.AllMines)
        {
            if (mine.Exploded) continue;

            foreach (var player in _core.PlayerManager.GetAlive())
            {
                if (player == null || !player.IsValid) continue;

                _globals.IsZombie.TryGetValue(player.PlayerID, out bool isZombie);
                if (!isZombie) continue;

                var pawn = player.PlayerPawn;
                if (pawn?.IsValid != true) continue;

                var pos = pawn.AbsOrigin;
                if (pos == null) continue;

                float dist = DistPointToSegment(pos.Value, mine.MinePosition, mine.BeamEnd);
                if (dist <= tripRadius)
                {
                    ExplodeTripMine(mine);
                    anyExploded = true;
                    break;
                }
            }
        }

        if (anyExploded)
            _globals.AllMines.RemoveAll(m => m.Exploded);
    }

    private static float DistPointToSegment(Vector p, Vector a, Vector b)
    {
        float dx = b.X - a.X, dy = b.Y - a.Y, dz = b.Z - a.Z;
        float lenSqr = dx * dx + dy * dy + dz * dz;

        if (lenSqr < 0.001f)
        {
            float ex = p.X - a.X, ey = p.Y - a.Y, ez = p.Z - a.Z;
            return MathF.Sqrt(ex * ex + ey * ey + ez * ez);
        }

        float t = Math.Clamp(
            ((p.X - a.X) * dx + (p.Y - a.Y) * dy + (p.Z - a.Z) * dz) / lenSqr,
            0f, 1f);

        float nx = a.X + t * dx - p.X;
        float ny = a.Y + t * dy - p.Y;
        float nz = a.Z + t * dz - p.Z;
        return MathF.Sqrt(nx * nx + ny * ny + nz * nz);
    }

    private void ExplodeTripMine(TripMineData mine)
    {
        if (mine.Exploded) return;
        mine.Exploded = true;

        DestroyMineEntities(mine);

        var mineCfg = _mineCFG.CurrentValue;
        var cfg = _extraItemsCFG.CurrentValue;
        float radius    = mineCfg.ExplorerRadius > 0 ? mineCfg.ExplorerRadius : cfg.TripMineRadius;
        float maxDamage = mineCfg.ExplorerDamage > 0f ? mineCfg.ExplorerDamage : cfg.TripMineDamage;
        float radiusSqr = radius * radius;

        var shooter = _core.PlayerManager.GetPlayer(mine.OwnerId);

        foreach (var target in _core.PlayerManager.GetAlive())
        {
            if (target == null || !target.IsValid) continue;

            _globals.IsZombie.TryGetValue(target.PlayerID, out bool isZombie);
            if (!isZombie) continue;

            var pawn = target.PlayerPawn;
            if (pawn?.IsValid != true) continue;

            var pos = pawn.AbsOrigin;
            if (pos == null) continue;

            float distSqr = _helpers.DistanceSquared(mine.MinePosition, pos.Value);
            if (distSqr > radiusSqr) continue;

            float dist = MathF.Sqrt(distSqr);
            float falloff = radius > 0f ? 1f - (dist / radius) : 1f;
            float dmg = maxDamage * Math.Max(0.1f, falloff);

            if (shooter != null && shooter.IsValid)
                _helpers.ApplyDamage(shooter, target, dmg, DamageTypes_t.DMG_BLAST);
        }

        _helpers.DrawExpandingRing(mine.MinePosition, radius, 255, 80, 0, 200, 0.4f);
    }

    private static void DestroyMineEntities(TripMineData mine)
    {
        if (mine.Beam != null && mine.Beam.IsValid && mine.Beam.IsValidEntity)
            mine.Beam.AcceptInput("Kill", 0);
        if (mine.Visual != null && mine.Visual.IsValid && mine.Visual.IsValidEntity)
            mine.Visual.AcceptInput("Kill", 0);
        mine.Beam   = null;
        mine.Visual = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Cleanup helpers (called from Events on death / disconnect / round-end)
    // ─────────────────────────────────────────────────────────────────────────

    public void CleanupJetpack(int playerId)
    {
        _globals.HasJetpack.Remove(playerId);
        _globals.JetpackFuel.Remove(playerId);
        _globals.JetpackLastFuelTime.Remove(playerId);
        _globals.JetpackRocketCooldownEnd.Remove(playerId);
        _globals.PrevAttack2Pressed.Remove(playerId);
    }

    public void CleanupTripMinesForPlayer(int playerId)
    {
        _globals.TripMineCharges.Remove(playerId);
        var mines = _globals.AllMines.Where(m => m.OwnerId == playerId && !m.Exploded).ToList();
        foreach (var mine in mines)
        {
            DestroyMineEntities(mine);
            mine.Exploded = true;
        }
        _globals.AllMines.RemoveAll(m => m.OwnerId == playerId);
    }

    public void CleanupAllMines()
    {
        foreach (var mine in _globals.AllMines)
        {
            if (!mine.Exploded)
                DestroyMineEntities(mine);
        }
        _globals.AllMines.Clear();
    }
}
