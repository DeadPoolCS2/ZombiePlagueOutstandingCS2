<div align="center"><h1><img width="600" height="131" alt="Han Zombie Plague S2" src="https://github.com/user-attachments/assets/d0316faa-c2d0-478f-a642-1e3c3651f1d4" /></h1></div>

<div align="center"><h1>Zombie Plague for Swiftly2 (CS2)</h1></div>

<div align="center">
A feature-rich Zombie Plague mode plugin for Counter-Strike 2, built on the <strong>SwiftlyS2</strong> framework.  
Supports 10 game modes, custom zombie classes, ammo-pack Extra Items, laser trip-mines, API extensions, and more.
</div>

<div align="center">

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/Z8Z31PY52N)

### Video Preview
https://www.youtube.com/watch?v=DVeR5u28M_s

</div>

---

## Feature Overview

| Feature | Description |
|---------|-------------|
| **10 Game Modes** | Classic infection, multi-infection, boss modes, and more — all configurable |
| **Special Class System** | Mother Zombie, Nemesis, Assassin, Survivor, Sniper, Hero — each with independent stats |
| **Extra Items Shop** | Ammo-pack currency; buy Armor, Grenades, Jetpack, Trip Mines, SCBA Suit, and more |
| **Laser Trip Mines** | Laser beam mines planted via `sw_plant`; auto-detonate when a zombie crosses the beam |
| **Grenades** | Incendiary, Flashbang/Light, Freeze, Teleport, Incendiary Bomb — each with toggles & spawn auto-give |
| **SCBA Suit** | Absorbs one zombie infection; purchasable in Extra Items (toggle: `CanUseScbaSuit`) |
| **Revive Token** | One-time auto-respawn on death |
| **Jetpack** | CTRL+SPACE to fly; right-click to fire rockets at zombies |
| **Multi-Jump & Knife Blink** | Extra jump charges; teleport blink on knife swing |
| **Round Announcements** | Chat summary (credits, player count) at round start; center-screen winner message at round end |
| **Knockback System** | Per-hit-location, per-hero multipliers for fine-grained balance |
| **API Support** | Full `IHanZombiePlagueAPI` for external plugin integration |
| **Sound & Visuals** | Vox broadcast system, ambient music, player glow, FOV adjustment |
| **Database** | Optional MySQL persistence for Ammo Pack balances |

---

## Workshop Assets

```
Sound pack : [3644652779](https://steamcommunity.com/sharedfiles/filedetails/?id=3644652779)
Zombie models: [3170427476](https://steamcommunity.com/sharedfiles/filedetails/?id=3170427476)
Mine model   : https://steamcommunity.com/workshop/filedetails/?id=3618032051
```

---

## Setup / Installation

