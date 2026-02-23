using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using static HanZombiePlagueS2.HZPZombieClassCFG;


namespace HanZombiePlagueS2;

public class HZPGlobals
{
    public bool ServerIsEmpty = true;
    public bool GameStart { get; set; }
    public bool SafeRoundStart { get; set; }
    public bool GameInfiniteClipMode { get; set; }
    public bool IsheroSetup { get; set; }
    public int Countdown { get; set; }

    public bool[] InSwing { get; } = new bool[65];

    public HZPVoxCFG.RoundVox? RoundVoxGroup = null;

    public Dictionary<int, bool> IsZombie = new Dictionary<int, bool>();

    public Dictionary<int, bool> IsMother = new Dictionary<int, bool>();
    public Dictionary<int, bool> IsSurvivor = new Dictionary<int, bool>();
    public Dictionary<int, bool> IsSniper = new Dictionary<int, bool>();
    public Dictionary<int, bool> IsNemesis = new Dictionary<int, bool>();
    public Dictionary<int, bool> IsAssassin = new Dictionary<int, bool>();
    public Dictionary<int, bool> IsHero = new Dictionary<int, bool>();

    public CancellationTokenSource? g_hRoundEndTimer { get; set; } = null;
    public CancellationTokenSource? g_hCountdown { get; set; } = null;

    public Dictionary<int, ZombieIdleState> g_ZombieIdleStates = new();
    public CancellationTokenSource? g_IdleTimer { get; set; } = null;

    public Dictionary<IPlayer, (int endTick, int fallEndTick, Vector originalVelocity)> jumpBoostState = new();

    public Dictionary<int, ZombieRegenState> g_ZombieRegenStates = new();

    public CancellationTokenSource? g_ZombieRegenTimer = null;

    public CancellationTokenSource? g_hAmbMusic { get; set; } = null;

    public Dictionary<int, bool> g_IsInvisible = new();

    public Dictionary<CCSPlayerController, GlowEntity> GlowEntity = new Dictionary<CCSPlayerController, GlowEntity>();

    public CancellationTokenSource? AssassinTimer;

    public Dictionary<int, bool> ThrowerIsZombie = new();

    public Dictionary<int, (CParticleSystem particle, CancellationTokenSource timer)> ActiveBurns = new();

    public Dictionary<uint, COmniLight> activeLights = new Dictionary<uint, COmniLight>();
    public Dictionary<uint, CancellationTokenSource> lightTimers = new Dictionary<uint, CancellationTokenSource>();

    public readonly Dictionary<SpawnType, List<SpawnPointData>> spawnCache= new();

    public Dictionary<int, float> StopZombieTimers = new();

    public Dictionary<int, bool> ScbaSuit = new Dictionary<int, bool>();
    public Dictionary<int, bool> GodState = new Dictionary<int, bool>();
    public Dictionary<int, bool> InfiniteAmmoState = new Dictionary<int, bool>();

    public Dictionary<int, bool> CanBuyWeaponsThisRound = new Dictionary<int, bool>();

    // ── Extra Items / Ammo Packs ──────────────────────────────────────────────
    /// <summary>Per-player ammo-pack balance (keyed by PlayerID).</summary>
    public Dictionary<int, int> AmmoPacks = new Dictionary<int, int>();
    /// <summary>
    /// Accumulated damage dealt by each human to zombies this round (keyed by PlayerID).
    /// Reset at round end and on disconnect. Used for damage-based AP reward.
    /// </summary>
    public Dictionary<int, int> DamageAccumulator = new Dictionary<int, int>();

    // ── Multijump ─────────────────────────────────────────────────────────────
    /// <summary>Number of extra jumps currently available to the player this round.</summary>
    public Dictionary<int, int> ExtraJumps = new Dictionary<int, int>();
    /// <summary>Jumps consumed since the player last touched the ground.</summary>
    public Dictionary<int, int> JumpsUsed = new Dictionary<int, int>();

