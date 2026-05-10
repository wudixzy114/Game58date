#nullable enable
using System;
using System.Collections.Generic;
using Game58date.Gameplay;
using Stride.Core.Serialization.Contents;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering.Compositing;
using Stride.Rendering.Images;
using Stride.Rendering.Lights;

namespace Game58date.Terrain;

public sealed class WeatherAmbienceController : SyncScript
{
    private const int ParticleCount = 28;

    private readonly WeatherParticleSlot[] particles = new WeatherParticleSlot[ParticleCount];
    private readonly WeatherEffectLibrary effectLibrary = new();
    private readonly WeatherStateResolver weatherStateResolver = new();
    private readonly Dictionary<string, Prefab?> effectPrefabCache = new();
    private readonly List<ForwardRenderer> forwardRenderers = new();
    private readonly WeatherRuntimeState runtimeState = CreateDefaultState();
    private readonly WeatherRuntimeState sourceState = CreateDefaultState();
    private readonly WeatherRuntimeState targetState = CreateDefaultState();
    private VoxelTerrainWorldRuntime? worldRuntime;
    private EnvironmentVisualFactory? visualFactory;
    private Entity? cameraEntity;
    private WorldLawRuntimeController? worldLawController;
    private Entity? weatherRoot;
    private IContentManager? content;
    private LightComponent? directionalLightComponent;
    private Color3 baseLightColor = new(1.0f, 0.98f, 0.94f);
    private float baseLightIntensity = 14f;
    private WeatherKind materialWeather = WeatherKind.Clear;
    private Entity? activeWeatherEffectEntity;
    private string? activeWeatherEffectKey;
    private float elapsedSeconds;
    private float transitionProgress = 1f;
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
        CacheSceneLighting();
        CacheScenePostEffects();

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