1. Install [SwiftlyS2](https://github.com/swiftly-solution/swiftly) on your CS2 server.
2. Drop the plugin folder into `addons/swiftlys2/plugins/`.
3. Start (or `sw_reload`) the server.
4. Edit the config files under `configs/plugins/HanZombiePlagueS2/` (see Configuration below).
5. Check the server console for load errors.

---

## Commands List

| Command / Alias | Description |
|-----------------|-------------|
| `sw_zp` / `!zp` / `!menu` | Open the main game menu |
| `sw_buyweapons` / `!buyweapons` | Open the weapon selection menu (CT/alive only) |
| `sw_extras` / `!extras` | Open the Extra Items shop |
| `sw_zclass` / `!zclass` | Open the zombie class preference menu |
| `sw_zmenu` / `!zmenu` | Open the admin item menu (requires `AdminMenuPermission`) |
| `sw_blink` / `!blink` | Activate Knife Blink (consumes 1 charge) |
| `sw_plant` / `!plant` | Plant a laser trip mine at your current position (CT/alive; requires a charge) |
| `sw_take` / `!take` | Recover your nearest planted trip mine (returns 1 charge) |
| `sw_give_ap <name\|#userid> <amount>` | Admin: give Ammo Packs to a player |

> **Weapons Menu**: `sw_buyweapons` is always available to CT players who are alive — there is no per-round single-use lock.

---

## Game Modes

All 10 modes are configured in `HZPMainCFG.jsonc`. Each mode supports:
- `Enable` – toggle on/off
- `Weight` – random-selection weight (higher = more likely)
- `ZombieCanReborn` – whether zombies respawn after death
- `EnableInfiniteClipMode` – infinite ammo for humans in this mode

| # | Mode | Description |
|---|------|-------------|
| 1 | **Normal Infection** | 1 (configurable) Mother Zombie infects the rest |
| 2 | **Multi Infection** | Multiple Mother Zombies start simultaneously |
| 3 | **Survivor** | 1 human Survivor with M249 + special stats; rest are zombies |
| 4 | **Sniper** | 1 human Sniper with AWP + special stats; rest are zombies |
| 5 | **Swarm** | Half the players are instantly zombies (no infection) |
| 6 | **Plague** | Half zombies + 1 Survivor + 1 Nemesis |
| 7 | **Assassin** | 1 invisible Assassin zombie; no infection |
| 8 | **Nemesis** | 1 ultra-powerful Nemesis zombie; no infection |
| 9 | **Hero** | Last X humans become Heroes with extreme stats |
| 10 | **Assassin vs Sniper** | Half zombies + 1 Assassin + 1 Sniper |

---

## Round Announcements

### Round Start (Chat)
At the start of each new round every non-bot player receives a chat message:

```
[HZP] New round begins. | Your credits: X | Players connected: Y
```

- `X` = the player's current Ammo Pack balance
- `Y` = total connected player count

### Round End (Center Screen)
When the round ends a center-screen message is displayed to all players:
- **Humans win**: `Humans WIN !!!`
- **Zombies win**: `Zombies WIN !!!`

Translation keys: `ServerGameHumanWin`, `ServerGameZombieWin`, `RoundStartAnnounce`.

---

## Extra Items Shop

Open with `sw_extras` / `!extras` or via the main game menu.

### Ammo Packs (Currency)

| Source | Amount (configurable) |
|--------|----------------------|
| Connect | `StartingAmmoPacks` (default: 0) |
| Survive a round as human | `RoundSurviveReward` (default: 3) |
| Zombie kills a human | `ZombieKillReward` (default: 2) |
| Human deals 500 damage to zombies | `HumanDamageReward` per `HumanDamageRewardThreshold` damage (default: +1 per 500) |
| Admin grant | `sw_give_ap <player> <amount>` |

### Item Catalogue

Items are defined in `configs/plugins/HanZombiePlagueS2/HZPExtraItemsCFG.jsonc`.
Toggle `"Enable": false` on any entry to hide it without deleting it.
Items whose corresponding **HZPMainCFG feature toggle** is disabled are automatically hidden from the menu.

| Key | Display Name | Team | Default Price | Toggle / Notes |
|-----|-------------|------|--------------|----------------|
| `armor` | Armor (100 points) | Human | 3 AP | Always available |
| `he_grenade` | Incendiary Grenade | Human | 2 AP | Requires `FireGrenade: true` |
| `flash_grenade` | Flashbang / Light Grenade | Human | 2 AP | Requires `LightGrenade: true` |
| `smoke_grenade` | Freeze Grenade | Human | 2 AP | Requires `FreezeGrenade: true` |
| `inc_grenade` | Incendiary Bomb | Human | 4 AP | Requires `SpawnGiveIncGrenade: true` |
| `teleport_grenade` | Teleport Grenade | Human | 3 AP | Requires `TelportGrenade: true` |
| `scba_suit` | SCBA Suit (Hazmat) | Human | 5 AP | Requires `CanUseScbaSuit: true`; absorbs 1 zombie infection |
| `multijump` | Multi-Jump (+1 jump) | Human | 4 AP | Stackable up to `MultijumpMax` |
| `knife_blink` | Knife Blink (3 charges) | Human | 5 AP | Use `sw_blink` / `!blink` |
| `jetpack` | Jetpack | Human | 10 AP | CTRL+SPACE fly; right-click rocket |
| `trip_mine` | Laser Trip Mine | Human | 6 AP | `sw_plant` to plant; `sw_take` to recover |
| `revive_token` | Revive Token | Human | 8 AP | Auto-respawns you once on death |
| `antidote` | Antidote (T-Virus Serum) | Zombie | 8 AP | Turns zombie back to human (special zombies immune) |
| `zombie_madness` | Zombie Madness | Zombie | 6 AP | Temporary invulnerability (configurable duration) |
| `t_virus_grenade` | T-Virus Grenade | Zombie | 6 AP | Infects humans in radius; can infect Heroes if `TVirusCanInfectHero: true` |

---

### Laser Trip Mine Details

| Setting | Value | Notes |
|---------|-------|-------|
| Purchase | 6 AP / charge | Charges model: 1 purchase = +1 charge |
| Plant | `sw_plant` / `!plant` | Consumes 1 charge; places mine in front of the player |
| Recover | `sw_take` / `!take` | Returns 1 charge; owner only (`OwnerOnlyPickup`) |
| Max active per player | 2 (configurable) | |
| Laser beam length | 300 units (configurable) | |
| Explosion radius | 360 units (configurable) | |
| Max damage | 2600 (linear falloff) | Zombies only, no friendly fire |
| Mine HP | 1800; detonates at ≤ 1000 HP | |
| Blocked for | Zombies, Survivor, Sniper, Nemesis, Assassin, Hero | |

Mine visual settings (color, model, sounds, delays) are in the `Mine` section of `configs/plugins/HanZombiePlagueS2/HZPMainCFG.jsonc`.
Combat settings (damage, radius, beam length, etc.) are in `configs/plugins/HanZombiePlagueS2/HZPExtraItemsCFG.jsonc`.

---

### Jetpack Details

- Hold **CTRL + SPACE** to fly upward (consumes fuel).
- Press **right-click (Mouse2)** to fire an explosive rocket (cooldown: 2 s).
- Fuel is reset at round start.
- Configure fuel capacity, thrust force, rocket damage/radius in `HZPExtraItemsCFG.jsonc`.

---

### SCBA Suit Details

- Absorbs **one** zombie infection (knife hit from zombie).
- Purchased from the Extra Items menu (5 AP default) when `CanUseScbaSuit: true` in `HZPMainCFG.jsonc`.
- Only one suit per player per round; re-purchasing while the suit is active is rejected with a refund.
- The suit is destroyed on use and a chat/sound notification is broadcast.

---

## Grenades

All grenades are configured in `HZPMainCFG.jsonc`.

| Grenade | Key | Toggle | Spawn Auto-Give Toggle | Range | Duration | Damage / Effect |
|---------|-----|--------|----------------------|-------|----------|----------------|
| Incendiary Grenade | `he_grenade` | `FireGrenade` | `SpawnGiveFireGrenade` | 300 units | 5 s | 500 initial + 10/s burning |
| Flashbang / Light Grenade | `flash_grenade` | `LightGrenade` | `SpawnGiveLightGrenade` | 1000 units | 30 s | Strong light / blind effect |
| Freeze Grenade | `smoke_grenade` | `FreezeGrenade` | `SpawnGiveFreezeGrenade` | 300 units | 10 s | Freezes target |
| Teleport Grenade | `teleport_grenade` | `TelportGrenade` | `SpawnGiveTelportGrenade` | — | — | Teleports player |
| Incendiary Bomb | `inc_grenade` | — | `SpawnGiveIncGrenade` | — | — | Fire damage area |
| T-Virus Grenade (Zombie) | `t_virus_grenade` | — | — | 300 units | — | Infects humans in radius |

---

## Configuration

### HZPMainCFG.jsonc (Main Settings)

```jsonc
{
  "HZPMainCFG": {
    "RoundReadyTime": 25,          // Prep time before Mother Zombie appears (seconds)
    "RoundTime": 4,                // Round duration (minutes)
    "HumanMaxHealth": 225,
    "HumanInitialSpeed": 1.0,
    "HumanInitialGravity": 0.8,
    "ChatPrefix": "[HZP]",         // Prefix prepended to all chat messages
    "KnockZombieForce": 250.0,
    "StunZombieTime": 0.1,

    // ── Grenades ─────────────────────────────────────────────────────────
    "FireGrenade": true,           // Enable Incendiary Grenade feature
    "SpawnGiveFireGrenade": true,  // Auto-give Incendiary Grenade on spawn
    "FireGrenadeRange": 300.0,
    "FireGrenadeDmg": 500.0,       // Initial damage on detonation
    "FireDmg": 10.0,               // Burn damage per second
    "FireGrenadeDuration": 5.0,    // Burn duration (seconds)

    "LightGrenade": true,          // Enable Flashbang/Light Grenade
    "SpawnGiveLightGrenade": true,
    "LightGrenadeRange": 1000.0,
    "LightGrenadeDuration": 30.0,

    "FreezeGrenade": true,         // Enable Freeze Grenade
    "SpawnGiveFreezeGrenade": true,
    "FreezeGrenadeRange": 300.0,
    "FreezeGrenadeDuration": 10.0,

    "TelportGrenade": true,        // Enable Teleport Grenade
    "SpawnGiveTelportGrenade": true,

    "SpawnGiveIncGrenade": true,   // Auto-give Incendiary Bomb on spawn (CT)

    "CanUseScbaSuit": true,        // Enable SCBA Suit feature
    "TVirusCanInfectHero": true,   // T-Virus Grenade can infect Heroes
    "TVirusGrenadeRange": 300.0,

    // ── Commands ─────────────────────────────────────────────────────────
    "BuyWeaponsCommand": "sw_buyweapons",
    "ExtraItemsCommand": "sw_extras",
    "PlantMineCommand": "sw_plant",
    "TakeMineCommand": "sw_take",
    "KnifeBlinkCommand": "sw_blink",

    // ── Database (optional MySQL for AP persistence) ───────────────────
    "AmmoPacksEnabled": false,
    "AmmoPacksConnectionName": "",
    "AmmoPacksTableName": "hzp_ammo_packs"
  }
}
```

### HZPExtraItemsCFG.jsonc (Extra Items)

```jsonc
{
  "HZPExtraItemsCFG": {
    "StartingAmmoPacks": 0,
    "RoundSurviveReward": 3,
    "ZombieKillReward": 2,
    "HumanDamageRewardThreshold": 500,
    "HumanDamageReward": 1,
    "Items": [
      { "Key": "armor",           "Name": "Armor (100 points)",              "Price": 3,  "Enable": true, "Team": "Human"  },
      { "Key": "he_grenade",      "Name": "Incendiary Grenade",              "Price": 2,  "Enable": true, "Team": "Human"  },
      { "Key": "flash_grenade",   "Name": "Flashbang / Light Grenade",       "Price": 2,  "Enable": true, "Team": "Human"  },
      { "Key": "smoke_grenade",   "Name": "Freeze Grenade",                  "Price": 2,  "Enable": true, "Team": "Human"  },
      { "Key": "inc_grenade",     "Name": "Incendiary Bomb",                 "Price": 4,  "Enable": true, "Team": "Human"  },
      { "Key": "teleport_grenade","Name": "Teleport Grenade",                "Price": 3,  "Enable": true, "Team": "Human"  },
      { "Key": "scba_suit",       "Name": "SCBA Suit (resist 1 zombie hit)", "Price": 5,  "Enable": true, "Team": "Human"  },
      { "Key": "multijump",       "Name": "Multi-Jump (+1 jump)",            "Price": 4,  "Enable": true, "Team": "Human"  },
      { "Key": "knife_blink",     "Name": "Knife Blink (3 charges)",         "Price": 5,  "Enable": true, "Team": "Human"  },
      { "Key": "jetpack",         "Name": "Jetpack",                         "Price": 10, "Enable": true, "Team": "Human"  },
      { "Key": "trip_mine",       "Name": "Laser Trip Mine",                 "Price": 6,  "Enable": true, "Team": "Human"  },
      { "Key": "revive_token",    "Name": "Revive Token",                    "Price": 8,  "Enable": true, "Team": "Human"  },
      { "Key": "antidote",        "Name": "Antidote (T-Virus Serum)",        "Price": 8,  "Enable": true, "Team": "Zombie" },
      { "Key": "zombie_madness",  "Name": "Zombie Madness (invulnerability)","Price": 6,  "Enable": true, "Team": "Zombie" },
      { "Key": "t_virus_grenade", "Name": "T-Virus Grenade",                 "Price": 6,  "Enable": true, "Team": "Zombie" }
    ]
  }
}
```

> **Toggle Behaviour**: Items whose corresponding `HZPMainCFG` toggle is disabled (`false`) are automatically hidden from the shop menu and cannot be purchased.

---

## Zombie Class Configuration

- **`HZPZombieClassCFG.jsonc`** – Normal zombie class list (`ZombieClassList`).
- **`HZPSpecialClassCFG.jsonc`** – Special class list (`SpecialClassList`) used by game modes.

Each class entry:

```jsonc
{
  "Name": "Red Skull",           // Must match the name referenced in HZPMainCFG
  "Enable": true,
  "PrecacheSoundEvent": "soundevents/...",
  "Stats": {
    "Health": 8000,
    "MotherZombieHealth": 18000,
    "Speed": 1.0,
    "Damage": 50.0,
    "Gravity": 0.7,
    "Fov": 110,
    "EnableRegen": true,
    "HpRegenSec": 5.0,
    "HpRegenHp": 30,
    "IdleInterval": 70.0
  },
  "Models": {
    "ModelPath": "characters/models/...",
    "CustomKinfeModelPath": ""
  },
  "Sounds": {
    "SoundInfect": "han.human.mandeath",
    "SoundDeath":  "han.zombie.manclassic_death"
    // ... SoundPain, SoundHurt, IdleSound, RegenSound, BurnSound, HitSound, etc.
  }
}
```

---

## Sound Broadcast System (Vox)

Configuration: `HZPVoxCFG.jsonc` → `VoxList` array.

Each voice package supports:

| Field | Trigger |
|-------|---------|
| `RoundMusicVox` | Round officially starts |
| `CoundDownVox` | Countdown (10 → 1) |
| `ZombieSpawnVox` | Mother Zombie appears |
| `HumanWinVox` | Humans win |
| `ZombieWinVox` | Zombies win |
| `NormalInfectionVox`, `NemesisVox`, etc. | Mode-specific announcements |

Multiple sound keys can be comma-separated — the system picks one at random.

---

## Chat Prefix

All plugin-generated chat messages are routed through the centralized `ChatMsg` helper, which prepends `ChatPrefix` (default: `[HZP]`) exactly once. To change the prefix, set `ChatPrefix` in `HZPMainCFG.jsonc`.

---

## Translations

Translation files are located at:
```
net10.0/HanZombiePlagueS2/resources/translations/en.jsonc
net10.0/HanZombiePlagueS2/resources/translations/zh-CN.jsonc
```

Key translation strings:

| Key | Default (EN) |
|-----|-------------|
| `RoundStartAnnounce` | `New round begins. \| Your credits: {0} \| Players connected: {1}` |
| `ServerGameHumanWin` | `Humans WIN !!!` |
| `ServerGameZombieWin` | `Zombies WIN !!!` |
| `ExtraItemsScbaSuitSuccess` | `You put on a Hazmat Suit and can resist one zombie attack! Ammo Packs remaining: {0}.` |
| `ExtraItemsTripMineSuccess` | `Trip mine charge added (you now have {0} charge(s)). Ammo Packs remaining: {1}.` |
| `TripMinePlanted` | `Mine planted ({0}/{1} active). Zombies crossing the laser beam will trigger the explosion!` |

---

## API Support

Full `IHanZombiePlagueAPI` interface is provided for external plugin integration.  
See [`API/net10.0/HanZombiePlagueAPI.xml`](API/net10.0/HanZombiePlagueAPI.xml) for full documentation.

Key capabilities:
- Listen to events: `HZP_OnPlayerInfect`, `HZP_OnNemesisSelected`, `HZP_OnGameStart`, `HZP_OnHumanWin`, etc.
- Query player state: `IsZombie`, `IsNemesis`, `CurrentMode`, etc.
- Force-set player roles and classes.
- Check win conditions, give props, set glow/FOV/god mode.

---

## Database / Ammo Packs Persistence

### Enabling persistence

Set the following three keys in `configs/plugins/HanZombiePlagueS2/HZPMainCFG.jsonc`:

| Key | Type | Default | Description |
|-----|------|---------|-------------|
| `AmmoPacksEnabled` | bool | `false` | Set to `true` to enable MySQL persistence for Ammo Pack balances. When `false` no database connection is opened and no warnings are emitted. |
| `AmmoPacksConnectionName` | string | `""` | Name of the connection entry to look up in `configs/database.jsonc`. Leave empty to use the value of `default_connection` from that file. |
| `AmmoPacksTableName` | string | `"hzp_ammo_packs"` | MySQL table name for per-player balances (alphanumeric and underscores only). The table is created automatically on first load. |

```jsonc
// HZPMainCFG.jsonc — database section
"AmmoPacksEnabled": true,
"AmmoPacksConnectionName": "",        // empty → uses default_connection from database.jsonc
"AmmoPacksTableName": "hzp_ammo_packs"
```

> **Note**: The startup warning `[HZP-DB] Connection '…' could not be resolved` only appears when `AmmoPacksEnabled` is `true` **and** the specified connection cannot be resolved. If persistence is disabled, no warning is shown.

### configs/database.jsonc — connection formats

`HanZombiePlagueS2` delegates all `configs/database.jsonc` reading to the **SwiftlyS2 database service** (the same API used by K4-Seasons and other SwiftlyS2 plugins), so every format the framework supports is automatically supported:

**Option A – object style** (host/port/database/user/password fields):

```jsonc
{
  "default_connection": "main",
  "connections": {
    "main": {
      "host": "127.0.0.1",
      "port": 3306,
      "database": "zombieplague",
      "user": "root",
      "password": ""
    }
  }
}
```

**Option B – DSN string** (`mysql://user:password@host:port/database`):

```jsonc
{
  "default_connection": "host",
  "connections": {
    "host": "mysql://root:secretpassword@127.0.0.1:3306/zombieplague"
  }
}
```

Both formats are handled natively by SwiftlyS2. The plugin does **not** require any changes to the Swiftly core `configs/database.jsonc`.

### Connection resolution order

1. If `AmmoPacksConnectionName` is non-empty → SwiftlyS2 looks up that key in `connections`.
2. If `AmmoPacksConnectionName` is empty → SwiftlyS2 falls back to `default_connection`.
3. If neither resolves → the plugin logs a warning and skips all DB operations (no crash).

---

## Security & Notes

- The weapons menu (`sw_buyweapons`) is available to any alive CT player at any time — there is no per-round single-use restriction.
- Extra items respect their corresponding `HZPMainCFG` toggles: if a toggle is `false`, the item is hidden from the menu and purchase is rejected with a refund.
- All chat messages are prefixed exactly once via the centralized `ChatMsg` helper.

---
