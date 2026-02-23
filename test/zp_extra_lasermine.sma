#include < amxmodx >
#include < fakemeta >
#include < fakemeta_util >
#include < fun >
#include < engine >
#include < hamsandwich >
#include < xs >

native zp_get_user_zombie( iPlayer );
native zp_get_user_ammo_packs( iPlayer );
native zp_set_user_ammo_packs( iPlayer, iPacks );
native zp_get_user_nemesis( i );
native zp_get_user_assassin( i );
native zp_is_lnj_round( );
//native Float: HattrickRange( i, j );
#define HattrickRange(%1,%2) entity_range(%1,%2)

new q;

#define MAX_ENTITIES		600
#define MAX_PLAYERS		32
#define MINE_ON			1
#define MINE_OFF			0
#define TASK_CREATE		84765
#define TASK_REMOVE		86766
#define MINE_COST			6
#define MINE_CLASSNAME		"zp_trip_mine"
#define MINE_MODEL_EXPLODE	"sprites/zerogxplode.spr"
#define MINE_MODEL_VIEW		"models/ZombieOutStanding/z_out_mine.mdl"
#define MINE_MODEL_SPRITE	"sprites/shockwave.spr"
#define MINE_SOUND_ACTIVATE	"weapons/mine_activate.wav"
#define MINE_SOUND_CHARGE		"weapons/mine_charge.wav"
#define MINE_SOUND_DEPLOY		"weapons/mine_deploy.wav"
#define MINE_SOUND_EXPLODE		"fvox/flatline.wav"
#define MINE_HEALTH		800.0
#define entity_get_owner(%0)		entity_get_int( %0, EV_INT_iuser2 )
#define entity_get_status(%0)		entity_get_int( %0, EV_INT_iuser1 )
#define entity_get_classname(%0,%1)	entity_get_string( %0, EV_SZ_classname, %1, charsmax( %1 ) )

//#define IP_SERVER_LICENTIAT "93.115.80.103"

const FFADE_IN = 0x0000

new g_iTripMines[ 33 ];
new g_iPlantedMines[ 33 ];
new g_iPlanting[ 33 ];
new g_iRemoving[ 33 ];
new g_hExplode;
new g_exploSpr;
new tripmine_glow;
new gmsgScreenShake;

public plugin_init( )
{
	register_plugin( "[ZP] Trip Mines", "1.0", "Hattrick" );
	
	register_clcmd( "say /lm", "Command_Buy" );
	register_clcmd( "say lm", "Command_Buy" );
	q=get_user_msgid("SayText");
	register_clcmd( "CreateLaser", "Command_Plant" );
	register_clcmd( "TakeLaser", "Command_Take" );
	
	register_logevent( "Event_RoundStart", 2, "1=Round_Start" );
	
	register_think( MINE_CLASSNAME, "Forward_Think" );
	
	tripmine_glow =		register_cvar("zp_tripmine_glow", "1");
	
	gmsgScreenShake = get_user_msgid( "ScreenShake" );
	/*
	new IP_LICENTIAT[20];
	get_user_ip(0, IP_LICENTIAT, 21, 1);

	if(!equal(IP_LICENTIAT, IP_SERVER_LICENTIAT))
	{
		server_print("Nu detii o licenta valida! Pluginul nu ruleaza.")
		pause("ade");
	}
	else
	{
		server_print("Detii o licenta valida! Pluginul functioneaza perfect!")
	}*/
}

public plugin_precache( )
{
	engfunc( EngFunc_PrecacheModel, MINE_MODEL_VIEW );
	
	engfunc( EngFunc_PrecacheSound, MINE_SOUND_ACTIVATE );
	engfunc( EngFunc_PrecacheSound, MINE_SOUND_CHARGE );
	engfunc( EngFunc_PrecacheSound, MINE_SOUND_DEPLOY );
	engfunc( EngFunc_PrecacheSound, MINE_SOUND_EXPLODE );
	
	g_hExplode = engfunc( EngFunc_PrecacheModel, MINE_MODEL_EXPLODE );
	g_exploSpr = engfunc( EngFunc_PrecacheModel, MINE_MODEL_SPRITE );
}

