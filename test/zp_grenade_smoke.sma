/*================================================================================
	
	---------------------------
	-*- [ZP] Grenade: smoke -*-
	---------------------------
	
	This plugin is part of Zombie Plague Mod and is distributed under the
	terms of the GNU General Public License. Check ZP_ReadMe.txt for details.
	
================================================================================*/

#include <amxmodx>
#include <fakemeta>
#include <hamsandwich>
#include <amx_settings_api>
#include <cs_weap_models_api>
#include <zombieplague>

// Settings file
new const ZP_SETTINGS_FILE[] = "zp_grenade_smoke.ini"

#define MODEL_MAX_LENGTH 64
#define SOUND_MAX_LENGTH 64
#define SPRITE_MAX_LENGTH 64

// Models
new g_model_grenade_smoke[MODEL_MAX_LENGTH] = "models/v_hegrenade.mdl"

// Sprites
new g_sprite_grenade_trail[SPRITE_MAX_LENGTH] = "sprites/laserbeam.spr"
new g_sprite_grenade_explode[SPRITE_MAX_LENGTH] = "sprites/zerogxplode.spr"

// HACK: pev_ field used to store custom nade types and their values
const PEV_NADE_TYPE = pev_flTimeStepSound
const NADE_TYPE_smoke = 4444 + 1;

new g_trailSpr, g_iID_SpriteExplode

new cvar_grenade_smoke_duration, cvar_grenade_smoke_radius

public plugin_init()
{
	register_plugin("[ZP] Grenade: Smoke", "0.0.5", "ZP Dev Team")
	
	register_forward(FM_SetModel, "fw_SetModel")
	
	RegisterHam(Ham_Think, "grenade", "fw_ThinkGrenade")
	RegisterHam(Ham_Spawn, "player", "fwHamSpawnPlayer", 1)
	
	cvar_grenade_smoke_duration = register_cvar("zp_grenade_smoke_damage", "667")
	cvar_grenade_smoke_radius = register_cvar("zp_grenade_smoke_radius", "300.0")
}

public plugin_precache()
{
	// Load from external file, save if not found
	if (!amx_load_setting_string(ZP_SETTINGS_FILE, "Models", "VIEW", g_model_grenade_smoke, charsmax(g_model_grenade_smoke)))
		amx_save_setting_string(ZP_SETTINGS_FILE, "Models", "VIEW", g_model_grenade_smoke)
	if (!amx_load_setting_string(ZP_SETTINGS_FILE, "Sprites", "TRAIL", g_sprite_grenade_trail, charsmax(g_sprite_grenade_trail)))
		amx_save_setting_string(ZP_SETTINGS_FILE, "Sprites", "TRAIL", g_sprite_grenade_trail)
	if (!amx_load_setting_string(ZP_SETTINGS_FILE, "Sprites", "EXPLODE", g_sprite_grenade_explode, charsmax(g_sprite_grenade_explode)))
		amx_save_setting_string(ZP_SETTINGS_FILE, "Sprites", "EXPLODE", g_sprite_grenade_explode)
	
	// Precache models
	precache_model(g_model_grenade_smoke)
	g_trailSpr = precache_model(g_sprite_grenade_trail)
	g_iID_SpriteExplode = precache_model(g_sprite_grenade_explode)
}

public fwHamSpawnPlayer(const iID)
{
	if (!is_user_alive(iID))
		return;
	
	// Set custom grenade model
	cs_set_player_view_model(iID, CSW_SMOKEGRENADE, g_model_grenade_smoke)
}

public zp_user_humanized_post(id)
{
	// Set custom grenade model
	cs_set_player_view_model(id, CSW_SMOKEGRENADE, g_model_grenade_smoke)
}

public zp_user_infected_pre(id)
{
	// Remove custom grenade model
	cs_reset_player_view_model(id, CSW_SMOKEGRENADE)
}

// Forward Set Model
public fw_SetModel(entity, const model[])
{
	// We don't care
	if (strlen(model) < 8)
		return;
	
	// Narrow down our matches a bit
	if (model[7] != 'w' || model[8] != '_')
		return;
	
	// Get damage time of grenade
	static Float:dmgtime
	pev(entity, pev_dmgtime, dmgtime)
	
	// Grenade not yet thrown
	if (dmgtime == 0.0)
		return;
	
	// Grenade's owner is zombie?
	if (zp_get_user_zombie(pev(entity, pev_owner)))
		return;
	
	// Smoke Grenade
	if (model[9] == 's' && model[10] == 'm')
	{
		set_pev(entity, pev_dmgtime, get_gametime() + 1.5)
		
		// Give it a glow
		fm_set_rendering(entity, kRenderFxGlowShell, 255, 0, 0, kRenderNormal, 16);
		
		// And a colored trail
		message_begin(MSG_BROADCAST, SVC_TEMPENTITY)
		write_byte(TE_BEAMFOLLOW) // TE id
		write_short(entity) // entity
		write_short(g_trailSpr) // sprite
		write_byte(10) // life
		write_byte(10) // width
		write_byte(255) // r
		write_byte(0) // g
		write_byte(0) // b
		write_byte(200) // brightness
		message_end()
		
		// Set grenade type on the thrown grenade entity
		set_pev(entity, PEV_NADE_TYPE, NADE_TYPE_smoke)
	}
}

