namespace HanZombiePlagueS2;

/// <summary>Team restriction for an extra item.</summary>
public enum ExtraItemTeam
{
    /// <summary>Purchasable by humans only.</summary>
    Human,
    /// <summary>Purchasable by zombies only.</summary>
    Zombie,
    /// <summary>Purchasable by everyone.</summary>
    Both
}

/// <summary>A single extra item entry loaded from HZPExtraItemsCFG.jsonc.</summary>
public class ExtraItemEntry
{
    /// <summary>Internal unique key (e.g. "armor", "he_grenade").</summary>
    public string Key { get; set; } = string.Empty;
    /// <summary>Display name shown in the menu.</summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>Price in ammo packs.</summary>
    public int Price { get; set; } = 0;
    /// <summary>Whether the item is enabled and shown in the menu.</summary>
    public bool Enable { get; set; } = true;
    /// <summary>Which team can purchase this item.</summary>
    public ExtraItemTeam Team { get; set; } = ExtraItemTeam.Human;
}

/// <summary>Root configuration model for the extra items system.</summary>
public class HZPExtraItemsCFG
{
    // ── Armor ──────────────────────────────────────────────────────────────────
    /// <summary>Armor given to the player (0–100).</summary>
    public int ArmorAmount { get; set; } = 100;

    // ── Multijump ─────────────────────────────────────────────────────────────
    /// <summary>Extra jumps added per Multijump purchase.</summary>
    public int MultijumpIncrement { get; set; } = 1;
    /// <summary>Maximum total extra jumps a player can accumulate.</summary>
    public int MultijumpMax { get; set; } = 3;

    // ── Zombie Madness ────────────────────────────────────────────────────────
    /// <summary>Duration in seconds of the Zombie Madness invulnerability.</summary>
    public float MadnessDuration { get; set; } = 10f;

    // ── Antidote ──────────────────────────────────────────────────────────────
    // (uses the existing TVaccine / MakeHuman logic; no extra scalar needed)

    // ── Knife Blink ───────────────────────────────────────────────────────────
    /// <summary>Number of blink charges given per purchase.</summary>
    public int KnifeBlinkCharges { get; set; } = 3;
    /// <summary>Maximum forward distance for a knife-blink teleport (units).</summary>
    public float KnifeBlinkDistance { get; set; } = 300f;
    /// <summary>Cooldown in seconds between blinks.</summary>
    public float KnifeBlinkCooldown { get; set; } = 2f;

    // ── Ammo Packs ───────────────────────────────────────────────────────────
    /// <summary>Starting ammo packs given to a player when they connect.</summary>
    public int StartingAmmoPacks { get; set; } = 0;
    /// <summary>Ammo packs awarded to a human that survives the round.</summary>
    public int RoundSurviveReward { get; set; } = 3;
    /// <summary>Ammo packs awarded to a zombie that kills a human.</summary>
    public int ZombieKillReward { get; set; } = 2;

    // ── Item list ─────────────────────────────────────────────────────────────
    /// <summary>List of extra items shown in the menu.</summary>
    public List<ExtraItemEntry> Items { get; set; } = new();
}