public client_disconnect( iPlayer )
{
	g_iTripMines[ iPlayer ] = 0;
	g_iPlanting[ iPlayer ] = false;
	g_iRemoving[ iPlayer ] = false;
	
	if( g_iPlantedMines[ iPlayer ] )
	{
		Func_RemoveMinesByOwner( iPlayer );
		
		g_iPlantedMines[ iPlayer ] = 0;
	}
	
	remove_task( iPlayer + TASK_REMOVE );
	remove_task( iPlayer + TASK_CREATE );
}

public Command_Buy( iPlayer )
{
	if( !is_user_alive( iPlayer ) )
	{
		Message( iPlayer, "^x04[ZOMBIE.THEXFORCE.RO]^x01 You should be Alive" );
		
		return PLUGIN_CONTINUE;
	}
	
	if( zp_get_user_zombie( iPlayer ) )
	{
		Message( iPlayer, "^x04[ZOMBIE.THEXFORCE.RO]^x01 You should be Human" );
		
		return PLUGIN_CONTINUE;
	}
	
	if( zp_get_user_ammo_packs( iPlayer ) < MINE_COST )
	{
		Message( iPlayer, "^x04[ZOMBIE.THEXFORCE.RO]^x01 You need %i ammo packs", MINE_COST );
		
		return PLUGIN_CONTINUE;
	}
	
	if( zp_is_lnj_round( ) )
	{
		Message( iPlayer, "^x04[ZOMBIE.THEXFORCE.RO]^x01 You can't buy a tripmine into an armageddon round" );
		
		return PLUGIN_CONTINUE;
	}
	
	zp_set_user_ammo_packs( iPlayer, zp_get_user_ammo_packs( iPlayer ) - MINE_COST );
	
	g_iTripMines[ iPlayer ]++;
	
	Message( iPlayer, "^x04[ZOMBIE.THEXFORCE.RO]^x01 You bought a trip mine. Press^x03 P^x01 to plant it or^x03 V^x01 to take it" );
	
	new_cmd("bind p CreateLaser", iPlayer);
	new_cmd("bind v TakeLaser", iPlayer);
	
	return PLUGIN_CONTINUE;
}

public Command_Plant( iPlayer )
{
	if( !is_user_alive( iPlayer ) )
	{
		Message( iPlayer, "^x04[ZOMBIE.THEXFORCE.RO]^x01 You should be Alive" );
		
		return PLUGIN_CONTINUE;
	}
	
	if( zp_get_user_zombie( iPlayer ) )
	{
		Message( iPlayer, "^x04[ZOMBIE.THEXFORCE.RO]^x01 You should be Human" );
		
		return PLUGIN_CONTINUE;
	}
	
	if( !g_iTripMines[ iPlayer ] )
	{
		Message( iPlayer, "^x04[ZOMBIE.THEXFORCE.RO]^x01 You don't have a trip mine to plant" );
		
		return PLUGIN_CONTINUE;
	}
	
	if( g_iPlantedMines[ iPlayer ] > 1 )
	{
		Message( iPlayer, "^x04[ZOMBIE.THEXFORCE.RO]^x01 You can plant only 2 mines" );
		
		return PLUGIN_CONTINUE;
	}
	
	if( zp_is_lnj_round( ) )
	{
		Message( iPlayer, "^x04[ZOMBIE.THEXFORCE.RO]^x01 You can't buy a tripmine into an Armageddon round" );
		
		return PLUGIN_CONTINUE;
	}
	
	if( g_iPlanting[ iPlayer ] || g_iRemoving[ iPlayer ] )
		return PLUGIN_CONTINUE;
	
	if( CanPlant( iPlayer ) ) 
	{
		g_iPlanting[ iPlayer ] = true;
		
		message_begin( MSG_ONE_UNRELIABLE, 108, _, iPlayer );
		write_byte( 1 );
		write_byte( 0 );
		message_end( );
		
		set_task( 1.2, "Func_Plant", iPlayer + TASK_CREATE );
	}
	
	return PLUGIN_CONTINUE;
}

