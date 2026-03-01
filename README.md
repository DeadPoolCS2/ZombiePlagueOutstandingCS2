<div align="center">

<img width="600" height="131" alt="Han Zombie Plague S2" src="https://github.com/user-attachments/assets/d0316faa-c2d0-478f-a642-1e3c3651f1d4" />

<h2>Zombie Outstanding — Counter-Strike 2</h2>

<p>A full-featured Zombie Plague plugin for CS2, built on the <strong>SwiftlyS2</strong> framework.<br>
Ammo Packs are persisted exclusively via the <strong>Economy</strong> plugin — no database setup needed.</p>

**[▶ Video Preview](https://www.youtube.com/watch?v=DVeR5u28M_s)**

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)
[![Framework](https://img.shields.io/badge/Framework-SwiftlyS2-orange)](https://github.com/swiftly-solution/swiftlys2)
[![Economy](https://img.shields.io/badge/Requires-Economy%20Plugin-green)](https://github.com/SwiftlyS2-Plugins/Economy)

</div>

---

## 📋 Table of Contents

1. [Features](#-features)
2. [Dependencies](#-dependencies)
3. [Workshop Assets](#-workshop-assets)
4. [Installation](#-installation)
5. [Commands](#-commands)
6. [Game Modes](#-game-modes)
7. [Zombie Classes](#-zombie-classes)
8. [Special Classes](#-special-classes)
9. [Extra Items Shop](#-extra-items-shop)
10. [Grenades](#-grenades)
11. [Ammo Packs & Rewards](#-ammo-packs--rewards)
12. [Dark Atmosphere](#-dark-atmosphere)
13. [Configuration Reference](#-configuration-reference)
14. [Translations](#-translations)
15. [API](#-api)

---

## ✨ Features

| Feature | Details |
|---------|---------|
| 🗺️ **10 Game Modes** | Infection, Multi-Infection, Nemesis, Survivor, Sniper, Swarm, Plague, Assassin, Hero, Assassin vs Sniper |
| 🧟 **6 Zombie Classes** | Classic Zombie, Raptor, Tight Zombie, Mutant, Predator Blue, Regenerator |
| 👑 **3 Special Classes** | Nemesis, Assassin, Mother Zombie — each with own HP / Speed / Gravity / Damage |
| 🛒 **Extra Items Shop** | Ammo-pack currency; Armor, Grenades, Jetpack, Laser Mine, SCBA Suit, Revive Token, and more |
| 💰 **Damage-Based AP Rewards** | Every N damage dealt to zombies → +AP (configurable) |
| 💣 **Laser Trip Mines** | Plant with `!plant`, recover with `!take`; auto-detonate when a zombie crosses the beam |
| 🚀 **Jetpack** | CTRL+SPACE to fly |
| 🧪 **SCBA Suit** | Absorbs one zombie infection |
| ❤️ **Revive Token** | Auto-respawn once on death |
| 🏃 **Multi-Jump & Knife Blink** | Stackable extra jumps; teleport blink on knife swing |
| ⚡ **Knockback System** | Per-hit-location and per-hero damage multipliers |
| 🌑 **Dark Atmosphere** | Configurable per-server fog (ceață) and screen darkness via tonemap; applied on every map load |
| 💾 **AP Persistence via Economy** | Balances survive reconnects, map changes, and server restarts — handled entirely by the Economy plugin |
| 🔌 **Full Plugin API** | `IHanZombiePlagueAPI` — external plugins can hook events, query state, and set roles |
| 🔊 **Vox / Sound System** | Countdown, mode announcements, win sounds, ambient music |

---

## 📦 Dependencies

> **All dependencies are required.** The plugin will not load correctly if any of them are missing.

| Dependency | Version | Link | Notes |
|------------|---------|------|-------|
| **SwiftlyS2** | latest | [swiftly-solution/swiftlys2](https://github.com/swiftly-solution/swiftlys2) | Core plugin framework |
| **Economy plugin** | latest | [SwiftlyS2-Plugins/Economy](https://github.com/SwiftlyS2-Plugins/Economy) | **Required** — stores all Ammo Pack balances |

### Why Economy?

Ammo Packs are now stored **exclusively** through the Economy plugin. This means:

- ✅ Balances survive reconnects, map changes and server restarts automatically
- ✅ No MySQL / database setup required for this plugin
- ✅ Balances can be shared with other Economy-compatible plugins (e.g. shop, rewards)
- ✅ Economy handles all persistence, loading and saving

### Economy Setup

1. Install the Economy plugin following its own README.
2. Add a wallet kind named `ammopacks` in Economy's configuration (the name is configurable via `EconomyWalletKind` in `HZPMainCFG.jsonc`).
3. That's it — the plugin registers the wallet kind automatically on startup if it doesn't exist.

---

## 🎨 Workshop Assets

| Asset | Workshop ID |
|-------|-------------|
| 🔊 Sound pack | [3644652779](https://steamcommunity.com/sharedfiles/filedetails/?id=3644652779) |
| 🧟 Zombie models | [3170427476](https://steamcommunity.com/sharedfiles/filedetails/?id=3170427476) |
| 💣 Laser mine model | [3618032051](https://steamcommunity.com/workshop/filedetails/?id=3618032051) |

---

## 🚀 Installation

```
1. Install SwiftlyS2 on your CS2 server.
2. Install the Economy plugin and configure it.
3. Copy the plugin folder to:
       addons/swiftlys2/plugins/HanZombiePlagueS2/
4. (Optional) Subscribe to the Workshop assets above.
5. Start / reload the server:  sw_reload
6. Edit configs under:
       configs/plugins/HanZombiePlagueS2/
7. Check the server console for any load errors.
```

### File Layout

```
addons/swiftlys2/plugins/
└── HanZombiePlagueS2/
    └── HanZombiePlagueS2.dll

configs/plugins/
└── HanZombiePlagueS2/
    ├── HZPMainCFG.jsonc          ← Core settings, game modes, commands
    ├── HZPExtraItemsCFG.jsonc    ← Extra items, AP rewards, item prices
    ├── HZPZombieClassCFG.jsonc   ← Zombie class stats & sounds
    ├── HZPSpecialClassCFG.jsonc  ← Nemesis / Survivor / Assassin stats
    ├── HZPWeaponsCFG.jsonc       ← Buy-menu weapon list
    └── HZPVoxCFG.jsonc           ← Round vox / sound group settings

translations/
└── en.jsonc                      ← English strings (copy to translations folder)
```

---

## 💬 Commands

### Player Commands

| Command | Chat Alias | Description |
|---------|-----------|-------------|
| `sw_zp` | `!zp` / `!menu` | Open the main game menu |
| `sw_extras` | `!extras` | Open the Extra Items shop |
| `sw_buyweapons` | `!buyweapons` | Open the weapon buy menu (alive CT only) |
| `sw_zclass` | `!zclass` | Choose your zombie class preference |
| `sw_blink` | `!blink` | Activate Knife Blink (costs 1 charge) |
| `sw_plant` | `!plant` | Plant a laser trip mine at your position |
| `sw_take` | `!take` | Recover your nearest planted mine |

### Admin Commands

| Command | Description | Permission |
|---------|-------------|-----------|
| `sw_zmenu` | Open the admin action menu | `AdminMenuPermission` |

> Command names can be changed freely in `HZPMainCFG.jsonc` under the command keys (`MainMenuCommand`, `ExtraItemsCommand`, etc.).

---

## 🗺️ Game Modes

All modes are configured in `HZPMainCFG.jsonc`. Each supports `Enable`, `Weight`, `ZombieCanReborn`, and `EnableInfiniteClipMode`.

| # | Mode | Description |
|---|------|-------------|
| 1 | 🧟 **Normal Infection** | 1 Mother Zombie infects the rest |
| 2 | 🧟🧟 **Multi Infection** | Multiple Mother Zombies start at once |
| 3 | 💀 **Nemesis** | 1 ultra-powerful Nemesis; no infection |
| 4 | 🏹 **Survivor** | 1 human Survivor (XM1014) vs all zombies |
| 5 | 🎯 **Sniper** | 1 human Sniper (AWP) vs all zombies |
| 6 | 🌊 **Swarm** | Half the players become zombies instantly |
| 7 | ☠️ **Plague** | Half zombies + 1 Nemesis + 1 Survivor |
| 8 | 🥷 **Assassin** | 1 invisible Assassin zombie; no infection |
| 9 | 🦸 **Hero** | Last X humans become Heroes with extreme stats |
| 10 | ⚔️ **Assassin vs Sniper** | Assassin zombie vs Sniper human |

---

## 🧟 Zombie Classes

Configured in `HZPZombieClassCFG.jsonc`. Stats match the original **Zombie Outstanding (ZO) v7.1** class sources.

| Class | HP | Speed | Gravity | Special |
|-------|----|-------|---------|---------|
| 🧟 **Classic Zombie** | 6 000 | 1.16× | 0.60 | Balanced — the default class |
| 🦅 **Raptor** | 4 800 | 1.22× | 1.00 | Fastest zombie |
| �� **Tight Zombie** | 7 500 | 0.88× | 0.80 | High HP, double-jump |
| 👾 **Mutant** | 6 250 | 0.98× | 1.00 | Extra health |
| 💙 **Predator Blue** | 5 600 | 1.12× | 0.80 | Powerful attacker |
| 💉 **Regenerator** | 4 750 | 1.00× | 1.00 | Regenerates 350 HP every 5 s |

> **Speed** is a multiplier relative to default human speed (250 u/s).  
> **MotherZombieHealth** = class HP × 2.5 (from `zp_zombie_first_hp`).

---

## 👑 Special Classes

Configured in `HZPSpecialClassCFG.jsonc`.

| Class | HP | Speed | Gravity | Damage | Used In |
|-------|----|-------|---------|--------|---------|
| 🧟 **Mother Zombie** | 15 000 | 1.16× | 0.60 | 150 | Normal / Multi Infection |
| 💀 **Nemesis** | 120 000 | 1.00× | 0.50 | 250 | Nemesis / Plague |
| 🥷 **Assassin** | 24 000 | 3.50× | 0.50 | 357 | Assassin / AVS |

---

## 🛒 Extra Items Shop

Open with `!extras` or via the main menu (`!zp`). Items are purchased with **Ammo Packs (AP)**.

### Item Catalogue

| Item | Team | Default Price | Description |
|------|------|--------------|-------------|
| 🛡️ **Armor** | Human | 3 AP | Grants 100 armor points |
| 💥 **HE Grenade** | Human | 2 AP | Incendiary grenade |
| ⚡ **Flash Grenade** | Human | 2 AP | Flashbang / light grenade |
| ❄️ **Smoke Grenade** | Human | 2 AP | Freeze grenade |
| 🔥 **Incendiary Bomb** | Human | 4 AP | Area fire damage |
| 🌀 **Teleport Grenade** | Human | 3 AP | Decoy teleporter |
| 🧪 **SCBA Suit** | Human | 5 AP | Absorbs one zombie infection |
| 🦘 **Multi-Jump (+1 jump)** | Human | 4 AP | Stackable, up to `MultijumpMax` |
| 🗡️ **Knife Blink (3 charges)** | Human | 5 AP | Teleport blink on knife swing (`!blink`) |
| 🚀 **Jetpack** | Human | 10 AP | CTRL+SPACE to fly (hold CTRL+SPACE) |
| 💣 **Laser Trip Mine** | Human | 6 AP | `!plant` to set, `!take` to recover |
| ❤️ **Revive Token** | Human | 8 AP | Auto-respawn once on death |
| 💊 **Antidote** | Zombie | 8 AP | Converts zombie back to human |
| 🛡️ **Zombie Madness** | Zombie | 6 AP | Temporary invulnerability (10 s) |
| 🧬 **T-Virus Grenade** | Zombie | 6 AP | Infects humans in radius |

> Items whose corresponding `HZPMainCFG` toggle is `false` are automatically hidden.

---

### 💣 Laser Trip Mine Details

| Setting | Default |
|---------|---------|
| Plant command | `!plant` / `sw_plant` |
| Recover command | `!take` / `sw_take` |
| Max active per player | 2 |
| Beam length | 300 units |
| Explosion radius | 360 units |
| Max damage | 2 600 (linear falloff) |
| Mine HP | 1 800 (detonates at ≤ 1 000 HP) |

Mine visuals (color, model, sounds) → `Mine` section of `HZPMainCFG.jsonc`.

---

### 🚀 Jetpack Details

- Hold **CTRL + SPACE** to fly (consumes fuel).
- Fuel resets every round.
- Configure in `HZPExtraItemsCFG.jsonc`: `JetpackMaxFuel`, `JetpackThrustForce`, `JetpackFuelConsumeRate`.

---

## 💣 Grenades

Configured in `HZPMainCFG.jsonc`.

| Grenade | Toggle | Auto-Give | Range | Duration | Effect |
|---------|--------|-----------|-------|----------|--------|
| 🔥 Incendiary | `FireGrenade` | `SpawnGiveFireGrenade` | 300 u | 8 s | 500 initial + 5/s burn |
| ⚡ Light / Flash | `LightGrenade` | `SpawnGiveLightGrenade` | 1 000 u | 30 s | Blind / light effect |
| ❄️ Freeze | `FreezeGrenade` | `SpawnGiveFreezeGrenade` | 300 u | 6 s | Freezes target |
| 🌀 Teleport | `TelportGrenade` | `SpawnGiveTelportGrenade` | — | — | Teleports player |
| 💣 Incendiary Bomb | — | `SpawnGiveIncGrenade` | — | — | Fire damage area |
| 🧬 T-Virus (Zombie) | — | — | 300 u | — | Infects humans in radius |

---

## 💰 Ammo Packs & Rewards

Ammo Packs (AP) are the in-game currency used to buy Extra Items. All balances are stored and managed by the **Economy plugin** — no reconnect loss, no manual saves needed.

### Earning AP

| Source | Amount | Config Key |
|--------|--------|-----------|
| Survive a round as human | +3 | `RoundSurviveReward` |
| Zombie kills / infects a human | +2 | `ZombieKillReward` |
| Human deals N damage to zombies | +1 per threshold | `HumanDamageRewardThreshold` / `HumanDamageReward` |
| Admin grant | any | Economy plugin admin commands |

> The damage reward stacks: deal 2× the threshold → earn 2× the reward, etc.

### Economy Wallet Kind

AP balances live in a wallet kind configured by `EconomyWalletKind` in `HZPMainCFG.jsonc` (default: `"ammopacks"`). The plugin registers this wallet kind in Economy automatically on startup if it doesn't already exist.

---

## 🌑 Dark Atmosphere

The plugin can apply a dark atmosphere on **every map load** by spawning two CS2 entities:

| Entity | Effect |
|--------|--------|
| `env_fog_controller` | Adds volumetric fog (ceată) with configurable colour, start/end distance, and opacity |
| `env_tonemap_controller2` | Lowers the screen's auto-exposure (makes the entire map darker) |

Both are **disabled by default** and fully configurable in `HZPMainCFG.jsonc` under the `Atmosphere` key.

### Fog Settings

| Key | Default | Description |
|-----|---------|-------------|
| `FogEnable` | `false` | Set `true` to activate fog |
| `FogColor` | `"100,120,130"` | Fog colour in `"R,G,B"` (0–255). Cold grey by default |
| `FogStart` | `400.0` | Distance from camera where fog begins (units) |
| `FogEnd` | `2000.0` | Distance where fog reaches maximum density (units) |
| `FogMaxDensity` | `0.7` | Max opacity: `0.0` = none, `1.0` = fully opaque |

### Darkness Settings

| Key | Default | Description |
|-----|---------|-------------|
| `DarknessEnable` | `false` | Set `true` to override screen exposure |
| `ExposureMin` | `0.1` | Minimum auto-exposure (CS2 default ≈ 0.5). Lower = darker |
| `ExposureMax` | `0.3` | Maximum auto-exposure (CS2 default ≈ 2.0). Lower = darker |

> **Tip:** A good starting combination for a horror atmosphere: `FogEnable: true`, `FogColor: "60,70,80"`, `FogStart: 200`, `FogEnd: 1200`, `FogMaxDensity: 0.8`, `DarknessEnable: true`, `ExposureMin: 0.05`, `ExposureMax: 0.15`.

---

## ⚙️ Configuration Reference

### `HZPMainCFG.jsonc` — Core Settings

```jsonc
{
  "HZPMainCFG": {
    // ── Round timing ────────────────────────────────────────────────────────
    "RoundReadyTime": 22.0,       // Seconds before Mother Zombie appears
    "RoundTime": 4.0,             // Round duration in minutes

    // ── Human base stats ────────────────────────────────────────────────────
    "HumanMaxHealth": 225,
    "HumanInitialSpeed": 1.0,
    "HumanInitialGravity": 0.8,

    // ── Knockback ───────────────────────────────────────────────────────────
    "KnockZombieForce": 250.0,
    "StunZombieTime": 0.1,

    // ── Grenades (each has a toggle + optional auto-give) ───────────────────
    "FireGrenade": true,
    "SpawnGiveFireGrenade": true,
    "LightGrenade": true,
    "SpawnGiveLightGrenade": true,
    "FreezeGrenade": true,
    "SpawnGiveFreezeGrenade": true,
    "TelportGrenade": true,
    "SpawnGiveTelportGrenade": false,

    // ── Special features ────────────────────────────────────────────────────
    "CanUseScbaSuit": true,
    "TVirusCanInfectHero": true,

    // ── Commands (change the trigger word here) ─────────────────────────────
    "MainMenuCommand": "sw_zp",
    "ExtraItemsCommand": "sw_extras",
    "ZombieClassCommand": "sw_zclass",
    "AdminMenuItemCommand": "sw_zmenu",

    // ── Admin ───────────────────────────────────────────────────────────────
    "AdminMenuPermission": "",     // Empty = everyone; or "perm1,perm2"

    // ── Chat ────────────────────────────────────────────────────────────────
    "ChatPrefix": "[HZP]",

    // ── Ammo Packs (Economy plugin) ─────────────────────────────────────────
    "EconomyWalletKind": "ammopacks",

    // ── Atmosphere (fog + darkness) — see "Dark Atmosphere" section ──────────
    "Atmosphere": {
      "FogEnable": false,           // true = spawn env_fog_controller on map load
      "FogColor": "100,120,130",    // "R,G,B" (0–255)
      "FogStart": 400.0,            // distance fog starts (units)
      "FogEnd": 2000.0,             // distance fog reaches max density (units)
      "FogMaxDensity": 0.7,         // 0.0 – 1.0

      "DarknessEnable": false,      // true = spawn env_tonemap_controller2
      "ExposureMin": 0.1,           // lower = darker (CS2 default ≈ 0.5)
      "ExposureMax": 0.3            // lower = darker (CS2 default ≈ 2.0)
    }
  }
}
```

---

### `HZPExtraItemsCFG.jsonc` — Items & AP Rewards

```jsonc
{
  "HZPExtraItemsCFG": {
    // ── AP Rewards ──────────────────────────────────────────────────────────
    "StartingAmmoPacks": 0,               // Initial AP (set in Economy config)
    "RoundSurviveReward": 3,              // AP for surviving a round as human
    "ZombieKillReward": 2,                // AP for a zombie killing a human
    "HumanDamageRewardThreshold": 500,    // Damage dealt needed to earn +AP
    "HumanDamageReward": 1,               // AP earned per threshold crossed

    // ── Item list ───────────────────────────────────────────────────────────
    "Items": [
      {
        "Key": "armor",
        "Name": "Armor",
        "Price": 3,
        "Enable": true,
        "Team": "Human"          // "Human" | "Zombie" | "Both"
      }
      // ... more items
    ]
  }
}
```

---

### `HZPZombieClassCFG.jsonc` — Zombie Class Schema

```jsonc
{
  "HZPZombieClassCFG": {
    "ZombieClassList": [
      {
        "Name": "Classic Zombie",
        "Enable": true,
        "Stats": {
          "Health": 6000,
          "Speed": 1.16,          // Multiplier (1.0 = default human speed)
          "Damage": 60.0,
          "Gravity": 0.6,         // Lower = floatier
          "Fov": 110,
          "EnableRegen": true,
          "HpRegenSec": 5.0,
          "HpRegenHp": 100
        },
        "Models": {
          "ModelPath": "characters/models/..."
        },
        "Sounds": {
          "SoundInfect": "han.human.mandeath",
          "SoundPain":   "han.hl.zombie.pain"
          // ...
        }
      }
    ]
  }
}
```

---

## 🌐 Translations

Translation files live in the `translations/` folder:

```
translations/
└── en.jsonc    ← English (bundled)
```

Key strings:

| Key | Default (EN) |
|-----|-------------|
| `RoundStartAnnounce` | `New round begins. \| Your credits: {0} \| Players connected: {1}` |
| `ServerGameHumanWin` | `Humans WIN !!!` |
| `ServerGameZombieWin` | `Zombies WIN !!!` |
| `APHumanDamageReward` | `You earned {0} Ammo Pack(s) for dealing damage to zombies!` |
| `APZombieKillReward` | `You earned {0} Ammo Pack(s) for infecting a human! Total: {1}` |
| `APRoundSurviveReward` | `You earned {0} Ammo Pack(s) for surviving the round! Total: {1}` |
| `ExtraItemsMenuAP` | `Your Ammo Packs: {0}` |
| `ExtraItemsScbaSuitSuccess` | `You put on a Hazmat Suit and can resist one zombie attack!` |
| `TripMinePlanted` | `Mine planted ({0}/{1} active). Zombies crossing the laser beam will trigger the explosion!` |

---

## 🔌 API

`IHanZombiePlagueAPI` is exposed as a SwiftlyS2 shared interface for external plugin integration.

### Registering

```csharp
public override void UseSharedInterface(IInterfaceManager interfaceManager)
{
    if (interfaceManager.HasSharedInterface("HanZombiePlague"))
    {
        var api = interfaceManager.GetSharedInterface<IHanZombiePlagueAPI>("HanZombiePlague");
        // use api...
    }
}
```

### Capabilities

| Category | Methods / Events |
|----------|-----------------|
| **Events** | `HZP_OnPlayerInfect`, `HZP_OnNemesisSelected`, `HZP_OnGameStart`, `HZP_OnHumanWin`, `HZP_OnZombieWin`, … |
| **Player queries** | `IsZombie`, `IsNemesis`, `IsAssassin`, `IsSurvivor`, `CurrentMode`, … |
| **Actions** | Force-set roles and classes, give/take Ammo Packs, set glow / FOV / god mode |

Full docs: [`src/IHanZombiePlagueAPI/IHanZombiePlagueAPI.cs`](src/IHanZombiePlagueAPI/IHanZombiePlagueAPI.cs)

---

<div align="center">

Remade with ❤️ — based on the original plugin by <em>[H-AN / HanZombiePlagueS2](https://github.com/H-AN/HanZombiePlagueS2)</em>

</div>
