namespace HanZombiePlagueS2;

/// <summary>
/// Visual and physics settings for the laser trip mine, embedded in HZPMainCFG.jsonc under the "Mine" key.
/// Combat settings (damage, radius, beam length, etc.) live in HZPExtraItemsCFG.jsonc.
/// </summary>
public class HanMineS2CFG
{
    // ── Visual ────────────────────────────────────────────────────────────────
    /// <summary>Workshop model path for the mine body (requires workshop addon 3618032051).</summary>
    public string Model { get; set; } = "models/stk_sentry_guns/lasermine/stk_lasermines_one.vmdl";
    /// <summary>Model yaw angle correction applied when spawning the prop (degrees).</summary>
    public string ModelAngleFix { get; set; } = "90.0";
    /// <summary>Glow color of the mine prop in "R,G,B,A" format (0–255 each).</summary>
    public string GlowColor { get; set; } = "0,255,0,255";
    /// <summary>Laser beam color in "R,G,B,A" format (0–255 each).</summary>
    public string LaserColor { get; set; } = "0,255,0,220";
    /// <summary>Laser beam width (engine units).</summary>
    public string LaserSize { get; set; } = "2.5";

    // ── Sounds ────────────────────────────────────────────────────────────────
    /// <summary>Sound event played when the mine is planted.</summary>
    public string MineOpenSound { get; set; } = "n4a_csdm_sentry.mine_set";
    /// <summary>Sound event played when the laser beam activates.</summary>
    public string LaserOpenSound { get; set; } = "n4a_csdm_sentry.mine_activate";
    /// <summary>Sound event played when the laser is tripped.</summary>
    public string LaserTouchSound { get; set; } = "n4a_csdm_sentry.elrocket_lghtning";
    /// <summary>Sound event file to precache for mine sounds.</summary>
    public string PrecacheSoundEvent { get; set; } = "soundevents/n4a_csdm_sentry.vsndevts";

    // ── Limits ────────────────────────────────────────────────────────────────
    /// <summary>Maximum simultaneously planted mines per player (0 = unlimited).</summary>
    public int Limit { get; set; } = 2;

    // ── Timing ────────────────────────────────────────────────────────────────
    /// <summary>Time in seconds the player must wait after triggering plant before the mine is placed.</summary>
    public float PlantTime { get; set; } = 1.0f;
    /// <summary>Time in seconds the player must wait after triggering take before the mine is recovered.</summary>
    public float TakeTime { get; set; } = 1.0f;

    // ── Mine health ───────────────────────────────────────────────────────────
    /// <summary>Full health of a freshly placed mine.</summary>
    public int MineHealth { get; set; } = 1800;
    /// <summary>HP threshold at or below which the mine explodes when damaged (0 = disable damage-explode).</summary>
    public int ExplodeThreshold { get; set; } = 1000;

    // ── Explosion ─────────────────────────────────────────────────────────────
    /// <summary>Explosion radius (engine units). Matches ZombiePlagueOutstanding 1.6 value.</summary>
    public int ExplorerRadius { get; set; } = 360;
    /// <summary>Maximum explosion damage at the mine centre (linear falloff). Matches ZombiePlagueOutstanding 1.6 value.</summary>
    public float ExplorerDamage { get; set; } = 2600f;

    // ── Beam geometry ─────────────────────────────────────────────────────────
    /// <summary>Length of the laser beam projected from the mine (engine units).</summary>
    public float BeamLength { get; set; } = 300f;
    /// <summary>Distance from the beam line within which a zombie triggers the mine (engine units).</summary>
    public float TripRadius { get; set; } = 40f;
    /// <summary>Forward distance from the player's eye at which the mine is planted (engine units).</summary>
    public float PlantDistance { get; set; } = 80f;

    // ── Ownership rules ───────────────────────────────────────────────────────
    /// <summary>Whether only the mine owner can pick up their own mine with sw_take / !take.</summary>
    public bool OwnerOnlyPickup { get; set; } = true;
    /// <summary>Whether the explosion can damage the owner's teammates (humans).</summary>
    public bool FriendlyFire { get; set; } = false;
}