public Command_Take( iPlayer )
{
	if( !is_user_alive( iPlayer ) )
	{
		Message( iPlayer, "^x04[ZOMBIE.THEXFORCE.RO]^x01 You should be Alive" );
		
		return PLUGIN_CONTINUE;
	}
	
	if( zp_get_user_zombie( iPlayer ) )
	{
		Message( iPlayer, "^x04[ZOMBIE.THEXFORCE.RO]^x01 You should be Human" );
		
		return PLUGIN_CONTINUE;
	}
	
	if( !g_iPlantedMines[ iPlayer ] )
	{
		Message( iPlayer, "^x04[ZOMBIE.THEXFORCE.RO]^x01 You don't have a planted mine" );
		
		return PLUGIN_CONTINUE;
	}
	
	if( g_iPlanting[ iPlayer ] || g_iRemoving[ iPlayer ] )
		return PLUGIN_CONTINUE;
	
	if( CanTake( iPlayer ) ) {
		g_iRemoving[ iPlayer ] = true;
		
		message_begin( MSG_ONE_UNRELIABLE, 108, _, iPlayer );
		write_byte( 1 );
		write_byte( 0 );
		message_end( );
		
		set_task( 1.2, "Func_Take", iPlayer + TASK_REMOVE );
	}
	
	return PLUGIN_CONTINUE;
}

public Event_RoundStart( ) 
{
	static iEntity, szClassName[ 32 ], iPlayer;
	for( iEntity = 0; iEntity < MAX_ENTITIES + 1; iEntity++ ) 
	{
		if( !is_valid_ent( iEntity ) )
			continue;
		
		szClassName[ 0 ] = '^0';
		entity_get_classname( iEntity, szClassName );
		
		if( equal( szClassName, MINE_CLASSNAME ) )
			remove_entity( iEntity );
	}
	
	for( iPlayer = 1; iPlayer < 33; iPlayer++ ) 
	{
		g_iTripMines[ iPlayer ] = 0;
		g_iPlantedMines[ iPlayer ] = 0;
	}
}

public Func_Take( iPlayer ) 
{
	iPlayer -= TASK_REMOVE;
	
	g_iRemoving[ iPlayer ] = false;
	
	static iEntity, szClassName[ 32 ], Float: flOwnerOrigin[ 3 ], Float: flEntityOrigin[ 3 ];
	for( iEntity = 0; iEntity < MAX_ENTITIES + 1; iEntity++ ) 
	{
		if( !is_valid_ent( iEntity ) )
			continue;
		
		szClassName[ 0 ] = '^0';
		entity_get_classname( iEntity, szClassName );
		
		if( equal( szClassName, MINE_CLASSNAME ) ) 
		{
			if( entity_get_owner( iEntity ) == iPlayer ) 
			{
				entity_get_vector( iPlayer, EV_VEC_origin, flOwnerOrigin );
				entity_get_vector( iEntity, EV_VEC_origin, flEntityOrigin );
				
				if( get_distance_f( flOwnerOrigin, flEntityOrigin ) < 55.0 ) 
				{
					g_iPlantedMines[ iPlayer ]--;
					g_iTripMines[ iPlayer ]++;
					
					remove_entity( iEntity );
					
					break;
				}
			}
		}
	}
}

public bool: CanTake( iPlayer ) 
{
	static iEntity, szClassName[ 32 ], Float: flOwnerOrigin[ 3 ], Float: flEntityOrigin[ 3 ];
	for( iEntity = 0; iEntity < MAX_ENTITIES + 1; iEntity++ ) 
	{
		if( !is_valid_ent( iEntity ) )
			continue;
		
		szClassName[ 0 ] = '^0';
		entity_get_classname( iEntity, szClassName );
		
		if( equal( szClassName, MINE_CLASSNAME ) ) 
		{
			if( entity_get_owner( iEntity ) == iPlayer ) 
			{
				entity_get_vector( iPlayer, EV_VEC_origin, flOwnerOrigin );
				entity_get_vector( iEntity, EV_VEC_origin, flEntityOrigin );
				
				if( get_distance_f( flOwnerOrigin, flEntityOrigin ) < 55.0 )
					return true;
			}
		}
	}
	
	return false;
}

public bool: CanPlant( iPlayer ) 
{
	static Float: flOrigin[ 3 ];
	entity_get_vector( iPlayer, EV_VEC_origin, flOrigin );
	
	static Float: flTraceDirection[ 3 ], Float: flTraceEnd[ 3 ], Float: flTraceResult[ 3 ], Float: flNormal[ 3 ];
	velocity_by_aim( iPlayer, 64, flTraceDirection );
	flTraceEnd[ 0 ] = flTraceDirection[ 0 ] + flOrigin[ 0 ];
	flTraceEnd[ 1 ] = flTraceDirection[ 1 ] + flOrigin[ 1 ];
	flTraceEnd[ 2 ] = flTraceDirection[ 2 ] + flOrigin[ 2 ];
	
	static Float: flFraction, iTr;
	iTr = 0;
	engfunc( EngFunc_TraceLine, flOrigin, flTraceEnd, 0, iPlayer, iTr );
	get_tr2( iTr, TR_vecEndPos, flTraceResult );
	get_tr2( iTr, TR_vecPlaneNormal, flNormal );
	get_tr2( iTr, TR_flFraction, flFraction );
	
	if( flFraction >= 1.0 ) {
		Message( iPlayer, "^x04[ZOMBIE.THEXFORCE.RO]^x01 You must plant the tripmine on a wall" );
		
		
		return false;
	}
	
	return true;
}

