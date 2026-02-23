#include amxmodx
#include hamsandwich
#include fakemeta
#include zombieplague

#define ID 666666666

new gs
native zp_get_user_sniper(id);
native zp_get_user_assassin(id);
native zp_get_sniper_count();
native zp_get_assassin_count();

public plugin_init()
{
	set_task(25.0, "rem", _, _, _, "b")
	RegisterHam(Ham_Killed, "player", "playerk",1)
	gs=CreateHudSyncObj()
	register_forward(FM_ClientDisconnect,"disco")
	set_task(60.0 * 4.0, "AddAmmo", .flags="b")
}

public AddAmmo()
{
	for (new i = 1;  i <= get_maxplayers(); i++)
	{
		if(is_user_bot(i) || !is_user_connected(i))	return PLUGIN_HANDLED;

		zp_set_user_ammo_packs(i, zp_get_user_ammo_packs(i) + 4)
		ColorChat ( i, "^x04[ZOMBIE.THEXFORCE.RO]^x01 Because you are^x03 active^x01 on the server, you got^x03 +4 ammo packs^x01.")
	}
	return PLUGIN_CONTINUE;
}

public zp_user_infected_post(id, infector, nemesis)
{
	if (!task_exists(ID))
		set_task(0.72, "inforem", ID)
}

public zp_user_humanized_post(id, survivor)
{
	if (!task_exists(ID))
		set_task(0.72, "inforem", ID)
}

public playerk(victim,killer,gibs)
{
	if (!task_exists(ID))
		set_task(0.72, "inforem", ID)
}

public spawn(id)
{
	if (!is_user_alive(id)) return
	if (!task_exists(ID))
		set_task(0.72, "inforem", ID)
}

public disco(id)
{
	if (!task_exists(ID) && zp_has_round_started())
		set_task(0.72, "inforem", ID)
}

getlasthuman()
{
	for (new i = 1;i<=get_maxplayers();i++)
	{
		if (is_user_alive(i)&&!zp_get_user_zombie(i))
			return i
	}
	return -1
}

getlastzombie()
{
	for (new i = 1;i<=get_maxplayers();i++)
	{
		if (is_user_alive(i)&&zp_get_user_zombie(i))
			return i
	}
	return -1
}

public inforem()
{
	if (!zp_has_round_started()) return

	new z = zp_get_zombie_count()
	new h = zp_get_human_count()

	if (z == 1 && h == 1)
	{
		new lasthuman = getlasthuman()
		new lastzombie = getlastzombie()

		if (lasthuman != -1 && lastzombie != -1)
		{
			new zname[32], hname[32]
			new zhp[32], hhp[32]

			get_user_name(lasthuman, hname, 31)
			get_user_name(lastzombie, zname, 31)

			AddCommas(get_user_health(lasthuman), hhp, 31)
			AddCommas(get_user_health(lastzombie), zhp, 31)

			set_hudmessage(150, 150, 150, 0.10, 0.45, 0, 6.0, 2.0, 0.1, 0.2, -1)
			ShowSyncHudMsg(0, gs, "%s (%s HP)  VS  %s (%s HP)", hname, hhp, zname, zhp)
		}

		return
	}

	if (z == h || !z || !h) return

	if (z < h)
	{
		if (z <= 8)
		{
			set_hudmessage(150, 150, 150, 0.78, 0.68, 2, 6.0, 1.0, 0.1, 0.2, -1)
			ShowSyncHudMsg(0, gs, "%d zombie%s remaining...", z, z==1?"":"s")
		}
	}

	else if (z > h)
	{
		if (h <= 8)
		{
			set_hudmessage(150, 150, 150, 0.78, 0.68, 2, 6.0, 1.0, 0.1, 0.2, -1)
			ShowSyncHudMsg(0, gs, "%d human%s remaining...", h, h==1?"":"s")
		}
	}
}

public rem()
{
	static hp[32],id
	new n = zp_get_nemesis_count()
	new a = zp_get_assassin_count()
	new s = zp_get_sniper_count()
	new v = zp_get_survivor_count()

	if (n == 1 && !a && !s && !v)
	{
		id = getnemesis()
		if (id!=-1)
		{
			AddCommas(get_user_health(id),hp,31)
			ColorChat ( 0, "^x04[ZOMBIE.THEXFORCE.RO]^x01 A^x03 Rapture^x01 reminder^x04 @^x03 Nemesis^x01 still has^x04 %s health points^x01.", hp)
		}
	}

	if (!n && a==1 && !s && !v)
	{
		id = getassassin()
		if (id!=-1)
		{
			AddCommas(get_user_health(id),hp,31)
			ColorChat ( 0, "^x04[ZOMBIE.THEXFORCE.RO]^x01 A^x03 Rapture^x01 reminder^x04 @^x03 Assassin^x01 still has^x04 %s health points^x01.", hp)
		}
	}

	if (!n && !a && 1==s && !v)
	{
		id = getsniper()
		if (id!=-1)
		{
			AddCommas(get_user_health(id),hp,31)
			ColorChat ( 0, "^x04[ZOMBIE.THEXFORCE.RO]^x01 A^x03 Rapture^x01 reminder^x04 @^x03 Sniper^x01 still has^x04 %s health points^x01.", hp)
		}
	}

	if (!n && !a && !s && v==1)
	{
		id = getsurvivor()
		if (id!=-1)
		{
			AddCommas(get_user_health(id),hp,31)
			ColorChat ( 0, "^x04[ZOMBIE.THEXFORCE.RO]^x01 A^x03 Rapture^x01 reminder^x04 @^x03 Survivor^x01 still has^x04 %s health points^x01.", hp)
		}
	}
}

getnemesis()
{
	for (new i = 1;i<=get_maxplayers();i++)
	{
		if(is_user_alive(i)&&zp_get_user_nemesis(i))
			return i
	}
	return -1
}

getassassin()
{
	for (new i = 1;i<=get_maxplayers();i++)
	{
		if(is_user_alive(i)&&zp_get_user_assassin(i))
			return i
	}
	return -1
}

getsniper()
{
	for (new i = 1;i<=get_maxplayers();i++)
	{
		if(is_user_alive(i)&&zp_get_user_sniper(i))
			return i
	}
	return -1
}

getsurvivor()
{
	for (new i = 1;i<=get_maxplayers();i++)
	{
		if(is_user_alive(i)&&zp_get_user_survivor(i))
			return i
	}
	return -1
}

stock ColorChat(const id, const input[], any:...) {
	new count = 1, players[32];
	static msg[191];
	vformat(msg, 190, input, 3);
	
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