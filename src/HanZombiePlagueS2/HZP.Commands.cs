using System.Numerics;
using System.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mono.Cecil.Cil;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SteamAPI;

namespace HanZombiePlagueS2;

public class HZPCommands
{
    private readonly ILogger<HZPCommands> _logger;
    private readonly ISwiftlyCore _core;
    private readonly HZPServices _services;
    private readonly IOptionsMonitor<HZPMainCFG> _mainCFG;
    private readonly HZPGlobals _globals;
    private readonly HZPZombieClassMenu _hZPZombieClassMenu;
    private readonly HZPAdminItemMenu _hZPAdminItemMenu;
    private readonly HZPHelpers _helpers;
    private readonly HZPWeaponsMenu _weaponsMenu;
    private readonly HZPGameMenu _gameMenu;
    private readonly HZPExtraItemsMenu _extraItemsMenu;

    public HZPCommands(ISwiftlyCore core, ILogger<HZPCommands> logger,
        HZPServices services, IOptionsMonitor<HZPMainCFG> mainCFG,
        HZPGlobals globals, HZPAdminItemMenu hZPAdminItemMenu,
        HZPZombieClassMenu hZPZombieClassMenu, HZPHelpers helpers,
        HZPWeaponsMenu weaponsMenu, HZPGameMenu gameMenu,
        HZPExtraItemsMenu extraItemsMenu)
    {
        _core = core;
        _logger = logger;
        _services = services;
        _mainCFG = mainCFG;
        _globals = globals;
        _hZPAdminItemMenu = hZPAdminItemMenu;
        _hZPZombieClassMenu = hZPZombieClassMenu;
        _helpers = helpers;
        _weaponsMenu = weaponsMenu;
        _gameMenu = gameMenu;
        _extraItemsMenu = extraItemsMenu;
    }

    public void MenuCommands()
    {
        var CFG = _mainCFG.CurrentValue;
        _core.Command.RegisterCommand(CFG.ZombieClassCommand, SelectZombieClass, true);
        _logger.LogInformation("[HZP] Registered zombie class command: {Cmd}", CFG.ZombieClassCommand);

        _core.Command.RegisterCommand(CFG.AdminMenuItemCommand, UseItemMenu, true);
        _logger.LogInformation("[HZP] Registered admin menu command: {Cmd}", CFG.AdminMenuItemCommand);

        _core.Command.RegisterCommand(CFG.BuyWeaponsCommand, BuyWeapons, true);
        _logger.LogInformation("[HZP] Registered buy weapons command: {Cmd}", CFG.BuyWeaponsCommand);

        _core.Command.RegisterCommand(CFG.MainMenuCommand, OpenGameMenu, true);
        _logger.LogInformation("[HZP] Registered main menu command: {Cmd}", CFG.MainMenuCommand);

        _core.Command.RegisterCommand(CFG.ExtraItemsCommand, OpenExtraItemsMenu, true);
        _logger.LogInformation("[HZP] Registered extra items command: {Cmd}", CFG.ExtraItemsCommand);

        _core.Command.RegisterCommand(CFG.KnifeBlinkCommand, KnifeBlink, true);
        _logger.LogInformation("[HZP] Registered knife blink command: {Cmd}", CFG.KnifeBlinkCommand);

        _core.Command.RegisterCommand(CFG.PlantMineCommand, PlantMine, true);
        _logger.LogInformation("[HZP] Registered plant mine command: {Cmd}", CFG.PlantMineCommand);

        _core.Command.RegisterCommand(CFG.TakeMineCommand, TakeMine, true);
        _logger.LogInformation("[HZP] Registered take mine command: {Cmd}", CFG.TakeMineCommand);

        _core.Command.RegisterCommand(CFG.GiveAmmoPacksCommand, GiveAmmoPacks, true);
        _logger.LogInformation("[HZP] Registered give ammo packs command: {Cmd}", CFG.GiveAmmoPacksCommand);
    }
    public void SelectZombieClass(ICommandContext context)
    {
        var player = context.Sender;
        if (player == null || !player.IsValid) 
            return;

        _hZPZombieClassMenu.OpenZombieClassMenu(player);

    }

    public void UseItemMenu(ICommandContext context)
    {
        var player = context.Sender;
        if (player == null || !player.IsValid)
            return;


        if (!HasAdminMenuPermission(player))
        {
            player.SendMessage(MessageType.Chat, _helpers.T(player, "NoPermission"));
            return;
        }
            

        _hZPAdminItemMenu.OpenAdminItemMenu(player);
    }

    public void BuyWeapons(ICommandContext context)
    {
        var player = context.Sender;
        if (player == null || !player.IsValid)
            return;

        _weaponsMenu.OpenWeaponsMenuIfAllowed(player);
    }

    public void OpenGameMenu(ICommandContext context)
    {
        var player = context.Sender;
        if (player == null || !player.IsValid) return;
        _gameMenu.OpenGameMenu(player);
    }

    public void OpenExtraItemsMenu(ICommandContext context)
    {
        var player = context.Sender;
        if (player == null || !player.IsValid) return;
        _extraItemsMenu.OpenExtraItemsMenu(player);
    }

    public void KnifeBlink(ICommandContext context)
    {
        var player = context.Sender;
        if (player == null || !player.IsValid) return;
        _extraItemsMenu.TryExecuteKnifeBlink(player);
    }

    public void PlantMine(ICommandContext context)
    {
        var player = context.Sender;
        if (player == null || !player.IsValid) return;
        _extraItemsMenu.TryPlantTripMine(player);
    }

    public void TakeMine(ICommandContext context)
    {
        var player = context.Sender;
        if (player == null || !player.IsValid) return;
        _extraItemsMenu.TryTakeTripMine(player);
    }

