#nullable enable
using System;
using Game58date.Gameplay;
using Stride.Core.Serialization.Contents;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Particles;

namespace Game58date.Terrain;

public sealed class WeatherAmbienceController : SyncScript
{
    private const int ParticleCount = 28;

    private readonly WeatherParticleSlot[] particles = new WeatherParticleSlot[ParticleCount];
    private readonly WeatherEffectLibrary effectLibrary = new();
    private readonly System.Collections.Generic.Dictionary<string, ParticleSystem?> particleSystemCache = new();
    private VoxelTerrainWorldRuntime? worldRuntime;
    private EnvironmentVisualFactory? visualFactory;
    private Entity? cameraEntity;
    private WorldLawRuntimeController? worldLawController;
    private Entity? weatherRoot;
    private IContentManager? content;
    private WeatherKind currentWeather = WeatherKind.Clear;
    private float elapsedSeconds;
    private bool isInitialized;

    public void Initialize(
        VoxelTerrainWorldRuntime runtime,
        Entity camera,
        WorldLawRuntimeController? worldLawController,
        EnvironmentVisualFactory visualFactory)
    {
        worldRuntime = runtime;
        cameraEntity = camera;
        this.worldLawController = worldLawController;
        this.visualFactory = visualFactory;
        isInitialized = true;
    }

    public override void Start()
    {
        if (!isInitialized || worldRuntime is null || cameraEntity is null || visualFactory is null)
        {
            throw new InvalidOperationException("Weather ambience controller must be initialized before Start.");
        }

        content = Services.GetService<IContentManager>();
        weatherRoot ??= new Entity("WeatherAmbience");
        Entity.AddChild(weatherRoot);

        if (weatherRoot.Transform.Scale == Vector3.Zero)
        {
            weatherRoot.Transform.Scale = Vector3.One;
        }

        for (int i = 0; i < particles.Length; i++)
        {
            Entity particleEntity = visualFactory.CreateBoxEntity(
                $"WeatherParticle_{i}",
                EnvironmentMaterialKind.Dust,
                Vector3.Zero,
                new Vector3(0.05f));
            weatherRoot.AddChild(particleEntity);
            particles[i] = new WeatherParticleSlot(
                particleEntity,
                Angle: Hash01(i, 17, 701) * MathF.PI * 2f,
                Radius: 1.8f + Hash01(i, 29, 719) * 7.8f,
                VerticalBias: Hash01(i, 43, 733) * 2.4f,
                Speed: 0.55f + Hash01(i, 59, 751) * 1.35f,
                Phase: Hash01(i, 73, 769) * MathF.PI * 2f);
        }

        ApplyWeatherMaterial(currentWeather);
    }

    public override void Update()
    {
        if (worldRuntime is null || cameraEntity is null || weatherRoot is null || visualFactory is null)
        {
            return;
        }

        elapsedSeconds += (float)Game.UpdateTime.Elapsed.TotalSeconds;
        weatherRoot.Transform.Position = cameraEntity.Transform.Position + new Vector3(0f, 2.4f, 0f);

        int worldX = (int)MathF.Floor(cameraEntity.Transform.Position.X);
        int worldZ = (int)MathF.Floor(cameraEntity.Transform.Position.Z);
        WorldSample sample = worldRuntime.SampleSurfaceWorld(worldX, worldZ);
        WeatherKind desiredWeather = ResolveWeather(sample, worldX, worldZ);
        if (desiredWeather != currentWeather)
        {
            currentWeather = desiredWeather;
            ApplyWeatherMaterial(currentWeather);
            TerrainRuntimeLogger.Logger.Info($"Weather ambience switched to {currentWeather} near ({worldX},{worldZ}) biome={sample.Biome}.");
        }

        UpdateParticles();
    }

    public override void Cancel()
    {
        if (weatherRoot is not null)
        {
            weatherRoot.Scene = null;
        }

        base.Cancel();
    }

    private WeatherKind ResolveWeather(WorldSample sample, int worldX, int worldZ)
    {
        float worldTime = worldLawController?.RuntimeState.WorldTimeSeconds ?? elapsedSeconds;
        int weatherTick = (int)MathF.Floor(worldTime / 18f);
        float localCycle = Hash01(worldX / 8, worldZ / 8, weatherTick + worldRuntime!.Settings.Seed);
        float signedCycle = localCycle * 2f - 1f;
        float atmosphere = worldLawController?.RuntimeState.World.Atmosphere ?? 0f;
        float humidity = sample.Moisture * 0.70f + sample.ShoreWeight * 0.12f + atmosphere * 0.28f + signedCycle * 0.10f;

        if ((sample.Biome == BiomeKind.Alpine || sample.Biome == BiomeKind.Mountains) && sample.Temperature < 0.48f && humidity > 0.56f)
        {
            return WeatherKind.Snow;
        }

        if ((sample.Biome == BiomeKind.Shore || sample.Biome == BiomeKind.Wetland) && humidity > 0.54f && atmosphere > 0.16f)
        {
            return WeatherKind.Fog;
        }

        if (humidity > 0.72f)
        {
            return WeatherKind.Rain;
        }

        if (humidity > 0.56f && signedCycle < -0.18f)
        {
            return WeatherKind.Fog;
        }

        if ((sample.Biome == BiomeKind.Hills || sample.Biome == BiomeKind.Scree || sample.Slope > 0.34f) && signedCycle > 0.24f)
        {
            return WeatherKind.Wind;
        }

        return WeatherKind.Clear;
    }

