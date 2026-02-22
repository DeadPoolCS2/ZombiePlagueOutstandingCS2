using System.Drawing;
using System.Reflection.Emit;
using System.Security;
using System.Timers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mono.Cecil.Cil;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.SteamAPI;

namespace HanZombiePlagueS2;

public class HZPAdminItemMenu
{
    private readonly ILogger<HZPAdminItemMenu> _logger;
    private readonly ISwiftlyCore _core;
    private readonly HZPMenuHelper _menuhelper;
    private readonly IOptionsMonitor<HZPMainCFG> _mainCFG;
    private readonly HZPHelpers _helpers;
    private readonly HZPServices _services;
    private readonly HZPGlobals _globals;

    public HZPAdminItemMenu(ISwiftlyCore core, ILogger<HZPAdminItemMenu> logger,
        HZPMenuHelper menuHelper, IOptionsMonitor<HZPMainCFG> mainCFG,
        HZPHelpers helpers, HZPServices service, HZPGlobals globals)
    {
        _core = core;
        _logger = logger;
        _menuhelper = menuHelper;
        _mainCFG = mainCFG;
        _helpers = helpers;
        _services = service;
        _globals = globals;
    }

    public IMenuAPI OpenAdminItemMenu(IPlayer player)
    {
        var main = _core.MenusAPI.CreateBuilder();
        IMenuAPI menu = _menuhelper.CreateMenu(_helpers.T(player, "AdminItemMenu"));

        menu.AddOption(new TextMenuOption(HtmlGradient.GenerateGradientText(
            _helpers.T(player, "AdminMenuSelect"),
            Color.Red, Color.LightBlue, Color.Red),
            updateIntervalMs: 500, pauseIntervalMs: 100)
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop
        });

        var CFG = _mainCFG.CurrentValue;