    // ── Knife Blink ───────────────────────────────────────────────────────────
    /// <summary>Remaining knife-blink charges for the player.</summary>
    public Dictionary<int, int> KnifeBlinkCharges = new Dictionary<int, int>();
    /// <summary>Environment.TickCount64 (ms) at which the player's blink cooldown expires.</summary>
    public Dictionary<int, long> KnifeBlinkCooldownEnd = new Dictionary<int, long>();

    // ── Zombie Madness ────────────────────────────────────────────────────────
    /// <summary>True while a zombie has an active Madness (invulnerability) buff.</summary>
    public Dictionary<int, bool> ZombieMadnessActive = new Dictionary<int, bool>();

    // ── Multijump input tracking ──────────────────────────────────────────────
    /// <summary>True if the player had the jump (Space) button pressed in the previous tick.</summary>
    public Dictionary<int, bool> PrevJumpPressed = new Dictionary<int, bool>();

    // ── Jetpack ───────────────────────────────────────────────────────────────
    /// <summary>True if the player currently owns a jetpack.</summary>
    public Dictionary<int, bool> HasJetpack = new Dictionary<int, bool>();
    /// <summary>Remaining fuel for the player's jetpack.</summary>
    public Dictionary<int, float> JetpackFuel = new Dictionary<int, float>();
    /// <summary>Server time (CurrentTime) at which fuel was last consumed.</summary>
    public Dictionary<int, float> JetpackLastFuelTime = new Dictionary<int, float>();
    /// <summary>Server time at which the player's rocket cooldown expires.</summary>
    public Dictionary<int, float> JetpackRocketCooldownEnd = new Dictionary<int, float>();
    /// <summary>True if the player had Attack2 (right-click) pressed in the previous tick.</summary>
    public Dictionary<int, bool> PrevAttack2Pressed = new Dictionary<int, bool>();

    // ── Trip Mines ────────────────────────────────────────────────────────────
    /// <summary>Number of unplanted mine charges the player has.</summary>
    public Dictionary<int, int> TripMineCharges = new Dictionary<int, int>();
    /// <summary>All currently active (not-yet-exploded) trip mines on the map.</summary>
    public List<TripMineData> AllMines = new List<TripMineData>();

    // ── Revive Token ──────────────────────────────────────────────────────────
    /// <summary>True if the player has an active revive token that will trigger on death.</summary>
    public Dictionary<int, bool> HasReviveToken = new Dictionary<int, bool>();

}
public class ZombieRegenState
{
    public int PlayerID;
    public int RegenAmount;       // 每次回血量
    public float RegenInterval;   // 间隔秒数
    public float NextRegenTime;   // 下一次回血时间戳（秒）
}

public class ZombieIdleState
{
    public int PlayerID;
    public float IdleInterval;   // 间隔秒数
    public float NextIdleTime;   // 下一次Idle时间
}

public enum SpawnType
{
    CT,
    T,
    DM
}
public struct SpawnPointData
{
    public Vector Position;
    public QAngle Angle;
}

public class GlowEntity
{
    public CBaseModelEntity? Relay { get; set; } = null;
    public CBaseModelEntity? Glow { get; set; } = null;
}

/// <summary>Represents a single planted trip mine on the map.</summary>
public class TripMineData
{
    public int OwnerId;
    /// <summary>Position of the mine body (on the wall surface).</summary>
    public Vector MinePosition;
    /// <summary>Far end of the laser beam.</summary>
    public Vector BeamEnd;
    /// <summary>The beam entity (laser visual).</summary>
    public CBeam? Beam;
    /// <summary>Particle visual attached at the mine position.</summary>
    public CParticleSystem? Visual;
    /// <summary>Remaining health of the mine.</summary>
    public int Health;
    /// <summary>True once the mine has detonated (pending removal).</summary>
    public bool Exploded;
}