    private void ApplyWeatherMaterial(WeatherKind weatherKind)
    {
        if (visualFactory is null)
        {
            return;
        }

        WeatherEffectDescriptor descriptor = effectLibrary.Get(weatherKind);
        bool particleAssetReady = TryResolveParticleEffect(descriptor);
        SetFallbackParticleVisibility(true);
        EnvironmentMaterialKind materialKind = descriptor.FallbackMaterial;

        foreach (WeatherParticleSlot particle in particles)
        {
            visualFactory.ApplyBoxModel(particle.Entity, materialKind);
        }

        if (particleAssetReady)
        {
            TerrainRuntimeLogger.Logger.Debug($"Weather particle asset ready for {descriptor.Weather}: {descriptor.ParticleAssetKey}");
        }
    }

    private bool TryResolveParticleEffect(WeatherEffectDescriptor descriptor)
    {
        if (descriptor.UsesPlaceholderGeometry || content is null || string.IsNullOrWhiteSpace(descriptor.ParticleAssetKey))
        {
            return false;
        }

        if (!particleSystemCache.TryGetValue(descriptor.ParticleAssetKey, out ParticleSystem? particleSystem))
        {
            try
            {
                particleSystem = content.Load<ParticleSystem>(descriptor.ParticleAssetKey);
            }
            catch
            {
                particleSystem = null;
            }

            particleSystemCache[descriptor.ParticleAssetKey] = particleSystem;
        }

        if (particleSystem is null)
        {
            return false;
        }

        return true;
    }

    private void SetFallbackParticleVisibility(bool visible)
    {
        foreach (WeatherParticleSlot particle in particles)
        {
            if (!visible)
            {
                particle.Entity.Transform.Scale = Vector3.Zero;
            }
        }
    }

    private void UpdateParticles()
    {
        int activeCount = currentWeather switch
        {
            WeatherKind.Clear => 6,
            WeatherKind.Wind => 12,
            WeatherKind.Fog => 20,
            WeatherKind.Snow => 24,
            WeatherKind.Rain => 28,
            _ => 0,
        };

        for (int i = 0; i < particles.Length; i++)
        {
            WeatherParticleSlot slot = particles[i];
            Entity particle = slot.Entity;
            if (i >= activeCount)
            {
                particle.Transform.Scale = Vector3.Zero;
                continue;
            }

            float motion = elapsedSeconds * slot.Speed + slot.Phase;
            float fall = Frac(motion * 0.35f);
            float angle = slot.Angle + elapsedSeconds * 0.08f * slot.Speed;
            float radius = slot.Radius + MathF.Sin(elapsedSeconds * 0.33f + slot.Phase) * 0.45f;

            Vector3 localPosition;
            Vector3 localScale;
            Vector3 localRotation;

            switch (currentWeather)
            {
                case WeatherKind.Rain:
                    localPosition = new Vector3(
                        MathF.Cos(angle) * radius,
                        6.8f - fall * 13.0f,
                        MathF.Sin(angle) * radius);
                    localScale = new Vector3(0.026f, 0.88f + slot.VerticalBias * 0.16f, 0.026f);
                    localRotation = new Vector3(0.16f, angle, 0.05f);
                    break;

                case WeatherKind.Snow:
                    localPosition = new Vector3(
                        MathF.Cos(angle + fall * 1.2f) * (radius * 0.82f),
                        5.6f - fall * 9.2f,
                        MathF.Sin(angle * 0.9f) * (radius * 0.82f));
                    localScale = new Vector3(0.08f + slot.VerticalBias * 0.01f);
                    localRotation = new Vector3(motion, angle * 0.4f, motion * 0.7f);
                    break;

                case WeatherKind.Fog:
                    localPosition = new Vector3(
                        MathF.Cos(angle * 0.28f) * (radius * 0.58f),
                        0.8f + MathF.Sin(motion * 0.55f) * 0.24f + slot.VerticalBias * 0.18f,
                        MathF.Sin(angle * 0.28f) * (radius * 0.58f));
                    localScale = new Vector3(
                        0.78f + slot.VerticalBias * 0.12f,
                        0.20f + slot.VerticalBias * 0.04f,
                        0.52f + slot.VerticalBias * 0.08f);
                    localRotation = new Vector3(0f, angle * 0.2f, 0f);
                    break;

                case WeatherKind.Wind:
                    localPosition = new Vector3(
                        -7.5f + fall * 15f,
                        1.2f + slot.VerticalBias * 0.45f,
                        MathF.Sin(angle) * 4.8f);
                    localScale = new Vector3(0.065f + slot.VerticalBias * 0.01f);
                    localRotation = new Vector3(0.08f, 0.24f, 0.08f);
                    break;

                default:
                    localPosition = new Vector3(
                        MathF.Cos(angle) * MathF.Min(radius, 3.4f),
                        1.4f + MathF.Sin(motion) * 0.36f,
                        MathF.Sin(angle) * MathF.Min(radius, 3.4f));
                    localScale = new Vector3(0.045f + slot.VerticalBias * 0.006f);
                    localRotation = new Vector3(0f, angle * 0.35f, 0f);
                    break;
            }

            particle.Transform.Position = localPosition;
            particle.Transform.Scale = localScale;
            particle.Transform.RotationEulerXYZ = localRotation;
        }
    }

    private static float Frac(float value)
    {
        return value - MathF.Floor(value);
    }

    private static float Hash01(int x, int z, int seed)
    {
        int hash = x * 374761393 + z * 668265263 + seed * 1442695041;
        hash = (hash ^ (hash >> 13)) * 1274126177;
        hash ^= hash >> 16;
        return (hash & 1023) / 1023f;
    }

    private readonly record struct WeatherParticleSlot(
        Entity Entity,
        float Angle,
        float Radius,
        float VerticalBias,
        float Speed,
        float Phase);
}
