#include <amxmodx>
#include <fun>
#include <fakemeta>
#include <hamsandwich>
#include <zombieplague>

#define is_valid_player(%1) (1 <= %1 <= _MaxPlayers)

new _Model[] = "models/ZombieOutStanding/v_golden_deagle.mdl";

new bool:_NightHawk[33];

new _MaxPlayers;
new _Item;

public plugin_init()
{
	register_plugin("Night Hawk", "1.0", "LondoN eXtream");

	_MaxPlayers = get_maxplayers();
	_Item = zp_register_extra_item("Golden Deagle (\rNight Hawk\w)", 32, ZP_TEAM_HUMAN);

	RegisterHam(Ham_TakeDamage, "player", "_hamTakeDamage");
	RegisterHam(Ham_Item_Deploy, "weapon_deagle", "_hamItemDeploy");
	RegisterHam(Ham_Killed, "player", "_hamKilled", 1)
}

public plugin_precache()
{
	precache_model(_Model);
}

public _hamTakeDamage(Victim, Inflictor, Attacker, Float:Damage)
{
	if(is_valid_player(Attacker) && get_user_weapon(Attacker) == CSW_DEAGLE && _NightHawk[Attacker])
	{
		SetHamParamFloat(4, Damage * 2.0);
	}
}

public _hamItemDeploy(Entity)
{
	new id = get_pdata_cbase(Entity, 41, 4);

	if(is_valid_player(id) && _NightHawk[id])
	{
		set_pev(id, pev_viewmodel2, _Model);
	}
}

public _hamKilled(id, Killer)
{
	if(is_user_connected(id) && id != Killer)
	{
		_NightHawk[id] = false;
	}
}

public zp_extra_item_selected(Player, ItemID)
{
	if(ItemID == _Item)
	{
		if(user_has_weapon(Player, CSW_DEAGLE))
		{
			new Weapons[32], Num;
			get_user_weapons(Player, Weapons, Num);
		
			for(new i = 0; i < Num; i++)
			{
				if((1<<CSW_DEAGLE) & (1<<Weapons[i]))
				{
					static _Name[32];
					get_weaponname(Weapons[i], _Name, charsmax(_Name));
					engclient_cmd(Player, "drop", _Name);
				}
			}
		}

		give_item(Player, "weapon_deagle");

		_NightHawk[Player] = true;
	}
}

public zp_user_infected_post(id)
{
	_NightHawk[id] = false;
}