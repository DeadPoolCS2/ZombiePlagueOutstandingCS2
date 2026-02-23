<div align="center">
  <h1><img width="600" height="131" alt="Han Zombie Plague S2" src="https://github.com/user-attachments/assets/d0316faa-c2d0-478f-a642-1e3c3651f1d4" /></h1>
  <h2>Zombie Outstanding — Counter-Strike 2</h2>
  <p>
    A full-featured Zombie Plague plugin for CS2, built on the <strong>SwiftlyS2</strong> framework.
  </p>

[![ko-fi](https://ko-fi.com/img/githubbutton_sm.svg)](https://ko-fi.com/Z8Z31PY52N)

**[▶ Video Preview](https://www.youtube.com/watch?v=DVeR5u28M_s)**

</div>

---

## Table of Contents

1. [Features](#features)
2. [Workshop Assets](#workshop-assets)
3. [Installation](#installation)
4. [Commands](#commands)
5. [Game Modes](#game-modes)
6. [Zombie Classes](#zombie-classes)
7. [Special Classes](#special-classes)
8. [Extra Items Shop](#extra-items-shop)
9. [Grenades](#grenades)
10. [Ammo Packs & Rewards](#ammo-packs--rewards)
11. [Configuration Reference](#configuration-reference)
12. [Database / Persistence](#database--persistence)
13. [Translations](#translations)
14. [API](#api)

---

## Features

| Feature | Details |
|---------|---------|
| **10 Game Modes** | Infection, Multi-Infection, Nemesis, Survivor, Sniper, Swarm, Plague, Assassin, Hero, Assassin vs Sniper |
| **6 Zombie Classes** | Classic Zombie, Raptor, Tight Zombie, Mutant, Predator Blue, Regenerator |
| **3 Special Classes** | Nemesis, Assassin, Mother Zombie — each with own HP/Speed/Gravity/Damage |
| **Extra Items Shop** | Ammo-pack currency; Armor, Grenades, Jetpack, Laser Mine, SCBA Suit, Revive Token, and more |
| **Damage-Based AP Rewards** | Every 600 damage dealt to zombies → +1 Ammo Pack |
| **Laser Trip Mines** | Plant with `!plant`, recover with `!take`; auto-detonate when a zombie crosses the beam |
| **Jetpack** | CTRL+SPACE to fly; right-click to fire rockets |
| **SCBA Suit** | Absorbs one zombie infection |
| **Revive Token** | Auto-respawn once on death |
| **Multi-Jump & Knife Blink** | Stackable extra jumps; teleport blink on knife swing |
| **Knockback System** | Per hit-location and per-hero damage multipliers |
| **Ammo Pack Persistence** | Optional MySQL backend — saves and restores AP across sessions |
| **Full Plugin API** | `IHanZombiePlagueAPI` — external plugins can hook events, query state, and set roles |
| **Vox / Sound System** | Countdown, mode announcements, win sounds, ambient music |

---

## Workshop Assets

| Asset | Workshop ID |
|-------|-------------|
| Sound pack | [3644652779](https://steamcommunity.com/sharedfiles/filedetails/?id=3644652779) |
| Zombie models | [3170427476](https://steamcommunity.com/sharedfiles/filedetails/?id=3170427476) |
| Laser mine model | [3618032051](https://steamcommunity.com/workshop/filedetails/?id=3618032051) |

---

## Installation

1. Install **[SwiftlyS2](https://github.com/swiftly-solution/swiftly)** on your CS2 server.
2. Copy the plugin folder into `addons/swiftlys2/plugins/`.
3. (Optional) Subscribe to the Workshop assets above and add them to your server's workshop list.
4. Start or reload the server (`sw_reload`).
5. Edit the configs under `configs/plugins/HanZombiePlagueS2/` (see [Configuration Reference](#configuration-reference)).
6. Check the server console for any load errors.

---

## Commands

| Command | Alias | Description |
|---------|-------|-------------|
| `sw_zp` | `!zp` / `!menu` | Open the main game menu |
| `sw_buyweapons` | `!buyweapons` | Open the weapon buy menu (CT / alive only) |
| `sw_extras` | `!extras` | Open the Extra Items shop |
| `sw_zclass` | `!zclass` | Choose your zombie class preference |
| `sw_zmenu` | `!zmenu` | Admin menu (requires `AdminMenuPermission`) |
| `sw_blink` | `!blink` | Activate Knife Blink (costs 1 charge) |
| `sw_plant` | `!plant` | Plant a laser trip mine at your position |
| `sw_take` | `!take` | Recover your nearest planted mine |
| `sw_give_ap <name\|#userid> <amount>` | — | Admin: grant Ammo Packs to a player |

---

## Game Modes

All modes are configured in `HZPMainCFG.jsonc`. Each supports `Enable`, `Weight`, `ZombieCanReborn`, and `EnableInfiniteClipMode`.

| # | Mode | Description |
|---|------|-------------|
| 1 | **Normal Infection** | 1 Mother Zombie infects the rest |
| 2 | **Multi Infection** | Multiple Mother Zombies start at once |
| 3 | **Nemesis** | 1 ultra-powerful Nemesis; no infection |
| 4 | **Survivor** | 1 human Survivor (XM1014) vs all zombies |
| 5 | **Sniper** | 1 human Sniper (AWP) vs all zombies |
| 6 | **Swarm** | Half the players become zombies instantly |
| 7 | **Plague** | Half zombies + 1 Nemesis + 1 Survivor |
| 8 | **Assassin** | 1 invisible Assassin zombie; no infection |
| 9 | **Hero** | Last X humans become Heroes with extreme stats |
| 10 | **Assassin vs Sniper** | Assassin zombie vs Sniper human |

---

## Zombie Classes

Configured in `HZPZombieClassCFG.jsonc`. Stats are taken directly from the original **Zombie Outstanding (ZO) v7.1** class sources.

| Class | HP | Speed | Gravity | Description |
|-------|----|-------|---------|-------------|
| **Classic Zombie** | 6 000 | 1.16× | 0.60 | Balanced — the default class |
| **Raptor** | 4 800 | 1.22× | 1.00 | Fastest zombie |
| **Tight Zombie** | 7 500 | 0.88× | 0.80 | High HP, double-jump |
| **Mutant** | 6 250 | 0.98× | 1.00 | Extra health |
| **Predator Blue** | 5 600 | 1.12× | 0.80 | Powerful attacker |
| **Regenerator** | 4 750 | 1.00× | 1.00 | Regenerates 350 HP every 5 s |

> **Speed** is a multiplier relative to the default human speed (250 u/s).  
> **MotherZombieHealth** = class HP × 2.5 (from `zp_zombie_first_hp`).

---

## Special Classes

Configured in `HZPSpecialClassCFG.jsonc`.

| Class | HP | Speed | Gravity | Damage | Used By |
|-------|----|-------|---------|--------|---------|
| **Mother Zombie** | 15 000 | 1.16× | 0.60 | 150 | Normal / Multi Infection |
| **Nemesis** | 120 000 | 1.00× | 0.50 | 250 | Nemesis / Plague mode |
| **Assassin** | 24 000 | 3.50× | 0.50 | 357 | Assassin / AVS mode |

> Nemesis and Assassin stats match the original ZO cvars:  
> `zp_nem_health 120000` · `zp_nem_damage 250` · `zp_assassin_health 24000` · `zp_assassin_damage 357`

---

## Extra Items Shop

Open with `!extras` or via the main menu (`!zp`).

### Item Catalogue

| Item | Team | Price | Description |
|------|------|-------|-------------|
| **Armor (100 AP)** | Human | 3 AP | Grants 100 armor points |
| **HE Grenade** | Human | 2 AP | Incendiary grenade |
| **Flash Grenade** | Human | 2 AP | Flashbang / light grenade |
| **Smoke Grenade** | Human | 2 AP | Freeze grenade |
| **Incendiary Bomb** | Human | 4 AP | Area fire damage |
| **Teleport Grenade** | Human | 3 AP | Decoy teleporter |
| **SCBA Suit** | Human | 5 AP | Absorbs one zombie infection |
| **Multi-Jump (+1 jump)** | Human | 4 AP | Stackable, up to `MultijumpMax` |
| **Knife Blink (3 charges)** | Human | 5 AP | Teleport blink on knife swing (`!blink`) |
| **Jetpack** | Human | 10 AP | CTRL+SPACE fly; right-click rocket |
| **Laser Trip Mine** | Human | 6 AP | `!plant` to set, `!take` to recover |
| **Revive Token** | Human | 8 AP | Auto-respawn once on death |
| **Antidote** | Zombie | 8 AP | Converts zombie back to human |
| **Zombie Madness** | Zombie | 6 AP | Temporary invulnerability (10 s) |
| **T-Virus Grenade** | Zombie | 6 AP | Infects humans in radius |

> Items whose corresponding `HZPMainCFG` toggle is `false` are automatically hidden and cannot be purchased.

---

### Laser Trip Mine

| Setting | Value |
|---------|-------|
| Plant command | `!plant` / `sw_plant` |
| Recover command | `!take` / `sw_take` |
| Max active per player | 2 |
| Beam length | 300 units |
| Explosion radius | 360 units |
| Max damage | 2 600 (linear falloff) |
| Mine HP | 1 800 (detonates at ≤ 1 000 HP) |

Mine visuals (color, model, sounds) → `Mine` section of `HZPMainCFG.jsonc`.  
Mine combat stats (damage, radius, beam) → `HZPExtraItemsCFG.jsonc`.

---

### Jetpack

- Hold **CTRL + SPACE** to fly (consumes fuel).
- **Right-click (Mouse2)** to fire a rocket (2 s cooldown).
- Fuel resets every round.
- Configure: `JetpackMaxFuel`, `JetpackThrustForce`, `JetpackRocketDamage`, `JetpackRocketRadius` in `HZPExtraItemsCFG.jsonc`.

---

## Grenades

All grenades are configured in `HZPMainCFG.jsonc`.

| Grenade | Toggle | Auto-Give | Range | Duration | Effect |
|---------|--------|-----------|-------|----------|--------|
| Incendiary Grenade | `FireGrenade` | `SpawnGiveFireGrenade` | 300 u | 5 s | 500 initial + 10/s burn |
| Light / Flashbang | `LightGrenade` | `SpawnGiveLightGrenade` | 1 000 u | 30 s | Blind / light effect |
| Freeze Grenade | `FreezeGrenade` | `SpawnGiveFreezeGrenade` | 300 u | 10 s | Freezes target |
| Teleport Grenade | `TelportGrenade` | `SpawnGiveTelportGrenade` | — | — | Teleports player |
| Incendiary Bomb | — | `SpawnGiveIncGrenade` | — | — | Fire damage area |
| T-Virus Grenade (Zombie) | — | — | 300 u | — | Infects humans in radius |

---

## Ammo Packs & Rewards

Ammo Packs (AP) are the in-game currency used to buy Extra Items.

| Source | Amount | Config Key |
|--------|--------|-----------|
| First connect | 0 | `StartingAmmoPacks` |
| Survive a round as human | +3 | `RoundSurviveReward` |
| Zombie kills / infects a human | +2 | `ZombieKillReward` |
| Human deals 600 damage to zombies | +1 | `HumanDamageRewardThreshold` / `HumanDamageReward` |
| Admin grant | any | `!give_ap <player> <amount>` |

> The damage reward repeats: 600 dmg = +1 AP, 1 200 dmg = +2 AP, etc.  
> AP balances are optionally persisted to MySQL (see [Database](#database--persistence)).

---

## Configuration Reference

### HZPMainCFG.jsonc — Key Settings

```jsonc
{
  "HZPMainCFG": {
    "RoundReadyTime": "22.0",        // Seconds before Mother Zombie appears
    "RoundTime": "4.0",              // Round duration in minutes

    // Human base stats (source: zp_human_health / zp_human_gravity)
    "HumanMaxHealth": 150,
    "HumanInitialSpeed": 1.0,
    "HumanInitialGravity": 1.0,

    // Knockback
    "KnockZombieForce": 250.0,
    "StunZombieTime": 0.1,

    // Grenades (each has a toggle + auto-give toggle)
    "FireGrenade": true,
    "SpawnGiveFireGrenade": true,
    "LightGrenade": true,
    "SpawnGiveLightGrenade": true,
    "FreezeGrenade": true,
    "SpawnGiveFreezeGrenade": true,
    "TelportGrenade": true,
    "SpawnGiveTelportGrenade": false,

    // Special features
    "CanUseScbaSuit": true,
    "TVirusCanInfectHero": true,

    // Chat
    "ChatPrefix": "[HZP]",

    // MySQL Ammo Packs persistence
    "AmmoPacksEnabled": false,
    "AmmoPacksConnectionName": "",
    "AmmoPacksTableName": "hzp_ammo_packs"
  }
}
```

### HZPExtraItemsCFG.jsonc — Ammo Packs & Items

```jsonc
{
  "HZPExtraItemsCFG": {
    "StartingAmmoPacks": 0,
    "RoundSurviveReward": 3,
    "ZombieKillReward": 2,
    "HumanDamageRewardThreshold": 600,  // damage dealt threshold per +1 AP
    "HumanDamageReward": 1,
    "Items": [ /* see Item Catalogue above */ ]
  }
}
```

### HZPZombieClassCFG.jsonc — Zombie Class Schema

```jsonc
{
  "Name": "Classic Zombie",      // must match name referenced in HZPMainCFG
  "Enable": true,
  "PrecacheSoundEvent": "soundevents/...",
  "Stats": {
    "Health": 6000,
    "MotherZombieHealth": 15000,
    "Speed": 1.16,               // multiplier (1.0 = default human speed)
    "Damage": 60.0,
    "Gravity": 0.6,              // multiplier (1.0 = normal, lower = floaty)
    "Fov": 110,
    "EnableRegen": true,
    "HpRegenSec": 5.0,           // seconds between regen ticks
    "HpRegenHp": 100,            // HP restored per tick
    "ZombieSoundVolume": 1.0,
    "IdleInterval": 70.0
  },
  "Models": {
    "ModelPath": "characters/models/...",
    "CustomKinfeModelPath": ""
  },
  "Sounds": {
    "SoundInfect": "han.human.mandeath",
    "SoundPain":   "han.hl.zombie.pain",
    "SoundHurt":   "han.zombie.manclassic_hurt",
    "SoundDeath":  "han.zombie.manclassic_death",
    "IdleSound":   "han.hl.nihilanth.idle,...",
    "RegenSound":  "han.zombie.state.manheal",
    "BurnSound":   "han.zombieplague.zburn",
    "HitSound":    "han.zombie.classic_hit",
    "HitWallSound":"han.zombie.classic_hitwall",
    "SwingSound":  "han.zombie.classic_swing"
  }
}
```

---

## Database / Persistence

Ammo Pack balances can be saved to MySQL so players keep their AP across reconnects.

### Enabling

In `HZPMainCFG.jsonc`:

```jsonc
"AmmoPacksEnabled": true,
"AmmoPacksConnectionName": "",        // leave empty → uses default_connection
"AmmoPacksTableName": "hzp_ammo_packs"
```

The table is created automatically on first load.

### configs/database.jsonc — formats

**Object style:**
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

**DSN string:**
```jsonc
{
  "default_connection": "host",
  "connections": {
    "host": "mysql://root:password@127.0.0.1:3306/zombieplague"
  }
}
```

> If `AmmoPacksEnabled` is `false`, no database connection is opened and no warnings are shown.

---

## Translations

Translation files are kept under `translations/` in the repository root:

```
translations/en.jsonc
```

> **Note:** Only English (`en.jsonc`) is bundled. The build copies translations into
> `resources/translations/` inside the published plugin folder. The `net10.0/` directory
> is **not** committed — it is generated during the release build.

Key strings:

| Key | Default (EN) |
|-----|-------------|
| `RoundStartAnnounce` | `New round begins. \| Your credits: {0} \| Players connected: {1}` |
| `ServerGameHumanWin` | `Humans WIN !!!` |
| `ServerGameZombieWin` | `Zombies WIN !!!` |
| `APHumanDamageReward` | `You earned {0} Ammo Pack(s) for dealing damage to zombies!` |
| `ExtraItemsScbaSuitSuccess` | `You put on a Hazmat Suit and can resist one zombie attack!` |
| `TripMinePlanted` | `Mine planted ({0}/{1} active). Zombies crossing the laser beam will trigger the explosion!` |

---

## API

`IHanZombiePlagueAPI` is exposed for external plugin integration.  
Full docs: [`API/net10.0/HanZombiePlagueAPI.xml`](API/net10.0/HanZombiePlagueAPI.xml) (generated on release)

**Capabilities:**

- **Events:** `HZP_OnPlayerInfect`, `HZP_OnNemesisSelected`, `HZP_OnGameStart`, `HZP_OnHumanWin`, `HZP_OnZombieWin`, and more.
- **Player queries:** `IsZombie`, `IsNemesis`, `IsAssassin`, `IsSurvivor`, `CurrentMode`, etc.
- **Actions:** Force-set roles and classes, give/take Ammo Packs, set glow / FOV / god mode.