        CopyState(CreateDefaultState(), runtimeState);
        CopyState(runtimeState, sourceState);
        CopyState(runtimeState, targetState);
        ApplyWeatherMaterial(materialWeather);
        ApplySceneFog();
    }

    public override void Update()
    {
        if (worldRuntime is null || cameraEntity is null || weatherRoot is null || visualFactory is null)
        {
            return;
        }

        float deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
        elapsedSeconds += deltaTime;
        weatherRoot.Transform.Position = cameraEntity.Transform.Position + new Vector3(0f, 2.4f, 0f);

        int worldX = (int)MathF.Floor(cameraEntity.Transform.Position.X);
        int worldZ = (int)MathF.Floor(cameraEntity.Transform.Position.Z);
        WorldSample sample = worldRuntime.SampleSurfaceWorld(worldX, worldZ);
        float worldTime = worldLawController?.RuntimeState.WorldTimeSeconds ?? elapsedSeconds;
        WeatherRuntimeState nextState = weatherStateResolver.Resolve(
            sample,
            worldX,
            worldZ,
            worldRuntime.Settings.Seed,
            worldTime,
            worldLawController?.RuntimeState);

        UpdateTargetState(nextState);
        AdvanceRuntimeState(deltaTime);
        UpdateMaterialWeather();

        worldRuntime.Stats.WeatherSummary =
            $"{runtimeState.CurrentWeather}->{runtimeState.TargetWeather} blend={runtimeState.Blend:0.00} intensity={runtimeState.Intensity:0.00} fog={runtimeState.FogDensity:0.00} wet={runtimeState.GroundWetness:0.00}";
        ApplySceneLighting(worldTime);
        ApplySceneFog();
        UpdateParticles();
    }

    public override void Cancel()
    {
        if (weatherRoot is not null)
        {
            weatherRoot.Scene = null;
        }

        DisableSceneFog();
        base.Cancel();
    }

    private void UpdateTargetState(WeatherRuntimeState nextState)
    {
        if (targetState.TargetWeather != nextState.TargetWeather)
        {
            CopyState(runtimeState, sourceState);
            CopyState(nextState, targetState);
            transitionProgress = 0f;
            return;
        }

        CopyState(nextState, targetState);
    }

    private void AdvanceRuntimeState(float deltaTime)
    {
        if (transitionProgress < 1f)
        {
            float duration = ResolveTransitionDuration(sourceState.TargetWeather, targetState.TargetWeather, targetState.Intensity);
            transitionProgress = MathUtil.Clamp(transitionProgress + deltaTime / duration, 0f, 1f);
            float blend = EaseInOut(transitionProgress);
            LerpState(sourceState, targetState, blend, runtimeState);
            runtimeState.CurrentWeather = sourceState.TargetWeather;
            runtimeState.TargetWeather = targetState.TargetWeather;
            runtimeState.Blend = blend;
            if (transitionProgress >= 1f)
            {
                CopyState(targetState, runtimeState);
                runtimeState.CurrentWeather = targetState.TargetWeather;
                runtimeState.TargetWeather = targetState.TargetWeather;
                runtimeState.Blend = 1f;
            }

            return;
        }

        DampStateToward(runtimeState, targetState, MathUtil.Clamp(deltaTime * 1.6f, 0f, 1f));
        runtimeState.CurrentWeather = targetState.TargetWeather;
        runtimeState.TargetWeather = targetState.TargetWeather;
        runtimeState.Blend = 1f;
    }

    private void UpdateMaterialWeather()
    {
        WeatherKind desiredMaterial = runtimeState.Blend < 0.55f
            ? runtimeState.CurrentWeather
            : runtimeState.TargetWeather;
        if (desiredMaterial == materialWeather)
        {
            return;
        }

        materialWeather = desiredMaterial;
        ApplyWeatherMaterial(materialWeather);
    }

    private void ApplyWeatherMaterial(WeatherKind weatherKind)
    {
        if (visualFactory is null)
        {
            return;
        }

        WeatherEffectDescriptor descriptor = effectLibrary.Get(weatherKind);
        bool prefabReady = TryActivateWeatherEffectPrefab(descriptor);
        SetFallbackParticleVisibility(!prefabReady);
        EnvironmentMaterialKind materialKind = descriptor.FallbackMaterial;

        foreach (WeatherParticleSlot particle in particles)
        {
            visualFactory.ApplyBoxModel(particle.Entity, materialKind);
        }

        if (prefabReady)
        {
            TerrainRuntimeLogger.Logger.Debug($"Weather prefab asset ready for {descriptor.Weather}: {descriptor.ParticleAssetKey}");
        }
    }

    private bool TryActivateWeatherEffectPrefab(WeatherEffectDescriptor descriptor)
    {
        if (descriptor.UsesPlaceholderGeometry || content is null || string.IsNullOrWhiteSpace(descriptor.ParticleAssetKey))
        {
            ClearActiveWeatherEffect();
            return false;
        }

        if (!effectPrefabCache.TryGetValue(descriptor.ParticleAssetKey, out Prefab? prefab))
        {
            try
            {
                prefab = content.Load<Prefab>(descriptor.ParticleAssetKey);
            }
            catch
            {
                prefab = null;
            }

            effectPrefabCache[descriptor.ParticleAssetKey] = prefab;
        }

        if (prefab is null)
        {
            ClearActiveWeatherEffect();
            return false;
        }

        if (activeWeatherEffectKey == descriptor.ParticleAssetKey && activeWeatherEffectEntity is not null)
        {
            return true;
        }

        ClearActiveWeatherEffect();
        activeWeatherEffectEntity = new Entity($"WeatherEffect_{descriptor.Weather}");
        activeWeatherEffectKey = descriptor.ParticleAssetKey;
        foreach (Entity child in prefab.Instantiate())
        {
            activeWeatherEffectEntity.AddChild(child);
        }

        weatherRoot?.AddChild(activeWeatherEffectEntity);
        return true;
    }

    private void SetFallbackParticleVisibility(bool visible)
    {
        foreach (WeatherParticleSlot particle in particles)
        {
            if (visible)
            {
                if (particle.Entity.Transform.Scale == Vector3.Zero)
                {
                    particle.Entity.Transform.Scale = new Vector3(0.05f);
                }

                continue;
            }

            particle.Entity.Transform.Scale = Vector3.Zero;
        }
    }

    private void ClearActiveWeatherEffect()
    {
        if (activeWeatherEffectEntity is not null)
        {
            activeWeatherEffectEntity.Scene = null;
            activeWeatherEffectEntity.Transform.Parent = null;
            activeWeatherEffectEntity = null;
        }

        activeWeatherEffectKey = null;
    }

    private void CacheSceneLighting()
    {
        Scene? scene = Entity.Scene;
        if (scene is null)
        {
            return;
        }

        foreach (Entity entity in scene.Entities)
        {
            if (entity.Name != "Directional light")
            {
                continue;
            }

            directionalLightComponent = entity.Get<LightComponent>();
            if (directionalLightComponent is not null)
            {
                baseLightColor = directionalLightComponent.GetColor();
                baseLightIntensity = directionalLightComponent.Intensity;
            }
            break;
        }
    }

    private void CacheScenePostEffects()
    {
        forwardRenderers.Clear();
        if (SceneSystem?.GraphicsCompositor is null)
        {
            return;
        }

        if (SceneSystem.GraphicsCompositor.Game is SceneCameraRenderer sceneCameraRenderer)
        {
            CollectForwardRenderers(sceneCameraRenderer.Child);
        }
        else
        {
            CollectForwardRenderers(SceneSystem.GraphicsCompositor.Game);
        }

        if (SceneSystem.GraphicsCompositor.SingleView is ForwardRenderer singleViewRenderer && !forwardRenderers.Contains(singleViewRenderer))
        {
            forwardRenderers.Add(singleViewRenderer);
        }
    }

    private void CollectForwardRenderers(object? renderer)
    {
        switch (renderer)
        {
            case ForwardRenderer forwardRenderer when !forwardRenderers.Contains(forwardRenderer):
                forwardRenderers.Add(forwardRenderer);
                break;

            case SceneCameraRenderer sceneCameraRenderer:
                CollectForwardRenderers(sceneCameraRenderer.Child);
                break;

            case SceneRendererCollection rendererCollection:
                foreach (object child in rendererCollection.Children)
                {
                    CollectForwardRenderers(child);
                }
                break;
        }
    }

    private void ApplySceneLighting(float worldTime)
    {
        if (directionalLightComponent is null)
        {
            return;
        }

        TerrainSceneWeatherLighting.Apply(directionalLightComponent, baseLightColor, baseLightIntensity, runtimeState, worldTime);
    }

    private void ApplySceneFog()
    {
        if (forwardRenderers.Count == 0)
        {
            return;
        }

        bool shouldEnableFog =
            runtimeState.TargetWeather == WeatherKind.Fog ||
            runtimeState.TargetWeather == WeatherKind.Rain ||
            runtimeState.SeaFog > 0.28f ||
            runtimeState.WoodlandMist > 0.32f ||
            runtimeState.FogDensity > 0.10f;
        float fogStart = MathUtil.Clamp(
            9f + (1f - runtimeState.FogDensity) * 18f - runtimeState.SeaFog * 4f + runtimeState.WindStrength * 4f,
            4f,
            34f);

        foreach (ForwardRenderer renderer in forwardRenderers)
        {
            PostProcessingEffects postEffects = renderer.PostEffects as PostProcessingEffects ?? new PostProcessingEffects();
            renderer.PostEffects = postEffects;
            postEffects.Fog.Enabled = shouldEnableFog;
            postEffects.Fog.Density = MathUtil.Clamp(runtimeState.FogDensity, 0.02f, 0.65f);
            postEffects.Fog.Color = runtimeState.FogColor;
            postEffects.Fog.FogStart = fogStart;
            postEffects.Fog.SkipBackground = false;
        }
    }

    private void DisableSceneFog()
    {
        foreach (ForwardRenderer renderer in forwardRenderers)
        {
            if (renderer.PostEffects is not PostProcessingEffects postEffects)
            {
                continue;
            }

            postEffects.Fog.Enabled = false;
        }
    }

    private void UpdateParticles()
    {
        WeatherRuntimeState from = transitionProgress < 1f ? sourceState : targetState;
        WeatherRuntimeState to = targetState;
        WeatherKind fromWeather = transitionProgress < 1f ? sourceState.TargetWeather : targetState.TargetWeather;
        WeatherKind toWeather = targetState.TargetWeather;
        float blend = transitionProgress < 1f ? EaseInOut(transitionProgress) : 1f;
        int fromCount = ComputeActiveCount(fromWeather, from);
        int toCount = ComputeActiveCount(toWeather, to);
        int activeCount = transitionProgress < 1f
            ? (int)MathF.Round(Lerp(fromCount, toCount, blend))
            : toCount;

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

            bool hasFromPose = i < fromCount;
            bool hasToPose = i < toCount;
            ParticlePose fromPose = hasFromPose
                ? ComputeParticlePose(fromWeather, from, slot, motion, fall, angle, radius)
                : ParticlePose.Zero;
            ParticlePose toPose = hasToPose
                ? ComputeParticlePose(toWeather, to, slot, motion, fall, angle, radius)
                : ParticlePose.Zero;

            if (!hasFromPose && hasToPose)
            {
                fromPose = toPose.WithScale(Vector3.Zero);
            }
            else if (hasFromPose && !hasToPose)
            {
                toPose = fromPose.WithScale(Vector3.Zero);
            }

            ParticlePose pose = transitionProgress < 1f
                ? LerpPose(fromPose, toPose, blend)
                : toPose;

            particle.Transform.Position = pose.Position;
            particle.Transform.Scale = pose.Scale;
            particle.Transform.RotationEulerXYZ = pose.RotationEuler;
        }
    }

    private static int ComputeActiveCount(WeatherKind weatherKind, WeatherRuntimeState state)
    {
        int activeCount = weatherKind switch
        {
            WeatherKind.Clear => 4 + (int)MathF.Round(state.Intensity * 6f),
            WeatherKind.Wind => 8 + (int)MathF.Round(state.WindStrength * 9f + state.AnomalyFactor * 3f),
            WeatherKind.Fog => 10 + (int)MathF.Round(state.Intensity * 9f + state.SeaFog * 5f + state.WoodlandMist * 4f),
            WeatherKind.Snow => 12 + (int)MathF.Round(state.Intensity * 12f + state.SnowCoverage * 3f),
            WeatherKind.Rain => 16 + (int)MathF.Round(state.Intensity * 10f + state.GroundWetness * 4f),
            _ => 0,
        };

        return Math.Clamp(activeCount, 0, ParticleCount);
    }

    private static ParticlePose ComputeParticlePose(
        WeatherKind weatherKind,
        WeatherRuntimeState state,
        WeatherParticleSlot slot,
        float motion,
        float fall,
        float angle,
        float radius)
    {
        return weatherKind switch
        {
            WeatherKind.Rain => ComputeRainPose(state, slot, fall, angle, radius),
            WeatherKind.Snow => ComputeSnowPose(state, slot, motion, fall, angle, radius),
            WeatherKind.Fog => ComputeFogPose(state, slot, motion, angle, radius),
            WeatherKind.Wind => ComputeWindPose(state, slot, fall, angle),
            _ => ComputeClearPose(state, slot, motion, angle, radius),
        };
    }

    private static ParticlePose ComputeRainPose(WeatherRuntimeState state, WeatherParticleSlot slot, float fall, float angle, float radius)
    {
        float horizontalRadius = radius * (0.52f + state.GroundWetness * 0.18f);
        float windOffset = 1.6f + state.WindStrength * 1.6f;
        Vector3 position = new(
            state.WindDirection.X * windOffset + MathF.Cos(angle) * horizontalRadius,
            7.2f - fall * (12.8f + state.Intensity * 2.4f),
            state.WindDirection.Y * windOffset + MathF.Sin(angle) * horizontalRadius);
        Vector3 scale = new(0.024f, 0.76f + state.Intensity * 0.54f + slot.VerticalBias * 0.16f, 0.024f);
        Vector3 rotation = new(0.12f + state.WindStrength * 0.18f, angle, 0.04f + state.WindStrength * 0.10f);

        if (fall > 0.92f)
        {
            position.Y = 0.16f + slot.VerticalBias * 0.05f;
            scale = new Vector3(0.14f + state.GroundWetness * 0.06f, 0.02f, 0.14f + state.GroundWetness * 0.06f);
            rotation = Vector3.Zero;
        }

        return new ParticlePose(position, scale, rotation);
    }

    private static ParticlePose ComputeSnowPose(WeatherRuntimeState state, WeatherParticleSlot slot, float motion, float fall, float angle, float radius)
    {
        Vector3 position = new(
            state.WindDirection.X * state.WindStrength * 0.9f + MathF.Cos(angle + fall * 1.4f) * (radius * 0.80f),
            5.8f - fall * 9.6f,
            state.WindDirection.Y * state.WindStrength * 0.9f + MathF.Sin(angle * 0.9f) * (radius * 0.84f));
        Vector3 scale = new(0.07f + state.SnowCoverage * 0.04f + slot.VerticalBias * 0.008f);
        Vector3 rotation = new(motion, angle * 0.4f, motion * 0.65f);
        return new ParticlePose(position, scale, rotation);
    }

    private static ParticlePose ComputeFogPose(WeatherRuntimeState state, WeatherParticleSlot slot, float motion, float angle, float radius)
    {
        float horizontal = radius * (0.46f + state.SeaFog * 0.22f + state.WoodlandMist * 0.18f);
        Vector3 position = new(
            MathF.Cos(angle * 0.24f) * horizontal,
            state.FogHeight + MathF.Sin(motion * 0.55f) * (0.12f + state.WoodlandMist * 0.10f) + slot.VerticalBias * (state.SeaFog > state.WoodlandMist ? 0.08f : 0.16f),
            MathF.Sin(angle * 0.24f) * horizontal);
        Vector3 scale = new(
            0.72f + state.SeaFog * 0.26f + slot.VerticalBias * 0.10f,
            0.14f + state.WoodlandMist * 0.12f + slot.VerticalBias * 0.03f,
            0.46f + state.SeaFog * 0.24f + slot.VerticalBias * 0.08f);
        Vector3 rotation = new(0f, angle * 0.18f, 0f);
        return new ParticlePose(position, scale, rotation);
    }

    private static ParticlePose ComputeWindPose(WeatherRuntimeState state, WeatherParticleSlot slot, float fall, float angle)
    {
        Vector3 position = new(
            state.WindDirection.X * -7.5f + fall * (13.5f + state.AnomalyFactor * 3.5f),
            1.1f + slot.VerticalBias * 0.42f,
            state.WindDirection.Y * 4.8f + MathF.Sin(angle) * (4.4f + state.WindStrength * 1.2f));
        Vector3 scale = new(0.06f + slot.VerticalBias * 0.01f, 0.10f + state.WindStrength * 0.08f, 0.06f + slot.VerticalBias * 0.01f);
        Vector3 rotation = new(0.08f, 0.24f, 0.08f + state.WindStrength * 0.08f);
        return new ParticlePose(position, scale, rotation);
    }

    private static ParticlePose ComputeClearPose(WeatherRuntimeState state, WeatherParticleSlot slot, float motion, float angle, float radius)
    {
        Vector3 position = new(
            MathF.Cos(angle) * MathF.Min(radius, 3.4f),
            1.3f + MathF.Sin(motion) * 0.28f + state.GroundWetness * 0.08f,
            MathF.Sin(angle) * MathF.Min(radius, 3.4f));
        Vector3 scale = new(0.04f + slot.VerticalBias * 0.006f);
        Vector3 rotation = new(0f, angle * 0.30f, 0f);
        return new ParticlePose(position, scale, rotation);
    }

    private static void CopyState(WeatherRuntimeState source, WeatherRuntimeState destination)
    {
        destination.CurrentWeather = source.CurrentWeather;
        destination.TargetWeather = source.TargetWeather;
        destination.Blend = source.Blend;
        destination.Intensity = source.Intensity;
        destination.FogHeight = source.FogHeight;
        destination.FogDensity = source.FogDensity;
        destination.FogColor = source.FogColor;
        destination.SnowCoverage = source.SnowCoverage;
        destination.GroundWetness = source.GroundWetness;
        destination.SeaFog = source.SeaFog;
        destination.WoodlandMist = source.WoodlandMist;
        destination.AnomalyFactor = source.AnomalyFactor;
        destination.WindDirection = source.WindDirection;
        destination.WindStrength = source.WindStrength;
    }

    private static void LerpState(WeatherRuntimeState from, WeatherRuntimeState to, float amount, WeatherRuntimeState destination)
    {
        destination.CurrentWeather = amount < 1f ? from.TargetWeather : to.TargetWeather;
        destination.TargetWeather = to.TargetWeather;
        destination.Blend = amount;
        destination.Intensity = Lerp(from.Intensity, to.Intensity, amount);
        destination.FogHeight = Lerp(from.FogHeight, to.FogHeight, amount);
        destination.FogDensity = Lerp(from.FogDensity, to.FogDensity, amount);
        destination.FogColor = LerpColor(from.FogColor, to.FogColor, amount);
        destination.SnowCoverage = Lerp(from.SnowCoverage, to.SnowCoverage, amount);
        destination.GroundWetness = Lerp(from.GroundWetness, to.GroundWetness, amount);
        destination.SeaFog = Lerp(from.SeaFog, to.SeaFog, amount);
        destination.WoodlandMist = Lerp(from.WoodlandMist, to.WoodlandMist, amount);
        destination.AnomalyFactor = Lerp(from.AnomalyFactor, to.AnomalyFactor, amount);
        destination.WindDirection = Vector2.Lerp(from.WindDirection, to.WindDirection, amount);
        destination.WindStrength = Lerp(from.WindStrength, to.WindStrength, amount);
    }

    private static void DampStateToward(WeatherRuntimeState current, WeatherRuntimeState target, float amount)
    {
        current.Intensity = Lerp(current.Intensity, target.Intensity, amount);
        current.FogHeight = Lerp(current.FogHeight, target.FogHeight, amount);
        current.FogDensity = Lerp(current.FogDensity, target.FogDensity, amount);
        current.FogColor = LerpColor(current.FogColor, target.FogColor, amount);
        current.SnowCoverage = Lerp(current.SnowCoverage, target.SnowCoverage, amount);
        current.GroundWetness = Lerp(current.GroundWetness, target.GroundWetness, amount);
        current.SeaFog = Lerp(current.SeaFog, target.SeaFog, amount);
        current.WoodlandMist = Lerp(current.WoodlandMist, target.WoodlandMist, amount);
        current.AnomalyFactor = Lerp(current.AnomalyFactor, target.AnomalyFactor, amount);
        current.WindDirection = Vector2.Lerp(current.WindDirection, target.WindDirection, amount);
        current.WindStrength = Lerp(current.WindStrength, target.WindStrength, amount);
    }

    private static WeatherRuntimeState CreateDefaultState()
    {
        return new WeatherRuntimeState
        {
            CurrentWeather = WeatherKind.Clear,
            TargetWeather = WeatherKind.Clear,
            Blend = 1f,
            Intensity = 0.10f,
            FogHeight = 0.84f,
            FogDensity = 0.05f,
            FogColor = new Color3(0.84f, 0.88f, 0.92f),
            SnowCoverage = 0f,
            GroundWetness = 0f,
            SeaFog = 0f,
            WoodlandMist = 0f,
            AnomalyFactor = 0f,
            WindDirection = new Vector2(1f, 0f),
            WindStrength = 0.10f,
        };
    }

    private static ParticlePose LerpPose(ParticlePose from, ParticlePose to, float amount)
    {
        return new ParticlePose(
            Vector3.Lerp(from.Position, to.Position, amount),
            Vector3.Lerp(from.Scale, to.Scale, amount),
            Vector3.Lerp(from.RotationEuler, to.RotationEuler, amount));
    }

    private static float ResolveTransitionDuration(WeatherKind from, WeatherKind to, float intensity)
    {
        float baseDuration = from == to ? 1.4f : 2.6f;
        if ((from, to) is (WeatherKind.Clear, WeatherKind.Rain) or (WeatherKind.Clear, WeatherKind.Snow))
        {
            baseDuration = 2.9f;
        }

        if ((from, to) is (WeatherKind.Fog, WeatherKind.Clear) or (WeatherKind.Clear, WeatherKind.Fog))
        {
            baseDuration = 3.2f;
        }

        return MathF.Max(1.0f, baseDuration - intensity * 0.5f);
    }

    private static float EaseInOut(float value)
    {
        value = MathUtil.Clamp(value, 0f, 1f);
        return value * value * (3f - 2f * value);
    }

    private static float Lerp(float from, float to, float amount)
    {
        amount = MathUtil.Clamp(amount, 0f, 1f);
        return from + (to - from) * amount;
    }

    private static Color3 LerpColor(Color3 from, Color3 to, float amount)
    {
        amount = MathUtil.Clamp(amount, 0f, 1f);
        return new Color3(
            from.R + (to.R - from.R) * amount,
            from.G + (to.G - from.G) * amount,
            from.B + (to.B - from.B) * amount);
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

    private readonly record struct ParticlePose(Vector3 Position, Vector3 Scale, Vector3 RotationEuler)
    {
        public static ParticlePose Zero => new(Vector3.Zero, Vector3.Zero, Vector3.Zero);

        public ParticlePose WithScale(Vector3 scale)
        {
            return this with { Scale = scale };
        }
    }
}
