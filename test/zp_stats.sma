#include < amxmodx >
#include < csx >
#include <london_cc>

enum {
	INT_STATS_KILLS = 0,
	INT_STATS_DEATHS
};

public plugin_init ( )
{
	register_plugin ( "ZP PRO STATS", "1.0", "LondoN eXtream" );
	
	register_clcmd ( "say", "CMD_ClientHook" );
	register_clcmd ( "say_team", "CMD_ClientHook" );
}

public CMD_ClientHook ( id )
{
	new CMD [ 11 ];
	read_argv ( 1, CMD, charsmax ( CMD ) );
	
	if ( equali ( CMD, "top", 3 ) || equali ( CMD, "/top", 4 ) )
	{
		new HANDLE_MENU = menu_create ( "\rTOP \w15", "FUNC_MENU_HANDLER" );
		
		new szName [ 32 ], szStats [ 8 ], Body [ 8 ], Temp [ 4096 ], TempNum [ 7 ];
		new Num = get_statsnum ( );
		new i, INT;
		
		if ( Num < 360 )
			INT = Num;
		else
			INT = 360;
			
		for ( i = 0; i < INT; i++ )
		{
			get_stats ( i, szStats, Body, szName, charsmax ( szName ) );
			num_to_str ( i + 1, TempNum, charsmax ( TempNum ) );
			
			static Kills [ 16 ], Deaths [ 16 ];
			AddCommas ( szStats [ INT_STATS_KILLS ], Kills, charsmax ( Kills ) );
			AddCommas ( szStats [ INT_STATS_DEATHS ], Deaths, charsmax ( Deaths ) );
			
			formatex ( Temp, charsmax ( Temp ), "\w(Rank: \r%d\w) %s (\r%s \wKills) (\r%s \wDeaths)", i + 1, szName, Kills, Deaths );
			
			menu_additem ( HANDLE_MENU, Temp );
		}
		
		menu_setprop ( HANDLE_MENU, MPROP_EXIT, MEXIT_ALL );
		menu_display ( id, HANDLE_MENU, 0 );
	}
	
	else if ( equali ( CMD, "rank" ) || equali ( CMD, "/rank" ) )
	{
		new szStats [ 8 ], Body [ 8 ], Name [ 32 ];
		new g_pos = get_user_stats ( id, szStats, Body );
		get_user_name ( id, Name, charsmax ( Name ) );
		
		static Me [ 16 ], Max [ 16 ], Kills [ 16 ], Deaths [ 16 ];
		AddCommas ( g_pos, Me, charsmax ( Me ) );
		AddCommas ( get_statsnum ( ), Max, charsmax ( Max ) );
		AddCommas ( szStats [ INT_STATS_DEATHS ], Deaths, charsmax ( Deaths ) );
		AddCommas ( szStats [ INT_STATS_KILLS ], Kills, charsmax ( Kills ) );
		
		ColorChat ( 0, "Player^x03 %s's^x01 rank is^x03 %s/%s^x01 with^x03 %s^x01 Kills and^x03 %s^x01 Deaths", Name, Me, Max, Kills, Deaths );
	}
	
	return PLUGIN_CONTINUE;
} 

public FUNC_MENU_HANDLER ( id, HANDLE_MENU, item )
{
	menu_destroy ( HANDLE_MENU );
	return PLUGIN_HANDLED;
}


AddCommas(iNum, szOutput[], iLen) {
	static Tmp[15], Pos, Num, Len;
	Tmp[0] = '^0', Pos = Num = Len = 0;

	if(iNum < 0) {
		szOutput[Pos++] = '-';
		iNum = abs(iNum);
	}

	Len = num_to_str(iNum, Tmp, charsmax(Tmp));
	if(Len <=3)
		Pos += copy(szOutput[Pos], iLen, Tmp);
	else {
		while((Num < Len) && (Pos < iLen)) {
			szOutput[Pos++] = Tmp[Num++];
			if((Len - Num) && !((Len - Num) % 3))
				szOutput[Pos++] = ',';
		}

		szOutput[Pos] = EOS;
	}

	return Pos;
}