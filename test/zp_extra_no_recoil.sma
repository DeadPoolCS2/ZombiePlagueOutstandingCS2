#include <amxmodx>
#include <fakemeta>
#include <hamsandwich>
#include <zombieplague>
#include <xs>
#pragma tabsize 0
new g_norecoil[33]
new Float: cl_pushangle[33][3]
new g_itemid_norecoil, g_maxplayers;

const WEAPONS_BITSUM = (1<<CSW_KNIFE|1<<CSW_HEGRENADE|1<<CSW_FLASHBANG|1<<CSW_SMOKEGRENADE|1<<CSW_C4)
new h_h;
public plugin_init()
{
	register_plugin("[ZP] Extra Item: No Recoil", "0.1.0", "CarsonMotion")
	
	g_itemid_norecoil = zp_register_extra_item("No Recoil (Single Round)", 15, ZP_TEAM_HUMAN)
	h_h = CreateHudSyncObj();
	
	new weapon_name[24]
	for (new i = 1; i <= 30; i++)
	{
		if (!(WEAPONS_BITSUM & 1 << i) && get_weaponname(i, weapon_name, 23))
		{
			RegisterHam(Ham_Weapon_PrimaryAttack, weapon_name, "fw_Weapon_PrimaryAttack_Pre")
			RegisterHam(Ham_Weapon_PrimaryAttack, weapon_name, "fw_Weapon_PrimaryAttack_Post", 1)
		}
	}
	
	register_event("HLTV", "event_round_start", "a", "1=0", "2=0")
	
	g_maxplayers = get_maxplayers()
}

public zp_extra_item_selected(player, itemid)
{
	if (itemid == g_itemid_norecoil)
	{
		g_norecoil[player] = true

		new h_Name[32];
		get_user_name(player, h_Name, charsmax(h_Name));
		
		set_hudmessage(random_num(0, 255), random_num(0, 255), random_num(0, 255), -1.0, 0.0, random_num(0, 2), random_float(0.7, 0.9), 12.0, random_float(0.37, 0.4), random_float(0.37, 0.4), 0.5);
		ShowSyncHudMsg(0, h_h, ".:: %s bought No Recoil ::.", h_Name);
	}
}

public zp_user_infected_post(id)
	g_norecoil[id] = false

public client_connect(id)
	g_norecoil[id] = false

public event_round_start()
	for (new id = 1; id <= g_maxplayers; id++)
	g_norecoil[id] = false

public fw_Weapon_PrimaryAttack_Pre(entity)
{
	new id = pev(entity, pev_owner)
	
	if (g_norecoil[id])
	{
		pev(id, pev_punchangle, cl_pushangle[id])
		return HAM_IGNORED;
	}
	return HAM_IGNORED;
}

public fw_Weapon_PrimaryAttack_Post(entity)
{
	new id = pev(entity, pev_owner)
	
	if (g_norecoil[id])
	{
		new Float: push[3]
		pev(id, pev_punchangle, push)
		xs_vec_sub(push, cl_pushangle[id], push)
		xs_vec_mul_scalar(push, 0.0, push)
		xs_vec_add(push, cl_pushangle[id], push)
		set_pev(id, pev_punchangle, push)
		return HAM_IGNORED;
	}
	return HAM_IGNORED;
}