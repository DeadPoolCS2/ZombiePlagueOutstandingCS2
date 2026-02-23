#include < amxmodx >
#include < fun >
#include < fakemeta_util >
#include < zombieplague >

new hp, grv
new ap, ap1, ap2

public plugin_precache ( )
{
	hp = zp_register_extra_item ( "HP (+1000)", 15, ZP_TEAM_HUMAN | ZP_TEAM_ZOMBIE );
	grv = zp_register_extra_item ( "Low Gravity", 15, ZP_TEAM_HUMAN | ZP_TEAM_ZOMBIE );
	ap = zp_register_extra_item ( "AP (+100)", 5, ZP_TEAM_HUMAN );
	ap1 = zp_register_extra_item ( "AP (+200)", 10, ZP_TEAM_HUMAN );
	ap2 = zp_register_extra_item ( "AP (+300)", 12, ZP_TEAM_HUMAN );
}

public zp_extra_item_selected ( id, itemid ) {
	if ( itemid == hp ) {
		set_user_health ( id, get_user_health ( id ) + 1000 )
		ColorChat ( id, "!g[ZOMBIE.THEXFORCE.RO] !nYou bought !gHP(+1000)" );
	}
	
	else if ( itemid == grv ) {
		fm_set_user_gravity ( id, 0.5 );
		ColorChat ( id, "!g[ZOMBIE.THEXFORCE.RO] !nYou bought !gLow Gravity" );
	}
	
	else if ( itemid == ap ) {
		set_user_armor ( id, get_user_armor ( id ) + 100 );
		ColorChat ( id, "!g[ZOMBIE.THEXFORCE.RO] !nYou bought !gArmor (+100)" );
	}
	
	else if ( itemid == ap1 ) {
		set_user_armor ( id, get_user_armor ( id ) + 200 );
		ColorChat ( id, "!g[ZOMBIE.THEXFORCE.RO] !nYou bought !gArmor (+200)" );
	}
	
	else if ( itemid == ap2 ) {
		set_user_armor ( id, get_user_armor ( id ) + 300 );
		ColorChat ( id, "!g[ZOMBIE.THEXFORCE.RO] !nYou bought !gArmor (+300)" );
	}
	
	return 0;
}

stock ColorChat(const id, const input[], any:...) {
	new count = 1, players[32];
	static msg[191];
	vformat(msg, 190, input, 3);
	
	replace_all(msg, 190, "!g", "^4");
	replace_all(msg, 190, "!n", "^3");
	
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
/* AMXX-Studio Notes - DO NOT MODIFY BELOW HERE
*{\\ rtf1\\ ansi\\ ansicpg1252\\ deff0\\ deflang1033{\\ fonttbl{\\ f0\\ fnil Tahoma;}}\n\\ viewkind4\\ uc1\\ pard\\ f0\\ fs16 \n\\ par }
*/
