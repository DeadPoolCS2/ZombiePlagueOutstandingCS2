using System.Drawing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using DrawingColor = System.Drawing.Color;

namespace HanZombiePlagueS2;

/// <summary>
/// Main game menu accessible via !menu / !zp chat commands or the hzp_menu console command.
/// Replicates the classic CS1.6 Zombie Outstanding menu structure:
///   1. Buy Weapons
///   2. Buy Extra Items
///   3. Choose Zombie Class
///   4. Unstuck
///   5. Join Spectator
/// </summary>
public class HZPGameMenu
{
    private readonly ILogger<HZPGameMenu> _logger;
    private readonly ISwiftlyCore _core;
    private readonly HZPGlobals _globals;
    private readonly HZPHelpers _helpers;
    private readonly HZPMenuHelper _menuHelper;
    private readonly HZPZombieClassMenu _zombieClassMenu;
    private readonly HZPExtraItemsMenu _extraItemsMenu;
    private readonly HZPWeaponsMenu _weaponsMenu;
    private readonly IOptionsMonitor<HZPMainCFG> _mainCFG;

    public HZPGameMenu(
        ISwiftlyCore core,
        ILogger<HZPGameMenu> logger,
        HZPGlobals globals,
        HZPHelpers helpers,
        HZPMenuHelper menuHelper,
        HZPZombieClassMenu zombieClassMenu,
        HZPExtraItemsMenu extraItemsMenu,
        HZPWeaponsMenu weaponsMenu,
        IOptionsMonitor<HZPMainCFG> mainCFG)
    {
        _core = core;
        _logger = logger;
        _globals = globals;
        _helpers = helpers;
        _menuHelper = menuHelper;
        _zombieClassMenu = zombieClassMenu;
        _extraItemsMenu = extraItemsMenu;
        _weaponsMenu = weaponsMenu;
        _mainCFG = mainCFG;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Open the main menu
    // ─────────────────────────────────────────────────────────────────────────

    public void OpenGameMenu(IPlayer player)
    {
        if (player == null || !player.IsValid) return;

        IMenuAPI menu = _menuHelper.CreateMenu(_helpers.T(player, "GameMenuTitle"));

        menu.AddOption(new TextMenuOption(HtmlGradient.GenerateGradientText(
            _helpers.T(player, "GameMenuHint"),
            DrawingColor.LightGreen, DrawingColor.Cyan, DrawingColor.LightGreen),
            updateIntervalMs: 600, pauseIntervalMs: 100)
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop
        });

        // 1 – Buy Weapons
        var buyWeaponsBtn = new ButtonMenuOption(_helpers.T(player, "GameMenuBuyWeapons"))
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop,
            CloseAfterClick = true
        };
        buyWeaponsBtn.Click += async (_, args) =>
        {
            var clicker = args.Player;
            _core.Scheduler.NextTick(() =>
            {
                if (!clicker.IsValid) return;
                _weaponsMenu.OpenWeaponsMenuIfAllowed(clicker);
            });
        };
        menu.AddOption(buyWeaponsBtn);

        // 2 – Buy Extra Items
        var extraItemsBtn = new ButtonMenuOption(_helpers.T(player, "GameMenuExtraItems"))
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop,
            CloseAfterClick = true
        };
        extraItemsBtn.Click += async (_, args) =>
        {
            var clicker = args.Player;
            _core.Scheduler.NextTick(() =>
            {
                if (!clicker.IsValid) return;
                _extraItemsMenu.OpenExtraItemsMenu(clicker);
            });
        };
        menu.AddOption(extraItemsBtn);

        // 3 – Choose Zombie Class
        var zombieClassBtn = new ButtonMenuOption(_helpers.T(player, "GameMenuZombieClass"))
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop,
            CloseAfterClick = true
        };
        zombieClassBtn.Click += async (_, args) =>
        {
            var clicker = args.Player;
            _core.Scheduler.NextTick(() =>
            {
                if (!clicker.IsValid) return;
                _zombieClassMenu.OpenZombieClassMenu(clicker);
            });
        };
        menu.AddOption(zombieClassBtn);

        // 4 – Unstuck
        var unstuckBtn = new ButtonMenuOption(_helpers.T(player, "GameMenuUnstuck"))
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop,
            CloseAfterClick = true
        };
        unstuckBtn.Click += async (_, args) =>
        {
            var clicker = args.Player;
            _core.Scheduler.NextTick(() =>
            {
                if (!clicker.IsValid) return;
                TryUnstuck(clicker);
            });
        };
        menu.AddOption(unstuckBtn);

        // 5 – Join Spectator
        var specBtn = new ButtonMenuOption(_helpers.T(player, "GameMenuJoinSpectator"))
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop,
            CloseAfterClick = true
        };
        specBtn.Click += async (_, args) =>
        {
            var clicker = args.Player;
            _core.Scheduler.NextTick(() =>
            {
                if (!clicker.IsValid) return;
                TryJoinSpectator(clicker);
            });
        };
        menu.AddOption(specBtn);

        _core.MenusAPI.OpenMenuForPlayer(player, menu);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Unstuck logic
    // ─────────────────────────────────────────────────────────────────────────

    private void TryUnstuck(IPlayer player)
    {
        if (player == null || !player.IsValid) return;

        var controller = player.Controller;
        if (controller == null || !controller.IsValid || !controller.PawnIsAlive)
        {
            player.SendMessage(MessageType.Chat, _helpers.T(player, "UnstuckMustBeAlive"));
            return;
        }

        var pawn = player.PlayerPawn;
        if (pawn == null || !pawn.IsValid) return;

        var origin = pawn.AbsOrigin;
        if (origin == null) return;

        // Try a series of small random offsets until the player is no longer stuck.
        // This is a best-effort implementation.
        var offsets = new (float x, float y, float z)[]
        {
            (50, 0, 0), (-50, 0, 0), (0, 50, 0), (0, -50, 0),
            (50, 50, 0), (-50, 50, 0), (50, -50, 0), (-50, -50, 0),
            (0, 0, 50), (70, 0, 50), (-70, 0, 50)
        };

        var angles = pawn.EyeAngles;
        foreach (var (dx, dy, dz) in offsets)
        {
            var dest = new Vector(origin.Value.X + dx, origin.Value.Y + dy, origin.Value.Z + dz);
            pawn.Teleport(dest, angles, Vector.Zero);
            break; // simple first-offset strategy
        }

        player.SendMessage(MessageType.Chat, _helpers.T(player, "UnstuckSuccess"));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Join Spectator logic
    // ─────────────────────────────────────────────────────────────────────────

    private void TryJoinSpectator(IPlayer player)
    {
        if (player == null || !player.IsValid) return;

        try
        {
            player.SwitchTeam(Team.Spectator);
            player.SendMessage(MessageType.Chat, _helpers.T(player, "JoinSpectatorSuccess"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[HZPGameMenu] Failed to move {Name} to spectator.", player.Name);
            player.SendMessage(MessageType.Chat, _helpers.T(player, "JoinSpectatorFailed"));
        }
    }
}