// Ham Grenade Think Forward
public fw_ThinkGrenade(entity)
{
	// Invalid entity
	if (!pev_valid(entity)) return HAM_IGNORED;
	
	// Get damage time of grenade
	static Float:dmgtime
	pev(entity, pev_dmgtime, dmgtime)
	
	new Float:current_time = get_gametime()
	
	// Check if it's time to go off
	if (dmgtime > current_time)
		return HAM_IGNORED;
	
	// Check if it's one of our custom nades
	switch (pev(entity, PEV_NADE_TYPE))
	{
		case NADE_TYPE_smoke: // smoke
		{
			static Float:fVecOrigin[3], Float:fVecOrigin2[3], szName[31];
			pev(entity, pev_origin, fVecOrigin)
			
			static iMaxPlayers, iID_MsgScreenFade;
			
			if (!iMaxPlayers)
				iMaxPlayers = get_maxplayers();
			
			if (!iID_MsgScreenFade)
				iID_MsgScreenFade = get_user_msgid("ScreenFade");
			
			new Float:fDistance,
			Float:fDamage,
			Float:fCvarDamage = get_pcvar_float(cvar_grenade_smoke_duration),
			iID_EntOwner = pev(entity, pev_owner),
			bAttackerConnected = is_user_connected(iID_EntOwner);
			
			static iID_MessageDeath;
			if (!iID_MessageDeath)
				iID_MessageDeath = get_user_msgid("DeathMsg");
			
			for (new iID = 1; iID <= iMaxPlayers; iID++)
			{
				if (!is_user_alive(iID) || !zp_get_user_zombie(iID))
					continue;
				
				pev(iID, pev_origin, fVecOrigin2)
				fDistance = get_distance_f(fVecOrigin, fVecOrigin2);
				
				if (fDistance > 300.0)
					continue;
				
				fDamage = fCvarDamage - (fCvarDamage / get_pcvar_float(cvar_grenade_smoke_radius)) * fDistance;
				
				if (fDamage < pev(iID, pev_health))
				{
					ExecuteHamB(Ham_TakeDamage, iID, entity, iID_EntOwner, fDamage, DMG_BLAST)
					
					message_begin(MSG_ONE_UNRELIABLE, iID_MsgScreenFade, _, iID)
					write_short(2048)
					write_short(1024)
					write_short(0x0000)
					write_byte(220)
					write_byte(0)
					write_byte(0)
					write_byte(205)
					message_end();
					
					if (bAttackerConnected)
					{
						get_user_name(iID, szName, charsmax(szName))
						
						ftClientPrintChatColor(iID_EntOwner, iID, "Damage done to ^3%s^1 :: ^4%d^1.", szName, floatround(fDamage))
					}
				}
				else
				{
					new iMsgBlock = get_msg_block(iID_MessageDeath);
					
					if (iMsgBlock == BLOCK_NOT)
						set_msg_block(iID_MessageDeath, BLOCK_ONCE)
					
					ExecuteHamB(Ham_Killed, iID, bAttackerConnected ? iID_EntOwner : iID, 0)
					
					if (iMsgBlock == BLOCK_NOT)
						set_msg_block(iID_MessageDeath, iMsgBlock)
					
					message_begin(MSG_ALL, iID_MessageDeath)
					write_byte(bAttackerConnected ? iID_EntOwner : iID)
					write_byte(iID)
					write_byte(0) //headshot
					write_string("grenade")
					message_end()
				}
			}
			
			message_begin(MSG_BROADCAST, SVC_TEMPENTITY)
			write_byte(TE_EXPLOSION)
			engfunc(EngFunc_WriteCoord, fVecOrigin[0])
			engfunc(EngFunc_WriteCoord, fVecOrigin[1])
			engfunc(EngFunc_WriteCoord, fVecOrigin[2])
			write_short(g_iID_SpriteExplode)
			write_byte(30)
			write_byte(15)
			write_byte(0)
			message_end()
			
			engfunc(EngFunc_RemoveEntity, entity)
			
			return HAM_SUPERCEDE;
		}
	}
	
	return HAM_IGNORED;
}

ftClientPrintChatColor(const iID_Target, const iID_Sender = 0, const szMessage[], any:...)
{
	if (iID_Target && !is_user_connected(iID_Target))
		return;
	
	static szBuffer[192];
	vformat(szBuffer, charsmax(szBuffer), szMessage, 4)
	
	static const D7_CHAT_TAG[] = "^x04[ZOMBIE.THEXFORCE.RO]^x01";
	format(szBuffer, charsmax(szBuffer), "%s %s", D7_CHAT_TAG, szBuffer)
	
	static iID_MsgSayText;
	if (!iID_MsgSayText)
		iID_MsgSayText = get_user_msgid("SayText");
	
	if (iID_Target)
		message_begin(MSG_ONE, iID_MsgSayText, _, iID_Target)
	else
		message_begin(MSG_ALL, iID_MsgSayText)
	
	write_byte(!iID_Sender ? iID_Target : iID_Sender)
	write_string(szBuffer)
	message_end()
}

// Set entity's rendering type (from fakemeta_util)
stock fm_set_rendering(entity, fx = kRenderFxNone, r = 255, g = 255, b = 255, render = kRenderNormal, amount = 16)
{
	static Float:color[3]
	color[0] = float(r)
	color[1] = float(g)
	color[2] = float(b)
	
	set_pev(entity, pev_renderfx, fx)
	set_pev(entity, pev_rendercolor, color)
	set_pev(entity, pev_rendermode, render)
	set_pev(entity, pev_renderamt, float(amount))
}
