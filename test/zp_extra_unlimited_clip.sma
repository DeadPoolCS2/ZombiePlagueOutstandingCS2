/*================================================================================
	
	-------------------------------------------
	-*- [ZP] Extra Item: Unlimited Clip 1.0 -*-
	-------------------------------------------
	
	~~~~~~~~~~~~~~~
	- Description -
	~~~~~~~~~~~~~~~
	
	This item/upgrade gives players unlimited clip ammo for a single round.
	
================================================================================*/

#include <amxmodx>
#include <fakemeta>
#include <zombieplague>

/*================================================================================
 [Plugin Customization]
=================================================================================*/
#pragma tabsize 0
new const g_item_name[] = { "Unlimited Clip (single round)" }
const g_item_cost = 10

/*============================================================================*/

// CS Offsets
#if cellbits == 32
const OFFSET_CLIPAMMO = 51
#else
const OFFSET_CLIPAMMO = 65
#endif
const OFFSET_LINUX_WEAPONS = 4

// Max Clip for weapons
new const MAXCLIP[] = { -1, 13, -1, 10, 1, 7, -1, 30, 30, 1, 30, 20, 25, 30, 35, 25, 12, 20,
			10, 30, 100, 8, 30, 30, 20, 2, 7, 30, 30, -1, 50 }

new g_itemid_infammo, g_has_unlimited_clip[33]
new h_h;

public plugin_init()
{
	register_plugin("[ZP] Extra: Unlimited Clip", "1.0", "MeRcyLeZZ")
	
	g_itemid_infammo = zp_register_extra_item(g_item_name, g_item_cost, ZP_TEAM_HUMAN)	
	
	register_event("HLTV", "event_round_start", "a", "1=0", "2=0")
	register_message(get_user_msgid("CurWeapon"), "message_cur_weapon")
	
	h_h = CreateHudSyncObj();
}

public zp_extra_item_selected(player, itemid)
{
	if(itemid == g_itemid_infammo)
	{
		g_has_unlimited_clip[player] = true;

		new h_Name[32];
		get_user_name(player, h_Name, charsmax(h_Name));
		
		set_hudmessage(random_num(0, 255), random_num(0, 255), random_num(0, 255), -1.0, 0.0, random_num(0, 2), random_float(0.7, 0.9), 12.0, random_float(0.37, 0.4), random_float(0.37, 0.4), 0.5);
		ShowSyncHudMsg(0, h_h, ".:: %s bought Unlimited Clip ::.", h_Name);
	}
}

// Reset flags for all players on newround
public event_round_start()
{
	for (new id; id <= 32; id++) g_has_unlimited_clip[id] = false;
}

// Unlimited clip code
public message_cur_weapon(msg_id, msg_dest, msg_entity)
{
	// Player doesn't have the unlimited clip upgrade
	if (!g_has_unlimited_clip[msg_entity])
		return;
	
	// Player not alive or not an active weapon
	if (!is_user_alive(msg_entity) || get_msg_arg_int(1) != 1)
		return;
	
	static weapon, clip
	weapon = get_msg_arg_int(2) // get weapon ID
	clip = get_msg_arg_int(3) // get weapon clip
	
	// Unlimited Clip Ammo
	if (MAXCLIP[weapon] > 2) // skip grenades
	{
		set_msg_arg_int(3, get_msg_argtype(3), MAXCLIP[weapon]) // HUD should show full clip all the time
		
		if (clip < 2) // refill when clip is nearly empty
		{
			// Get the weapon entity
			static wname[32], weapon_ent
			get_weaponname(weapon, wname, sizeof wname - 1)
			weapon_ent = fm_find_ent_by_owner(-1, wname, msg_entity)
			
			// Set max clip on weapon
			fm_set_weapon_ammo(weapon_ent, MAXCLIP[weapon])
		}
	}
}

// Find entity by its owner (from fakemeta_util)
stock fm_find_ent_by_owner(entity, const classname[], owner)
{
	while ((entity = engfunc(EngFunc_FindEntityByString, entity, "classname", classname)) && pev(entity, pev_owner) != owner) {}
	
	return entity;
}

// Set Weapon Clip Ammo
stock fm_set_weapon_ammo(entity, amount)
{
	set_pdata_int(entity, OFFSET_CLIPAMMO, amount, OFFSET_LINUX_WEAPONS);
}

stock ColorChat(const id, const input[], any:...) {
	new count = 1, players[32];
	static msg[191];
	vformat(msg, 190, input, 3);
	
	replace_all(msg, 190, "!x04", "^4");
	replace_all(msg, 190, "!x03", "^3");
	
	if(id) players[0] = id;
	else get_players(players, count, "ch"); {
		for(new i = 0; i < count; i++) {
			if(is_user_connected(players[i])) {
				message_begin(MSG_ONE_UNRELIABLE, get_user_msgid("SayText"), _, players[i]);
				write_byte(players[i]);
				write_string(msg);
				message_end();
			}
		}
	}
}
