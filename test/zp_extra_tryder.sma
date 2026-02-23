/*================================================================================
	
	--------------------------------
	-*- [ZP] Tryder -*-
	--------------------------------
	
	~~~~~~~~~~~~~~~
	- Description -
	~~~~~~~~~~~~~~~
	
	Player with Glow + Unlimited Clip + Health + Armor.
	
================================================================================*/

#include <amxmodx>
#include <zombieplague>
#include <fakemeta_util>
#include <hamsandwich>

#define VERSION "2.2"
#pragma tabsize 0

#define MODEL_TRYDER "vip" // name of the model
#define TASK_DELAY 0.5
#define MODELSET_TASK 100

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
			
new g_fumado, g_has_unlimited_clip[33], g_tryder[33]
new tryder_health, tryder_armor, tryder_model, tryder_glow
new r, g, b

new g_has_tryder_model[33]
new g_tryder_model[33][32]
new Float:g_models_counter
new h_h;

public plugin_init()
{
	register_plugin("[ZP] Tryder", VERSION, "ILUSION")
	h_h = CreateHudSyncObj();
	g_fumado = zp_register_extra_item("Tryder", 35, ZP_TEAM_HUMAN)
	register_event("HLTV", "event_round_start", "a", "1=0", "2=0")
	register_logevent("event_round_end", 2, "1=Round_End")
	register_message(get_user_msgid("CurWeapon"), "message_cur_weapon")
	register_clcmd("drop", "clcmd_drop")
	register_forward(FM_ClientUserInfoChanged, "fw_ClientUserInfoChanged")
	RegisterHam(Ham_Killed, "player", "fw_PlayerKilled")
	
	tryder_health = 	register_cvar("zp_tryder_health", "1000")
	tryder_armor = 		register_cvar("zp_tryder_armor", "500")
	tryder_model = 		register_cvar("zp_tryder_model", "0")
	tryder_glow =		register_cvar("zp_tryder_glow", "1")
	r =			register_cvar("zp_tryder_glow_red", "0")
	g = 			register_cvar("zp_tryder_glow_green", "127")
	b = 			register_cvar("zp_tryder_glow_blue", "255")
	
	
	register_cvar("zp_tryder_version", VERSION, FCVAR_SERVER)
}

public plugin_precache()
{
	new modelpath[100]
	formatex(modelpath, sizeof modelpath - 1, "models/player/%s/%s.mdl", MODEL_TRYDER, MODEL_TRYDER)
	engfunc(EngFunc_PrecacheModel, modelpath)
}

// Item Selected forward
public zp_extra_item_selected(player, itemid)
{
	// check if the selected item matches any of our registered ones
	if (itemid == g_fumado)
	{
		// Strip off from weapons
		fm_strip_user_weapons(player)
		static red, green, blue
		red = get_pcvar_num(r)
		green = get_pcvar_num(g)
		blue = get_pcvar_num(b)
		// Glow
		if (get_pcvar_num(tryder_glow))
			fm_set_rendering(player, kRenderFxGlowShell, red, green, blue, kRenderNormal, 20)
		// Equips
		fm_give_item(player, "weapon_knife")
		fm_give_item(player, "weapon_xm1014")
		fm_give_item(player, "weapon_deagle")
		fm_give_item(player, "weapon_flashbang")
		fm_give_item(player, "weapon_hegrenade")
		// Clip
		g_has_unlimited_clip[player] = true
		// Dont Drop
		g_tryder[player] = true
		// HP
		fm_set_user_health(player, get_pcvar_num(tryder_health))
		// Aura
		// Armor
		fm_set_user_armor(player, get_pcvar_num(tryder_armor))
		if (get_pcvar_num(tryder_model))
			set_task(0.1, "cambiar", player)

		new h_Name[32];
		get_user_name(player, h_Name, charsmax(h_Name));
		
		set_hudmessage(random_num(0, 255), random_num(0, 255), random_num(0, 255), -1.0, 0.0, random_num(0, 2), random_float(0.7, 0.9), 12.0, random_float(0.37, 0.4), random_float(0.37, 0.4), 0.5);
		ShowSyncHudMsg(0, h_h, ".:: %s is now a Tryder ::.", h_Name);
	}
}

public clcmd_drop(player)
{
	if (g_tryder[player])
		return PLUGIN_HANDLED;
	
	return PLUGIN_CONTINUE;
}

public cambiar(player)
{
	if (g_tryder[player])
	{
		copy(g_tryder_model[player], sizeof g_tryder_model[] - 1, MODEL_TRYDER)
		
		new currentmodel[32]
		fm_get_user_model(player, currentmodel, sizeof currentmodel - 1)
		
		if (!equal(currentmodel, g_tryder_model[player]))
		{
			set_task(1.0 + g_models_counter, "task_set_model", player+MODELSET_TASK)
			g_models_counter += TASK_DELAY
		}
	}
	
	return PLUGIN_HANDLED
}

public event_round_start()
{
	for (new id; id <= 32; id++) g_has_unlimited_clip[id] = false;
	for (new player; player <= 32; player++) g_tryder[player] = false;
}

public event_round_end()
{
	g_models_counter = 0.0
}

public fw_PlayerKilled(victim, attacker, shouldgib)
{
	if (g_tryder[victim])
	{
		fm_reset_user_model(victim)
	}
}

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

public task_set_model(player)
{
	// Get player id
	player -= MODELSET_TASK
    
	// Actually set the player's model
	fm_set_user_model(player, g_tryder_model[player])
}


public fw_ClientUserInfoChanged(player)
{
	if (g_tryder[player] && !zp_get_user_first_zombie(player) && !zp_get_user_zombie(player) && !zp_get_user_nemesis(player) && !zp_get_user_survivor(player))
	{
		// Player doesn't have a custom model
		if (!g_has_tryder_model[player])
			return FMRES_IGNORED;

		// Get current model
		static currentmodel[32]
		fm_get_user_model(player, currentmodel, sizeof currentmodel - 1)
    
		// Check whether it matches the custom model - if not, set it again
		if (!equal(currentmodel, g_tryder_model[player]))
			fm_set_user_model(player, g_tryder_model[player])
    
		return FMRES_IGNORED;
	}
	
	return FMRES_IGNORED;
}

// Set Weapon Clip Ammo
stock fm_set_weapon_ammo(entity, amount)
{
	set_pdata_int(entity, OFFSET_CLIPAMMO, amount, OFFSET_LINUX_WEAPONS);
}

stock fm_set_user_model(player, const modelname[]) 
{     
	// Set new model     
	engfunc(EngFunc_SetClientKeyValue, player, engfunc(EngFunc_GetInfoKeyBuffer, player), "model", modelname)         
	
	// Remember this player has a custom model     
	g_has_tryder_model[player] = true 
}

stock fm_get_user_model(player, model[], len) 
{     
	// Retrieve current model
	engfunc(EngFunc_InfoKeyValue, engfunc(EngFunc_GetInfoKeyBuffer, player), "model", model, len) 
}

stock fm_reset_user_model(player)
{
	// Player doesn't have a custom model any longer
	g_has_tryder_model[player] = false
    
	dllfunc(DLLFunc_ClientUserInfoChanged, player, engfunc(EngFunc_GetInfoKeyBuffer, player))
}