public Func_Plant( iPlayer ) 
{
	iPlayer -= TASK_CREATE;
	
	g_iPlanting[ iPlayer ] = false;
	
	static Float: flOrigin[ 3 ];
	entity_get_vector( iPlayer, EV_VEC_origin, flOrigin );
	
	static Float: flTraceDirection[ 3 ], Float: flTraceEnd[ 3 ], Float: flTraceResult[ 3 ], Float: flNormal[ 3 ];
	velocity_by_aim( iPlayer, 128, flTraceDirection );
	flTraceEnd[ 0 ] = flTraceDirection[ 0 ] + flOrigin[ 0 ];
	flTraceEnd[ 1 ] = flTraceDirection[ 1 ] + flOrigin[ 1 ];
	flTraceEnd[ 2 ] = flTraceDirection[ 2 ] + flOrigin[ 2 ];
	
	static Float: flFraction, iTr;
	iTr = 0;
	engfunc( EngFunc_TraceLine, flOrigin, flTraceEnd, 0, iPlayer, iTr );
	get_tr2( iTr, TR_vecEndPos, flTraceResult );
	get_tr2( iTr, TR_vecPlaneNormal, flNormal );
	get_tr2( iTr, TR_flFraction, flFraction );
	
	static iEntity;
	iEntity = create_entity( "info_target" );
	
	if( !iEntity )
		return;
	
	entity_set_string( iEntity, EV_SZ_classname, MINE_CLASSNAME );
	entity_set_model( iEntity, MINE_MODEL_VIEW );
	entity_set_size( iEntity, Float: { -4.0, -4.0, -4.0 }, Float: { 4.0, 4.0, 4.0 } );
	
	if (get_pcvar_num(tripmine_glow))
	{
		fm_set_rendering( iEntity, kRenderFxGlowShell, 0, 120, 240, kRenderNormal, 13 )
	}
	
	entity_set_int( iEntity, EV_INT_iuser2, iPlayer );
	
	g_iPlantedMines[ iPlayer ]++;

	set_pev( iEntity, pev_iuser3, g_iPlantedMines[ iPlayer ] );
	
	entity_set_float( iEntity, EV_FL_frame, 0.0 );
	entity_set_float( iEntity, EV_FL_framerate, 0.0 );
	entity_set_int( iEntity, EV_INT_movetype, MOVETYPE_FLY );
	entity_set_int( iEntity, EV_INT_solid, SOLID_NOT );
	entity_set_int( iEntity, EV_INT_body, 3 );
	entity_set_int( iEntity, EV_INT_sequence, 7 );
	entity_set_float( iEntity, EV_FL_takedamage, DAMAGE_NO );
	entity_set_int( iEntity, EV_INT_iuser1, MINE_OFF );
	
	static Float: flNewOrigin[ 3 ], Float: flEntAngles[ 3 ];
	flNewOrigin[ 0 ] = flTraceResult[ 0 ] + ( flNormal[ 0 ] * 8.0 );
	flNewOrigin[ 1 ] = flTraceResult[ 1 ] + ( flNormal[ 1 ] * 8.0 );
	flNewOrigin[ 2 ] = flTraceResult[ 2 ] + ( flNormal[ 2 ] * 8.0 );
	
	entity_set_origin( iEntity, flNewOrigin );
	
	vector_to_angle( flNormal, flEntAngles );
	entity_set_vector( iEntity, EV_VEC_angles, flEntAngles );
	flEntAngles[ 0 ] *= -1.0;
	flEntAngles[ 1 ] *= -1.0;
	flEntAngles[ 2 ] *= -1.0;
	entity_set_vector( iEntity, EV_VEC_v_angle, flEntAngles );
	
	g_iTripMines[ iPlayer ]--;
	
	emit_sound( iEntity, CHAN_WEAPON, MINE_SOUND_DEPLOY, VOL_NORM, ATTN_NORM, 0, PITCH_NORM );
	emit_sound( iEntity, CHAN_VOICE, MINE_SOUND_CHARGE, VOL_NORM, ATTN_NORM, 0, PITCH_NORM );
	
	entity_set_float( iEntity, EV_FL_nextthink, get_gametime( ) + 0.6 );
}