    public void GiveAmmoPacks(ICommandContext context)
    {
        var sender = context.Sender;

        // Require admin permission
        if (sender != null && sender.IsValid)
        {
            if (!HasAdminMenuPermission(sender))
            {
                sender.SendMessage(MessageType.Chat, _helpers.T(sender, "NoPermission"));
                return;
            }
        }

        // Parse arguments: hzp_give_ap <target> <amount>
        string[] args = context.Args;
        if (args.Length < 2)
        {
            if (sender != null && sender.IsValid)
                sender.SendMessage(MessageType.Chat, _helpers.T(sender, "GiveAPUsage"));
            return;
        }

        string targetName = args[0];
        if (!int.TryParse(args[1], out int amount) || amount <= 0)
        {
            if (sender != null && sender.IsValid)
                sender.SendMessage(MessageType.Chat, _helpers.T(sender, "GiveAPInvalidAmount"));
            return;
        }

        var allPlayers = _core.PlayerManager.GetAllPlayers();
        IPlayer? target = null;

        // Support "#userid" or partial name match
        if (targetName.StartsWith("#") && int.TryParse(targetName[1..], out int uid))
        {
            target = allPlayers.FirstOrDefault(p => p != null && p.IsValid && p.PlayerID == uid);
        }
        else
        {
            target = allPlayers.FirstOrDefault(p =>
                p != null && p.IsValid &&
                p.Name.Contains(targetName, StringComparison.OrdinalIgnoreCase));
        }

        if (target == null || !target.IsValid)
        {
            if (sender != null && sender.IsValid)
                sender.SendMessage(MessageType.Chat, _helpers.T(sender, "GiveAPTargetNotFound"));
            return;
        }

        _extraItemsMenu.AddAmmoPacks(target.PlayerID, amount);
        int newTotal = _extraItemsMenu.GetAmmoPacks(target.PlayerID);

        target.SendMessage(MessageType.Chat,
            ChatMsg(_helpers.T(target, "GiveAPReceived", amount, newTotal)));

        if (sender != null && sender.IsValid)
            sender.SendMessage(MessageType.Chat,
                ChatMsg(_helpers.T(sender, "GiveAPSuccess", target.Name, amount, newTotal)));
    }

    private string ChatMsg(string message) =>
        _helpers.ChatMsg(message);

    private bool HasAdminMenuPermission(IPlayer player)
    {
        if (player == null || !player.IsValid)
            return false;

        ulong steamId = player.SteamID;
        if (steamId == 0)
            return false;

        var permString = _mainCFG.CurrentValue.AdminMenuPermission;

        if (string.IsNullOrWhiteSpace(permString))
            return true;

        foreach (var perm in permString.Split(','))
        {
            var p = perm.Trim();
            if (p.Length == 0)
                continue;

            if (_core.Permission.PlayerHasPermission(steamId, p))
                return true;
        }

        return false;
    }

    public void RoundCvar()
    {
        var CFG = _mainCFG.CurrentValue;
        _core.Engine.ExecuteCommand("mp_randomspawn 1");
        _core.Engine.ExecuteCommand($"mp_roundtime_hostage {CFG.RoundTime}");
        _core.Engine.ExecuteCommand($"mp_roundtime_defuse {CFG.RoundTime}");
        _core.Engine.ExecuteCommand($"mp_roundtime {CFG.RoundTime}");
        _core.Engine.ExecuteCommand("mp_give_player_c4 0");

    }

    public void ServerCvar()
    {
        
        _core.Engine.ExecuteCommand("mp_randomspawn 1");
        _core.Engine.ExecuteCommand("mp_roundtime_hostage 3");
        _core.Engine.ExecuteCommand("mp_roundtime_defuse 3");
        _core.Engine.ExecuteCommand("mp_roundtime 3");
        _core.Engine.ExecuteCommand("bot_quota_mode fill");
        _core.Engine.ExecuteCommand("bot_quota 20");
        _core.Engine.ExecuteCommand("mp_ignore_round_win_conditions 1");
        _core.Engine.ExecuteCommand("bot_join_after_player 1");
        _core.Engine.ExecuteCommand("bot_chatter off");
        _core.Engine.ExecuteCommand("mp_autokick 0");
        _core.Engine.ExecuteCommand("mp_round_restart_delay 0");
        _core.Engine.ExecuteCommand("mp_autoteambalance 0");
        _core.Engine.ExecuteCommand("mp_startmoney 16000");
    }
    public void Command()
    {
        _core.Command.RegisterCommand("jointeam", RegisterJoin, true);
        _core.Command.HookClientCommand(OnJoinTeam);

    }

    public void RegisterJoin(ICommandContext context){
    }


    public HookResult OnJoinTeam(int playerId, string commandLine)
    {
        IPlayer? player = _core.PlayerManager.GetPlayer(playerId);
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        if (!player.IsFakeClient)
        {
            if (commandLine.StartsWith("jointeam 2"))
            {
                player.SwitchTeam(Team.CT);
                _core.Scheduler.DelayBySeconds(1.0f, () =>
                {
                    _services.JoinTeamCheck(player);
                });
            }
            else if (commandLine.StartsWith("jointeam 3"))
            {
                player.SwitchTeam(Team.CT);
                _core.Scheduler.DelayBySeconds(1.0f, () =>
                {
                    _services.JoinTeamCheck(player);
                });

            }
            else if (commandLine.StartsWith("jointeam 1"))
            {
                player.SwitchTeam(Team.CT);
                _core.Scheduler.DelayBySeconds(1.0f, () =>
                {
                    _services.JoinTeamCheck(player);
                });
                return HookResult.Stop;
            }

        }
        return HookResult.Continue;
    }


}