        string VaccineButtonText = _helpers.T(player, "ItemTVaccine");
        var VaccineButton = new ButtonMenuOption(VaccineButtonText)
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop,
            CloseAfterClick = true
        };
        VaccineButton.Tag = "extend";

        VaccineButton.Click += async (_, args) =>
        {
            var clicker = args.Player;

            _core.Scheduler.NextTick(() =>
            {
                if (!clicker.IsValid)
                    return;

                var Id = clicker.PlayerID;

                _globals.IsAssassin.TryGetValue(Id, out bool IsAssassin);
                _globals.IsNemesis.TryGetValue(Id, out bool IsNemesis);
                _globals.IsZombie.TryGetValue(Id, out bool IsZombie);

                if (IsAssassin || IsNemesis)
                {
                    clicker.SendCenter(_helpers.T(clicker, "ItemTVaccineError"));
                    return;
                }

                if(!IsZombie)
                    return;

                _globals.IsHero.TryGetValue(Id, out bool IsHero);
                _globals.IsSniper.TryGetValue(Id, out bool IsSniper);
                _globals.IsSurvivor.TryGetValue(Id, out bool IsSurvivor);
                
                int maxHealth;
                if (IsHero)
                {
                    maxHealth = CFG.Hero.HeroHealth;
                }
                else if (IsSniper)
                {
                    maxHealth = CFG.Sniper.SniperHealth;
                }
                else if (IsSurvivor)
                {
                    maxHealth = CFG.Survivor.SurvivorHealth;
                }
                else
                {
                    maxHealth = CFG.HumanMaxHealth;
                }

                string Default = "characters/models/ctm_st6/ctm_st6_variante.vmdl";
                string Custom = string.IsNullOrEmpty(CFG.HumandefaultModel) ? Default : CFG.HumandefaultModel;

                _helpers.TVaccine(clicker, maxHealth, CFG.HumanInitialSpeed, Custom, CFG.TVaccineSound, 1.0f);
                _helpers.SendChatT(clicker, "ItemTVaccineSuccess");
                _helpers.SendChatToAllT("ItemTVaccineSuccessToAll", clicker.Name);
            });
        };

        menu.AddOption(VaccineButton);

        string TVirusButtonText = _helpers.T(player, "ItemTVirus");
        var TVirusButton = new ButtonMenuOption(TVirusButtonText)
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop,
            CloseAfterClick = true
        };
        TVirusButton.Tag = "extend";

        TVirusButton.Click += async (_, args) =>
        {
            var clicker = args.Player;

            _core.Scheduler.NextTick(() =>
            {
                if (!clicker.IsValid)
                    return;

                _services.SetPlayerZombie(clicker);

                _helpers.SendChatT(clicker, "ItemTVirusSuccess");
                _helpers.SendChatToAllT("ItemTVirusSuccess", clicker.Name);
            });
        };

        menu.AddOption(TVirusButton);

        string TVirusGButtonText = _helpers.T(player, "ItemTVirusGrenade");
        var TVirusGButton = new ButtonMenuOption(TVirusGButtonText)
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop,
            CloseAfterClick = true
        };
        TVirusGButton.Tag = "extend";

        TVirusGButton.Click += async (_, args) =>
        {
            var clicker = args.Player;

            _core.Scheduler.NextTick(() =>
            {
                if (!clicker.IsValid)
                    return;

                var Id = clicker.PlayerID;
                _globals.IsZombie.TryGetValue(Id, out bool IsZombie);
                if (!IsZombie)
                {
                    _helpers.SendChatT(clicker, "ItemHumanCantUse");
                    return;
                }

                _helpers.TVirusGrenade(clicker);

                _helpers.SendChatT(clicker, "ItemTVirusGrenadeSuccess");
                _helpers.SendChatToAllT("ItemTVirusGrenadeSuccessToAll", clicker.Name);
            });
        };

        menu.AddOption(TVirusGButton);

        string ScbaButtonText = _helpers.T(player, "ItemSCBASuit");
        var ScbaButton = new ButtonMenuOption(ScbaButtonText)
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop,
            CloseAfterClick = true
        };
        ScbaButton.Tag = "extend";

        ScbaButton.Click += async (_, args) =>
        {
            var clicker = args.Player;

            _core.Scheduler.NextTick(() =>
            {
                if (!clicker.IsValid)
                    return;

                var Id = clicker.PlayerID;
                _globals.IsZombie.TryGetValue(Id, out bool IsZombie);
                if (IsZombie)
                {
                    _helpers.SendChatT(clicker, "ItemZombieCantUse");
                    return;
                }

                _globals.ScbaSuit.TryGetValue(Id, out bool IsHaveScbaSuit);
                if (IsHaveScbaSuit)
                {
                    _helpers.SendChatT(clicker, "ItemSCBASuitAlready");
                    return;
                }

                _helpers.GiveScbaSuit(clicker, CFG.ScbaSuitGetSound);
                _helpers.SendChatT(clicker, "ItemSCBASuitSuccess");
                _helpers.SendChatToAllT("ItemSCBASuitSuccessToAll", clicker.Name);
            });
        };

        menu.AddOption(ScbaButton);

        string GodButtonText = _helpers.T(player, "ItemGodMode");
        var GodButton = new ButtonMenuOption(GodButtonText)
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop,
            CloseAfterClick = true
        };
        GodButton.Tag = "extend";

        GodButton.Click += async (_, args) =>
        {
            var clicker = args.Player;

            _core.Scheduler.NextTick(() =>
            {
                if (!clicker.IsValid)
                    return;

                var Id = player.PlayerID;

                _globals.GodState.TryGetValue(Id, out bool IsGodState);
                if (IsGodState)
                {
                    _helpers.SendChatT(clicker, "ItemGodModeAlready");
                    return;
                }

                float time = 20f;
                _helpers.SetGodState(clicker, time);
                _helpers.SendChatT(clicker, "ItemGodModeSuccess", time);
                _helpers.SendChatToAllT("ItemGodModeSuccessToAll", clicker.Name, time);
            });
        };

        menu.AddOption(GodButton);

        string HealthButtonText = _helpers.T(player, "ItemAddHelath");
        var HealthButton = new ButtonMenuOption(HealthButtonText)
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop,
            CloseAfterClick = true
        };
        HealthButton.Tag = "extend";

        HealthButton.Click += async (_, args) =>
        {
            var clicker = args.Player;

            _core.Scheduler.NextTick(() =>
            {
                if (!clicker.IsValid)
                    return;

                var Id = clicker.PlayerID;
                _globals.IsZombie.TryGetValue(Id, out bool IsZombie);
                if (IsZombie)
                {
                    _helpers.SendChatT(clicker, "ItemZombieCantUse");
                    return;
                }
                    

                _globals.IsHero.TryGetValue(Id, out bool IsHero);
                _globals.IsSniper.TryGetValue(Id, out bool IsSniper);
                _globals.IsSurvivor.TryGetValue(Id, out bool IsSurvivor);


                int maxHealth;
                if (IsHero)
                {
                    maxHealth = CFG.Hero.HeroHealth;
                }
                else if (IsSniper)
                {
                    maxHealth = CFG.Sniper.SniperHealth;
                }
                else if (IsSurvivor)
                {
                    maxHealth = CFG.Survivor.SurvivorHealth;
                }
                else
                {
                    maxHealth = CFG.HumanMaxHealth;
                }

                int value = 200;

                var pawn = clicker.PlayerPawn;
                if (pawn == null || !pawn.IsValid)
                    return;

                var currentHealth = pawn.Health;
                var newHealth = currentHealth + value;

                if (currentHealth >= maxHealth)
                {
                    _helpers.SendChatT(clicker, "ItemAddHelathMax", maxHealth);
                    return;
                }

                _helpers.AddHealth(clicker, maxHealth, value, CFG.AddHealthSound);

                _helpers.SendChatT(clicker, "ItemAddHelathSuccess", value, pawn.Health, maxHealth);
                _helpers.SendChatToAllT("ItemAddHelathSuccessToAll", clicker.Name, value, maxHealth);
            });
        };

        menu.AddOption(HealthButton);

        string InfiniteAmmoButtonText = _helpers.T(player, "ItemInfiniteAmmo");
        var InfiniteAmmoButton = new ButtonMenuOption(InfiniteAmmoButtonText)
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop,
            CloseAfterClick = true
        };
        InfiniteAmmoButton.Tag = "extend";

        InfiniteAmmoButton.Click += async (_, args) =>
        {
            var clicker = args.Player;

            _core.Scheduler.NextTick(() =>
            {
                if (!clicker.IsValid)
                    return;

                var Id = clicker.PlayerID;
                _globals.IsZombie.TryGetValue(Id, out bool IsZombie);
                if (IsZombie)
                {
                    _helpers.SendChatT(clicker, "ItemZombieCantUse");
                    return;
                }

                _globals.InfiniteAmmoState.TryGetValue(Id, out bool IsInfiniteAmmoState);
                if (IsInfiniteAmmoState)
                {
                    _helpers.SendChatT(clicker, "ItemInfiniteAmmoAlready");
                    return;
                }

                float time = 20f;
                _helpers.SetInfiniteAmmoState(clicker, time);


                _helpers.SendChatT(clicker, "ItemInfiniteAmmoSuccess", time);
                _helpers.SendChatToAllT("ItemInfiniteAmmoSuccessToAll", clicker.Name, time);
            });
        };

        menu.AddOption(InfiniteAmmoButton);

        string FireGrenadeButtonText = _helpers.T(player, "ItemFireGrenade");
        var FireGrenadeButton = new ButtonMenuOption(FireGrenadeButtonText)
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop,
            CloseAfterClick = true
        };
        FireGrenadeButton.Tag = "extend";

        FireGrenadeButton.Click += async (_, args) =>
        {
            var clicker = args.Player;

            _core.Scheduler.NextTick(() =>
            {
                if (!clicker.IsValid)
                    return;

                var Id = clicker.PlayerID;
                _globals.IsZombie.TryGetValue(Id, out bool IsZombie);
                if (IsZombie)
                {
                    _helpers.SendChatT(clicker, "ItemZombieCantUse");
                    return;
                }
                _helpers.GiveFireGrenade(clicker);
                _helpers.SendChatT(clicker, "ItemFireGrenadeSuccess");
            });
        };

        menu.AddOption(FireGrenadeButton);

        string LightGrenadeButtonText = _helpers.T(player, "ItemLightGrenade");
        var LightGrenadeButton = new ButtonMenuOption(LightGrenadeButtonText)
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop,
            CloseAfterClick = true
        };
        LightGrenadeButton.Tag = "extend";

        LightGrenadeButton.Click += async (_, args) =>
        {
            var clicker = args.Player;

            _core.Scheduler.NextTick(() =>
            {
                if (!clicker.IsValid)
                    return;

                var Id = clicker.PlayerID;
                _globals.IsZombie.TryGetValue(Id, out bool IsZombie);
                if (IsZombie)
                {
                    _helpers.SendChatT(clicker, "ItemZombieCantUse");
                    return;
                }
                _helpers.GiveLightGrenade(clicker);
                _helpers.SendChatT(clicker, "ItemLightGrenadeSuccess");
            });
        };

        menu.AddOption(LightGrenadeButton);

        string FreezeGrenadeButtonText = _helpers.T(player, "ItemFreezeGrenade");
        var FreezeGrenadeButton = new ButtonMenuOption(FreezeGrenadeButtonText)
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop,
            CloseAfterClick = true
        };
        FreezeGrenadeButton.Tag = "extend";

        FreezeGrenadeButton.Click += async (_, args) =>
        {
            var clicker = args.Player;

            _core.Scheduler.NextTick(() =>
            {
                if (!clicker.IsValid)
                    return;

                var Id = clicker.PlayerID;
                _globals.IsZombie.TryGetValue(Id, out bool IsZombie);
                if (IsZombie)
                {
                    _helpers.SendChatT(clicker, "ItemZombieCantUse");
                    return;
                }
                _helpers.GiveFreezeGrenade(clicker);
                _helpers.SendChatT(clicker, "ItemFreezeGrenadeSuccess");
            });
        };

        menu.AddOption(FreezeGrenadeButton);

        string TeleprotGrenadeButtonText = _helpers.T(player, "ItemTeleportGrenade");
        var TeleprotGrenadeButton = new ButtonMenuOption(TeleprotGrenadeButtonText)
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop,
            CloseAfterClick = true
        };
        TeleprotGrenadeButton.Tag = "extend";

        TeleprotGrenadeButton.Click += async (_, args) =>
        {
            var clicker = args.Player;

            _core.Scheduler.NextTick(() =>
            {
                if (!clicker.IsValid)
                    return;

                var Id = clicker.PlayerID;
                _globals.IsZombie.TryGetValue(Id, out bool IsZombie);
                if (IsZombie)
                {
                    _helpers.SendChatT(clicker, "ItemZombieCantUse");
                    return;
                }
                _helpers.GiveTeleprotGrenade(clicker);
                _helpers.SendChatT(clicker, "ItemTeleportGrenadeSuccess");
            });
        };

        menu.AddOption(TeleprotGrenadeButton);

        string IncGrenadeButtonText = _helpers.T(player, "ItemIncGrenade");
        var IncGrenadeButton = new ButtonMenuOption(IncGrenadeButtonText)
        {
            TextStyle = MenuOptionTextStyle.ScrollLeftLoop,
            CloseAfterClick = true
        };
        IncGrenadeButton.Tag = "extend";

        IncGrenadeButton.Click += async (_, args) =>
        {
            var clicker = args.Player;

            _core.Scheduler.NextTick(() =>
            {
                if (!clicker.IsValid)
                    return;

                var Id = clicker.PlayerID;
                _globals.IsZombie.TryGetValue(Id, out bool IsZombie);
                if (IsZombie)
                {
                    _helpers.SendChatT(clicker, "ItemZombieCantUse");
                    return;
                }
                _helpers.GiveIncGrenade(clicker);
                _helpers.SendChatT(clicker, "ItemIncGrenadeSuccess");
            });
        };

        menu.AddOption(IncGrenadeButton);

        _core.MenusAPI.OpenMenuForPlayer(player, menu);
        return menu;
    }



}