public Func_RemoveMinesByOwner( iPlayer ) 
{
	static iEntity, szClassName[ 32 ];
	for( iEntity = 0; iEntity < MAX_ENTITIES + 1; iEntity++ ) 
	{
		if( !is_valid_ent( iEntity ) )
			continue;
		
		szClassName[ 0 ] = '^0';
		entity_get_classname( iEntity, szClassName );
		
		if( equal( szClassName, MINE_CLASSNAME ) )
			if( entity_get_int( iEntity, EV_INT_iuser2 ) == iPlayer )
				remove_entity( iEntity );
	}
}

Func_Explode( iEntity ) 
{
	g_iPlantedMines[ entity_get_owner( iEntity ) ]--;
	
	static Float: flOrigin[ 3 ], Float: flZombieOrigin[ 3 ], Float: flVelocity[ 3 ];
	entity_get_vector( iEntity, EV_VEC_origin, flOrigin );

	message_begin( MSG_BROADCAST, SVC_TEMPENTITY );
	write_byte( TE_EXPLOSION );
	engfunc( EngFunc_WriteCoord, flOrigin[ 0 ] );
	engfunc( EngFunc_WriteCoord, flOrigin[ 1 ] );
	engfunc( EngFunc_WriteCoord, flOrigin[ 2 ] );
	write_short( g_hExplode );
	emit_sound( iEntity, CHAN_WEAPON, MINE_SOUND_EXPLODE, VOL_NORM, ATTN_NORM, 0, PITCH_NORM );
	write_byte( 55 );
	write_byte( 15 );
	write_byte( 0 );
	message_end( );
	
	message_begin( MSG_BROADCAST, SVC_TEMPENTITY );
	write_byte( TE_EXPLOSION );
	engfunc( EngFunc_WriteCoord, flOrigin[ 0 ] );
	engfunc( EngFunc_WriteCoord, flOrigin[ 1 ] );
	engfunc( EngFunc_WriteCoord, flOrigin[ 2 ] );
	write_short( g_hExplode );
	emit_sound( iEntity, CHAN_WEAPON, MINE_SOUND_EXPLODE, VOL_NORM, ATTN_NORM, 0, PITCH_NORM );
	write_byte( 65 );
	write_byte( 15 );
	write_byte( 0 );
	message_end( );
	
	message_begin( MSG_BROADCAST, SVC_TEMPENTITY );
	write_byte( TE_EXPLOSION );
	engfunc( EngFunc_WriteCoord, flOrigin[ 0 ] );
	engfunc( EngFunc_WriteCoord, flOrigin[ 1 ] );
	engfunc( EngFunc_WriteCoord, flOrigin[ 2 ] );
	write_short( g_hExplode );
	emit_sound( iEntity, CHAN_WEAPON, MINE_SOUND_EXPLODE, VOL_NORM, ATTN_NORM, 0, PITCH_NORM );
	write_byte( 85 );
	write_byte( 15 );
	write_byte( 0 );
	message_end( );
	
	message_begin( MSG_BROADCAST, SVC_TEMPENTITY );
	write_byte( TE_BEAMCYLINDER); // TE id
	engfunc( EngFunc_WriteCoord, flOrigin[0] ); // x
	engfunc( EngFunc_WriteCoord, flOrigin[1] ); // y
	engfunc( EngFunc_WriteCoord, flOrigin[2] ); // z
	engfunc( EngFunc_WriteCoord, flOrigin[0] ); // x axis
	engfunc( EngFunc_WriteCoord, flOrigin[1] ); // y axis
	engfunc( EngFunc_WriteCoord, flOrigin[2] + 400.0 ); // z axis
	write_short( g_exploSpr ); // sprite
	write_byte( 0 ); // startframe
	write_byte( 0 ); // framerate
	write_byte( 4 ); // life
	write_byte( 60 ); // width
	write_byte( 0 ); // noise
	write_byte( 121 ); // red
	write_byte( 121 ); // green
	write_byte( 121 ); // blue
	write_byte( 200 ); // brightness
	write_byte( 0 ); // speed
	message_end( );
	
	message_begin( MSG_BROADCAST, SVC_TEMPENTITY );
	write_byte( TE_BEAMCYLINDER); // TE id
	engfunc( EngFunc_WriteCoord, flOrigin[0] ); // x
	engfunc( EngFunc_WriteCoord, flOrigin[1] ); // y
	engfunc( EngFunc_WriteCoord, flOrigin[2] ); // z
	engfunc( EngFunc_WriteCoord, flOrigin[0] ); // x axis
	engfunc( EngFunc_WriteCoord, flOrigin[1] ); // y axis
	engfunc( EngFunc_WriteCoord, flOrigin[2] + 700.0 ); // z axis
	write_short( g_exploSpr ); // sprite
	write_byte( 0 ); // startframe
	write_byte( 0 ); // framerate
	write_byte( 4 ); // life
	write_byte( 110 ); // width
	write_byte( 0 ); // noise
	write_byte( 121 ); // red
	write_byte( 121 ); // green
	write_byte( 121 ); // blue
	write_byte( 200 ); // brightness
	write_byte( 0 ); // speed
	message_end( );
	
	message_begin( MSG_BROADCAST, SVC_TEMPENTITY );
	write_byte( TE_BEAMCYLINDER); // TE id
	engfunc( EngFunc_WriteCoord, flOrigin[0] ); // x
	engfunc( EngFunc_WriteCoord, flOrigin[1] ); // y
	engfunc( EngFunc_WriteCoord, flOrigin[2] ); // z
	engfunc( EngFunc_WriteCoord, flOrigin[0] ); // x axis
	engfunc( EngFunc_WriteCoord, flOrigin[1] ); // y axis
	engfunc( EngFunc_WriteCoord, flOrigin[2] + 900.0 ); // z axis
	write_short( g_exploSpr ); // sprite
	write_byte( 0 ); // startframe
	write_byte( 0 ); // framerate
	write_byte( 4 ); // life
	write_byte( 160 ); // width
	write_byte( 0 ); // noise
	write_byte( 121 ); // red
	write_byte( 121 ); // green
	write_byte( 121 ); // blue
	write_byte( 200 ); // brightness
	write_byte( 0 ); // speed
	message_end( );
	
	static iZombie;
	for( iZombie = 1; iZombie < MAX_PLAYERS + 1; iZombie++ ) 
	{
		if( is_user_connected( iZombie ) ) 
		{
			if( is_user_alive( iZombie ) ) 
			{
				entity_get_vector( iZombie, EV_VEC_origin, flZombieOrigin );
				
				if( get_distance_f( flOrigin, flZombieOrigin ) < 340.0 ) 
				{
					entity_get_vector( iZombie, EV_VEC_velocity, flVelocity );
					
					flVelocity[ 2 ] += 240.0;
					flVelocity[ 1 ] += 200.0;
					flVelocity[ 0 ] += 160.0;
					
					entity_set_vector( iZombie, EV_VEC_velocity, flVelocity );
				}
			}
		}
	}
	
	for( new i = 1; i < 33; i++ )
	{
		if( !is_user_connected( i ) || !is_user_alive( i ) ) continue;
		if( zp_get_user_zombie( i ) )
		{
			static Float: fDistance, Float: fDamage;

			fDistance = HattrickRange( i, iEntity );

			if( fDistance < 340 )
			{
				fDamage = 2850.0 - fDistance;

				static Float: fVelocity[ 3 ];
				pev( i, pev_velocity, fVelocity );

				xs_vec_mul_scalar( fVelocity, 1.75, fVelocity );

				set_pev( i, pev_velocity, fVelocity );

				message_begin( MSG_ONE_UNRELIABLE, get_user_msgid( "ScreenFade" ), _, i );
				write_short( 4096 );
				write_short( 4096 );
				write_short( FFADE_IN );
				write_byte( 220 );
				write_byte( 0 );
				write_byte( 0 );
				write_byte( fDistance < 220 ? 220 : 205 );
				message_end( );
				
				message_begin( MSG_ONE_UNRELIABLE, gmsgScreenShake, _, i );
				write_short( 4096 * 100 ); // amplitude
				write_short( 4096 * 500 ); // duration
				write_short( 4096 * 200 ); // frequency
				message_end( );

				if( float( get_user_health( i ) ) - fDamage > 0 )
				{
					ExecuteHamB( Ham_TakeDamage, i, iEntity, entity_get_owner( iEntity ), fDamage, DMG_BLAST );
				}
				else 
				{
					ExecuteHamB( Ham_Killed, i, entity_get_owner( iEntity ), 2 );
				}
				
				if( !zp_get_user_nemesis( i ) && !zp_get_user_assassin( i ) )
					fDamage *= 0.75;

				static cName[ 32 ]; get_user_name( i, cName, 31 );
				Message( entity_get_owner( iEntity ), "^x04[ZOMBIE.THEXFORCE.RO]^x01 Damage to^x04 %s^x01 ::^x04 %0.0f^x01 damage", cName, fDamage );
			}
		}
	}

	for( new i = 1; i < 33; i++ )
	{
		if( !is_user_connected( i ) || !is_user_alive( i ) )
			continue;
		if( !zp_get_user_zombie( i ) )
		{
			message_begin( MSG_ONE_UNRELIABLE, gmsgScreenShake, _, i );
			write_short( 4096 * 3 );
			write_short( 4096 * 2 );
			write_short( 4096 * 4 );
			message_end( );
			
			if( HattrickRange( i, iEntity ) < 340 )
			{
				static Float: fVelocity[ 3 ];
				pev( i, pev_velocity, fVelocity );

				xs_vec_mul_scalar( fVelocity, 1.5, fVelocity );

				set_pev( i, pev_velocity, fVelocity );
			}
		}
	}

	remove_entity( iEntity );
}

