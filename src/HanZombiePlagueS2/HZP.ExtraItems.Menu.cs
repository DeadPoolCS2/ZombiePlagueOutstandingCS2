using System.Drawing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
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

    public HZPExtraItemsMenu(
        ISwiftlyCore core,
        ILogger<HZPExtraItemsMenu> logger,
        HZPGlobals globals,
        HZPHelpers helpers,
        HZPMenuHelper menuHelper,
        IOptionsMonitor<HZPExtraItemsCFG> extraItemsCFG,
        IOptionsMonitor<HZPMainCFG> mainCFG)
    {
        _core = core;
        _logger = logger;
        _globals = globals;
        _helpers = helpers;
        _menuHelper = menuHelper;
        _extraItemsCFG = extraItemsCFG;
        _mainCFG = mainCFG;
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
        return isNemesis || isAssassin;
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
}
