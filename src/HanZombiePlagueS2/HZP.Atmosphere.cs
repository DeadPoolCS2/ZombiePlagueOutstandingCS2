using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace HanZombiePlagueS2;

/// <summary>
/// Applies a configurable dark atmosphere on every map load by spawning:
/// <list type="bullet">
///   <item><description>An <c>env_fog_controller</c> entity for fog (ceață).</description></item>
///   <item><description>An <c>env_tonemap_controller2</c> entity for screen darkness.</description></item>
/// </list>
/// Both are fully configurable from <see cref="AtmosphereCFG"/> in <c>HZPMainCFG.jsonc</c>.
/// </summary>
public class HZPAtmosphere
{
    private readonly ILogger<HZPAtmosphere> _logger;
    private readonly ISwiftlyCore _core;
    private readonly IOptionsMonitor<HZPMainCFG> _mainCFG;

    public HZPAtmosphere(
        ILogger<HZPAtmosphere> logger,
        ISwiftlyCore core,
        IOptionsMonitor<HZPMainCFG> mainCFG)
    {
        _logger = logger;
        _core = core;
        _mainCFG = mainCFG;
    }

    /// <summary>
    /// Called from <see cref="HZPEvents.Event_OnMapLoad"/> on every map transition.
    /// Despawns any entities left from the previous map, then re-creates them.
    /// </summary>
    public void Apply()
    {
        var cfg = _mainCFG.CurrentValue.Atmosphere;

        ApplyFog(cfg);
        ApplyDarkness(cfg);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Fog
    // ─────────────────────────────────────────────────────────────────────────

    private void ApplyFog(AtmosphereCFG cfg)
    {
        // Remove any existing plugin-managed fog controller from a previous map.
        foreach (var existing in _core.EntitySystem.GetAllEntitiesByDesignerName<CFogController>("env_fog_controller"))
        {
            if (existing != null && existing.IsValid)
                existing.AcceptInput<int>("Kill", 0);
        }

        if (!cfg.FogEnable)
            return;

        var fog = _core.EntitySystem.CreateEntityByDesignerName<CFogController>("env_fog_controller");
        if (fog == null || !fog.IsValid)
        {
            _logger.LogWarning("[HZP-Atmosphere] Failed to create env_fog_controller.");
            return;
        }

        fog.DispatchSpawn();

        var color = ParseRgb(cfg.FogColor, 100, 120, 130);

        fog.AcceptInput<bool>("TurnOn", true);
        fog.AcceptInput<float>("SetStartDist", cfg.FogStart);
        fog.AcceptInput<float>("SetEndDist", cfg.FogEnd);
        fog.AcceptInput<float>("SetMaxDensity", Math.Clamp(cfg.FogMaxDensity, 0f, 1f));
        fog.AcceptInput<SwiftlyS2.Shared.Natives.Color>("SetColor", color);
        fog.AcceptInput<SwiftlyS2.Shared.Natives.Color>("SetColorSecondary", color);

        _logger.LogInformation("[HZP-Atmosphere] Fog applied: color={C} start={S} end={E} density={D}",
            cfg.FogColor, cfg.FogStart, cfg.FogEnd, cfg.FogMaxDensity);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Darkness (tonemap)
    // ─────────────────────────────────────────────────────────────────────────

    private void ApplyDarkness(AtmosphereCFG cfg)
    {
        // Remove any existing plugin-managed tonemap controller from a previous map.
        foreach (var existing in _core.EntitySystem.GetAllEntitiesByDesignerName<CTonemapController2Alias_env_tonemap_controller2>("env_tonemap_controller2"))
        {
            if (existing != null && existing.IsValid)
                existing.AcceptInput<int>("Kill", 0);
        }

        if (!cfg.DarknessEnable)
            return;

        var tonemap = _core.EntitySystem.CreateEntityByDesignerName<CTonemapController2Alias_env_tonemap_controller2>("env_tonemap_controller2");
        if (tonemap == null || !tonemap.IsValid)
        {
            _logger.LogWarning("[HZP-Atmosphere] Failed to create env_tonemap_controller2.");
            return;
        }

        float eMin = Math.Max(0.01f, cfg.ExposureMin);
        float eMax = Math.Max(eMin, cfg.ExposureMax);

        tonemap.AutoExposureMin = eMin;
        tonemap.AutoExposureMax = eMax;
        tonemap.AutoExposureMinUpdated();
        tonemap.AutoExposureMaxUpdated();
        tonemap.DispatchSpawn();

        _logger.LogInformation("[HZP-Atmosphere] Darkness applied: exposureMin={Min} exposureMax={Max}",
            eMin, eMax);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static SwiftlyS2.Shared.Natives.Color ParseRgb(string rgb, byte defaultR, byte defaultG, byte defaultB)
    {
        const byte opaque = 255;
        try
        {
            var parts = rgb.Split(',');
            if (parts.Length >= 3)
                return new SwiftlyS2.Shared.Natives.Color(
                    byte.Parse(parts[0].Trim()),
                    byte.Parse(parts[1].Trim()),
                    byte.Parse(parts[2].Trim()),
                    opaque);
        }
        catch { }
        return new SwiftlyS2.Shared.Natives.Color(defaultR, defaultG, defaultB, opaque);
    }
}