public Forward_Think( iEntity ) 
{
	static Float: flGameTime, iStatus;
	flGameTime = get_gametime( );
	iStatus = entity_get_status( iEntity );
	
	switch( iStatus ) 
	{
		case MINE_OFF: 
		{
			entity_set_int( iEntity, EV_INT_iuser1, MINE_ON );
			entity_set_float( iEntity, EV_FL_takedamage, DAMAGE_YES );
			entity_set_int( iEntity, EV_INT_solid, SOLID_BBOX );
			entity_set_float( iEntity, EV_FL_health, MINE_HEALTH + 1000.0 );
			
			emit_sound( iEntity, CHAN_VOICE, MINE_SOUND_ACTIVATE, VOL_NORM, ATTN_NORM, 0, PITCH_NORM );
		}
		
		case MINE_ON: 
		{
			static Float: flHealth;
			flHealth = entity_get_float( iEntity, EV_FL_health );

			if( is_user_alive( entity_get_owner( iEntity ) ) )
			{
				if( entity_get_owner( iEntity ) )
				{
					if( pev( iEntity, pev_iuser3) == 1 )
					{
						set_hudmessage(0, 200, 100, 0.05, 0.3, 0, 0.0, 0.12, 2.0, 1.0, -1),
						show_hudmessage( entity_get_owner( iEntity ), "Lasermine #1 HP: %0.0f", flHealth - 1000.0 );
					}
					else
					{
						set_hudmessage(0, 200, 100, 0.05, 0.33, 0, 0.0, 0.12, 2.0, 1.0, -1),
						show_hudmessage( entity_get_owner( iEntity ), "Lasermine #2 HP: %0.0f", flHealth - 1000.0 );
					}
				}
				
				if( flHealth <= 1000.0 ) 
				{
					Func_Explode( iEntity );
				
					return FMRES_IGNORED;
				}
			}
		}
	}
	
	if( is_valid_ent( iEntity ) )
		entity_set_float( iEntity, EV_FL_nextthink, flGameTime + 0.1 );
	
	return FMRES_IGNORED;
}

Message( v, c[ ], any: ... )
{
	static cBuffer[ 192 ];
	vformat( cBuffer, 191, c, 3 );

	if( v )
	{
		message_begin( MSG_ONE_UNRELIABLE, q, _, v );
		write_byte( v );
		write_string( cBuffer );
		message_end( );
	}

	else
	{
		static i[ 32 ], j, k;
		get_players( i, j, "ch" );
		for( k = 0; k < j; k++ )
		{
			message_begin( MSG_ONE_UNRELIABLE, q, _, i[ k ] );
			write_byte( i[ k ] );
			write_string( cBuffer );
			message_end( );
		}
	}
}

stock new_cmd(const text[], iPlayer = 0)
{
	message_begin(MSG_ONE, 51, _, iPlayer);
	write_byte(strlen(text) + 2);
	write_byte(10);
	write_string(text);
	message_end();
}