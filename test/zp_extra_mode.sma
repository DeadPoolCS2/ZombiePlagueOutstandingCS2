#include <amxmodx>
#include <amxmisc>
#include <zombieplague>

native zp_make_user_sniper(id);
native zp_make_user_assassin(id);

// Survivor Item
new const ItemNameSurvivor[] = "Buy Survivor (One Round)";
new ItemCostSurvivor = 3000;
new ItemSurvivor;

// Nemesis Item
new const ItemNameNemesis[] = "Buy Nemesis (One Round)";
new ItemCostNemesis = 3000;
new ItemNemesis;

// Sniper Item
new const ItemNameSniper[] = "Buy Sniper (One Round)";
new ItemCostSniper = 3500;
new ItemSniper;

// Assassin Item
new const ItemNameAssassin[] = "Buy Assassin (One Round)";
new ItemCostAssassin = 3500;
new ItemAssassin;

new bool: OnePerMap [ 33 ];

public plugin_precache() {
	register_plugin("Buy Mode's", "1.0", "Zombie OutStanding v2.3");
	
	ItemSurvivor = zp_register_extra_item(ItemNameSurvivor, ItemCostSurvivor, ZP_TEAM_HUMAN);
	ItemNemesis = zp_register_extra_item(ItemNameNemesis, ItemCostNemesis, ZP_TEAM_HUMAN);
	ItemSniper = zp_register_extra_item(ItemNameSniper, ItemCostSniper, ZP_TEAM_HUMAN);
	ItemAssassin = zp_register_extra_item(ItemNameAssassin, ItemCostAssassin, ZP_TEAM_HUMAN);
}

public zp_extra_item_selected(player, itemid) {
	if(zp_has_round_started())
		return PLUGIN_HANDLED;
	
	if ( OnePerMap [ player ] ) {
		ColorChat(player, "^x04[ZOMBIE.THEXFORCE.RO]^x01 Only one mod per map is allowed!");
		return PLUGIN_HANDLED;
	}
	
	if(itemid == ItemSurvivor) {
		zp_make_user_survivor(player);
		ColorChat(player, "^x04[ZOMBIE.THEXFORCE.RO]^x01 You become an^x03 survivor^x01.");
		OnePerMap [ player ] = true;
	}
	else if(itemid == ItemNemesis) {
		zp_make_user_nemesis(player);
		ColorChat(player, "^x04[ZOMBIE.THEXFORCE.RO]^x01 You become an^x03 nemesis^x01.");
		OnePerMap [ player ] = true;
	}
	else if(itemid == ItemSniper) {
		zp_make_user_sniper(player);
		ColorChat(player, "^x04[ZOMBIE.THEXFORCE.RO]^x01 You become an^x03 sniper^x01.");
		OnePerMap [ player ] = true;
	}
	else if(itemid == ItemAssassin) {
		zp_make_user_assassin(player);
		ColorChat(player, "^x04[ZOMBIE.THEXFORCE.RO]^x01 You become an^x03 assassin^x01.");
		OnePerMap [ player ] = true;
	}
	return PLUGIN_CONTINUE;
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