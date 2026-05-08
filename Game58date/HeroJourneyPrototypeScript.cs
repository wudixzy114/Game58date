#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Profiling;
using Stride.Rendering;
using Stride.Rendering.Lights;

namespace Game58date;

public sealed class HeroJourneyPrototypeScript : SyncScript, IInputEventListener, IInputEventListener<TextInputEvent>
{
    private static readonly Color4 HudTitleColor = new(1.00f, 0.95f, 0.60f, 1.00f);
    private static readonly Color4 HudTextColor = new(0.95f, 0.95f, 0.95f, 1.00f);
    private static readonly Color4 HudHintColor = new(0.75f, 0.75f, 0.75f, 1.00f);
    private static readonly Color4 HudInputColor = new(0.50f, 0.95f, 1.00f, 1.00f);
    private static readonly Color4 HudDisabledColor = new(0.45f, 0.45f, 0.45f, 1.00f);
    private static readonly Color4 HudLogColor = new(0.85f, 0.85f, 0.85f, 1.00f);

    private readonly StringBuilder intentBuffer = new();
    private readonly Queue<string> eventLog = new();
    private readonly List<OmenMarker> omenMarkers = new();
    private readonly Random random = new(5800);

    private WorldLawState worldState = new();
    private HeroJourneyStage heroStage = HeroJourneyStage.OrdinaryWorld;
    private Entity? groundEntity;
    private Entity? centralSphereEntity;
    private Model? sphereModel;
    private LightComponent? sunLight;
    private float baseSunIntensity;
    private Color3 baseSunColor = new(1.00f, 1.00f, 1.00f);
    private float worldTime;
    private float pulseTime;
    private float omenCooldown;
    private float omenVisualTime;
    private bool textInputEnabled;
    private bool hudVisible = true;

    public override void Start()
    {
        groundEntity = FindEntity("Ground");
        centralSphereEntity = FindEntity("Sphere");
        sphereModel = centralSphereEntity?.Get<ModelComponent>()?.Model;
        sunLight = FindEntity("Directional light")?.Get<LightComponent>();

        if (sunLight is not null)
        {
            baseSunIntensity = sunLight.Intensity;
            baseSunColor = sunLight.GetColor();
        }

        CreateOmenMarkers();
        EnableTextInput();

        PushLog("Prototype ready.");
        PushLog("Tab toggles text input. Enter submits the current intent.");
        PushLog("F2 sea crossing, F3 loss, F4 violent, F5 peaceful, F6 mentor.");
    }

    public override void Update()
    {
        float deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
        worldTime += deltaTime;
        pulseTime += deltaTime;
        omenCooldown = MathF.Max(0f, omenCooldown - deltaTime);
        omenVisualTime = MathF.Max(0f, omenVisualTime - deltaTime);

        HandleHotkeys();
        SimulateWorld(deltaTime);
        UpdateLandmarkVisuals();
        UpdateOmenMarkers();
        DrawHud();
    }

    public override void Cancel()
    {
        DisableTextInput();
        base.Cancel();
    }

    public void ProcessEvent(TextInputEvent inputEvent)
    {
        if (!textInputEnabled || inputEvent.Type != TextInputEventType.Input)
        {
            return;
        }

        foreach (char character in inputEvent.Text)
        {
            if (character is '\r' or '\n')
            {
                continue;
            }

            intentBuffer.Append(character);
        }
    }

    private void HandleHotkeys()
    {
        if (Input.IsKeyPressed(Keys.F1))
        {
            hudVisible = !hudVisible;
            PushLog(hudVisible ? "HUD enabled." : "HUD hidden.");
        }

        if (Input.IsKeyPressed(Keys.Tab))
        {
            if (textInputEnabled)
            {
                DisableTextInput();
                PushLog("Text input disabled.");
            }
            else
            {
                EnableTextInput();
                PushLog("Text input enabled.");
            }
        }

        if (textInputEnabled && (Input.IsKeyPressed(Keys.Back) || Input.IsKeyPressed(Keys.BackSpace)) && intentBuffer.Length > 0)
        {
            intentBuffer.Length -= 1;
        }

        if (textInputEnabled && (Input.IsKeyPressed(Keys.Enter) || Input.IsKeyPressed(Keys.Return)))
        {
            SubmitIntent();
        }

        if (Input.IsKeyPressed(Keys.F2))
        {
            SubmitIntent("I want to cross the sea and reach a new trial.");
        }

        if (Input.IsKeyPressed(Keys.F3))
        {
            RegisterLoss("A serious setback was recorded.");
        }

        if (Input.IsKeyPressed(Keys.F4))
        {
            RegisterViolentChoice();
        }

        if (Input.IsKeyPressed(Keys.F5))
        {
            RegisterPeacefulChoice();
        }

        if (Input.IsKeyPressed(Keys.F6))
        {
            SubmitIntent("I want to find a mentor and a sign.");
        }
    }

    private void SubmitIntent()
    {
        SubmitIntent(intentBuffer.ToString());
        intentBuffer.Clear();
    }

    private void SubmitIntent(string rawIntent)
    {
        string intent = rawIntent.Trim();
        if (string.IsNullOrWhiteSpace(intent))
        {
            PushLog("Empty intent ignored.");
            return;
        }

        worldState.IntentCount++;
        worldState.ExplorationDrive = Clamp01(worldState.ExplorationDrive + 0.20f);
        PushLog($"Intent: {intent}");

        string normalized = intent.ToLowerInvariant();

        if (ContainsAny(normalized, "sea", "cross", "shore", "ocean", "ship", "harbor"))
        {
            worldState.BorderLonging = Clamp01(worldState.BorderLonging + 0.45f);
            worldState.Curiosity = Clamp01(worldState.Curiosity + 0.24f);
            worldState.TargetBiome = "Desert Crossing";
            TriggerOmen(OmenType.PathRevelation, "Mist opened a route toward a hidden dock.");
        }
        else if (ContainsAny(normalized, "mentor", "guide", "teacher", "sage", "sign"))
        {
            worldState.Curiosity = Clamp01(worldState.Curiosity + 0.16f);
            worldState.Faith = Clamp01(worldState.Faith + 0.28f);
            TriggerOmen(OmenType.GuideArrival, "A quiet traveler appeared near the road and pointed north.");
        }
        else if (ContainsAny(normalized, "treasure", "relic", "knowledge", "ruin", "secret"))
        {
            worldState.Curiosity = Clamp01(worldState.Curiosity + 0.30f);
            worldState.ResourcePressure = Clamp01(worldState.ResourcePressure + 0.12f);
            TriggerOmen(OmenType.Divination, "Fire sparks drew the outline of a buried geometry.");
        }
        else if (ContainsAny(normalized, "peace", "help", "save", "heal", "kind"))
        {
            RegisterPeacefulChoice();
            TriggerOmen(OmenType.SocialShift, "The market lights returned and strangers resumed trading news.");
        }
        else if (ContainsAny(normalized, "kill", "take", "destroy", "rule", "loot"))
        {
            RegisterViolentChoice();
            TriggerOmen(OmenType.NaturalAnomaly, "Wind direction reversed and a cold pressure spread across the ground.");
        }
        else
        {
            worldState.Curiosity = Clamp01(worldState.Curiosity + 0.12f);
            TriggerOmen(OmenType.Divination, "The world answered with a brief and uncertain glow.");
        }

        TryAdvanceHeroStage();
    }

    private void RegisterLoss(string reason)
    {
        worldState.LossMemory = Clamp01(worldState.LossMemory + 0.50f);
        worldState.BlessingWeight = Clamp01(worldState.BlessingWeight + 0.42f);
        PushLog(reason);
        TriggerOmen(OmenType.Divination, "A loss shifted fortune back toward the player.");
    }

    private void RegisterViolentChoice()
    {
        worldState.Karma = MathF.Max(-1f, worldState.Karma - 0.18f);
        worldState.Violence = Clamp01(worldState.Violence + 0.24f);
        worldState.ResourcePressure = Clamp01(worldState.ResourcePressure + 0.10f);
        PushLog("Behavior profile moved toward violence.");
    }

    private void RegisterPeacefulChoice()
    {
        worldState.Karma = MathF.Min(1f, worldState.Karma + 0.16f);
        worldState.Faith = Clamp01(worldState.Faith + 0.12f);
        worldState.Violence = Clamp01(worldState.Violence - 0.08f);
        PushLog("Behavior profile moved toward compassion.");
    }

    private void SimulateWorld(float deltaTime)
    {
        worldState.ExplorationDrive = Clamp01(worldState.ExplorationDrive + deltaTime * 0.01f);
        worldState.BorderLonging = Clamp01(worldState.BorderLonging - deltaTime * 0.01f);
        worldState.LossMemory = Clamp01(worldState.LossMemory - deltaTime * 0.015f);
        worldState.BlessingWeight = Clamp01(worldState.BlessingWeight - deltaTime * 0.020f);
        worldState.ResourcePressure = Clamp01(worldState.ResourcePressure + MathF.Max(0f, worldState.Violence - 0.40f) * deltaTime * 0.02f);

        if (omenCooldown <= 0f)
        {
            TrySpawnEmergentOmen();
        }

        UpdateSunLight();
    }

    private void TrySpawnEmergentOmen()
    {
        if (worldState.ResourcePressure > 0.72f)
        {
            TriggerOmen(OmenType.NaturalAnomaly, "Resource pressure triggered a backlash.");
            worldState.ResourcePressure = Clamp01(worldState.ResourcePressure - 0.25f);
            return;
        }

        if (worldState.BlessingWeight > 0.65f)
        {
            TriggerOmen(OmenType.PathRevelation, "A hidden route appeared after a loss.");
            worldState.BlessingWeight = Clamp01(worldState.BlessingWeight - 0.30f);
            return;
        }

        if (worldState.Karma < -0.55f)
        {
            TriggerOmen(OmenType.NaturalAnomaly, "Bad karma turned even clear weather hostile.");
            worldState.Karma = MathF.Min(1f, worldState.Karma + 0.12f);
            return;
        }

        if (worldState.Karma > 0.55f && random.NextSingle() > 0.45f)
        {
            TriggerOmen(OmenType.GuideArrival, "A patient light waited longer than before.");
            return;
        }

        if (worldState.ExplorationDrive > 0.68f && random.NextSingle() > 0.55f)
        {
            TriggerOmen(OmenType.SocialShift, "A distant harbor bell suggested long-range travel was possible.");
        }
    }

    private void TriggerOmen(OmenType omenType, string description)
    {
        omenCooldown = 8f + random.NextSingle() * 4f;
        omenVisualTime = 6f;
        worldState.LastOmen = omenType;
        worldState.LastOmenDescription = description;
        worldState.OmenCount++;

        switch (omenType)
        {
            case OmenType.PathRevelation:
                worldState.PathVisibility = Clamp01(worldState.PathVisibility + 0.55f);
                worldState.BorderLonging = Clamp01(worldState.BorderLonging - 0.22f);
                break;
            case OmenType.GuideArrival:
                worldState.Faith = Clamp01(worldState.Faith + 0.18f);
                break;
            case OmenType.NaturalAnomaly:
                worldState.Atmosphere = Clamp01(worldState.Atmosphere + 0.22f);
                break;
            case OmenType.SocialShift:
                worldState.SocialFlux = Clamp01(worldState.SocialFlux + 0.24f);
                break;
            case OmenType.Divination:
                worldState.Curiosity = Clamp01(worldState.Curiosity + 0.18f);
                break;
        }

        PushLog($"Omen: {description}");
        TryAdvanceHeroStage();
    }

    private void TryAdvanceHeroStage()
    {
        HeroJourneyStage nextStage = heroStage;

        if (worldState.IntentCount >= 1 && worldState.ExplorationDrive > 0.12f)
        {
            nextStage = HeroJourneyStage.CallToAdventure;
        }

        if (worldState.OmenCount >= 1 && worldState.Curiosity > 0.26f)
        {
            nextStage = HeroJourneyStage.CrossingTheThreshold;
        }

        if (worldState.BorderLonging > 0.40f || worldState.PathVisibility > 0.45f)
        {
            nextStage = HeroJourneyStage.RoadOfTrials;
        }

        if (worldState.Faith > 0.45f && worldState.Karma > 0.10f)
        {
            nextStage = HeroJourneyStage.MeetingTheMentor;
        }

        if (worldState.PathVisibility > 0.70f && worldState.Curiosity > 0.55f)
        {
            nextStage = HeroJourneyStage.ApproachToTheInmostCave;
        }

        if (worldState.BlessingWeight > 0.55f && worldState.OmenCount >= 3)
        {
            nextStage = HeroJourneyStage.Transformation;
        }

        if (nextStage > heroStage)
        {
            heroStage = nextStage;
            PushLog($"Hero stage advanced to {GetStageTitle(heroStage)}.");
        }
    }

    private void UpdateSunLight()
    {
        if (sunLight is null)
        {
            return;
        }

        float baseWave = 0.88f + MathF.Sin(pulseTime * 0.8f) * 0.12f;
        float omenBoost = worldState.LastOmen switch
        {
            OmenType.PathRevelation => 4.5f,
            OmenType.GuideArrival => 2.2f,
            OmenType.NaturalAnomaly => -4.0f,
            OmenType.SocialShift => 1.2f,
            OmenType.Divination => 3.0f,
            _ => 0f,
        };

        sunLight.Intensity = MathF.Max(5f, baseSunIntensity * baseWave + omenBoost + worldState.Karma * 1.4f);

        Color3 stageColor = heroStage switch
        {
            HeroJourneyStage.OrdinaryWorld => new Color3(1.00f, 1.00f, 1.00f),
            HeroJourneyStage.CallToAdventure => new Color3(1.00f, 0.92f, 0.80f),
            HeroJourneyStage.CrossingTheThreshold => new Color3(0.88f, 0.95f, 1.00f),
            HeroJourneyStage.RoadOfTrials => new Color3(1.00f, 0.80f, 0.55f),
            HeroJourneyStage.MeetingTheMentor => new Color3(0.82f, 1.00f, 0.84f),
            HeroJourneyStage.ApproachToTheInmostCave => new Color3(0.75f, 0.82f, 1.00f),
            HeroJourneyStage.Transformation => new Color3(1.00f, 0.96f, 0.66f),
            _ => baseSunColor,
        };

        float blend = Clamp01(worldState.Atmosphere * 0.55f + worldState.PathVisibility * 0.45f);
        sunLight.SetColor(LerpColor(baseSunColor, stageColor, blend));
    }

    private void UpdateLandmarkVisuals()
    {
        if (centralSphereEntity is not null)
        {
            float scale = 1.00f + worldState.PathVisibility * 0.70f + MathF.Sin(worldTime * 1.8f) * 0.05f;
            centralSphereEntity.Transform.Scale = new Vector3(scale);
            centralSphereEntity.Transform.Position = new Vector3(0f, 0.5f + worldState.BlessingWeight * 0.65f, 0f);
        }

        if (groundEntity is not null)
        {
            float spread = 1.00f + worldState.ResourcePressure * 0.16f;
            groundEntity.Transform.Scale = new Vector3(spread, 1.00f, spread);
        }
    }

    private void UpdateOmenMarkers()
    {
        if (omenMarkers.Count == 0)
        {
            return;
        }

        int highlightedIndex = GetHighlightedMarkerIndex();
        float pulse = omenVisualTime > 0f ? 0.55f + MathF.Sin(worldTime * 5.2f) * 0.45f : 0.08f;

        for (int index = 0; index < omenMarkers.Count; index++)
        {
            OmenMarker marker = omenMarkers[index];
            bool highlighted = index == highlightedIndex;

            float scale = highlighted
                ? 0.45f + pulse * 0.40f
                : 0.18f + MathF.Sin(worldTime + index) * 0.015f;

            marker.Entity.Transform.Scale = new Vector3(scale);
            marker.LightComponent.Intensity = highlighted ? 8f + pulse * 22f : 0.15f;
            marker.Light.Radius = highlighted ? 10f : 2f;
            marker.LightComponent.SetColor(GetMarkerColor(index, highlighted));
        }
    }

    private int GetHighlightedMarkerIndex()
    {
        return worldState.LastOmen switch
        {
            OmenType.NaturalAnomaly => 0,
            OmenType.SocialShift => 1,
            OmenType.GuideArrival => 2,
            OmenType.Divination => 3,
            OmenType.PathRevelation => 4,
            _ => -1,
        };
    }

    private void CreateOmenMarkers()
    {
        Scene? scene = Entity.Scene;
        if (scene is null)
        {
            return;
        }

        var definitions = new[]
        {
            new { Name = "OmenNature", Position = new Vector3(-6f, 1.2f, 5f) },
            new { Name = "OmenSociety", Position = new Vector3(-2.5f, 1.1f, 7f) },
            new { Name = "OmenGuide", Position = new Vector3(0f, 1.0f, 8f) },
            new { Name = "OmenDivination", Position = new Vector3(2.7f, 1.1f, 7f) },
            new { Name = "OmenPath", Position = new Vector3(6f, 1.2f, 5f) },
        };

        foreach (var definition in definitions)
        {
            var entity = new Entity(definition.Name)
            {
                Transform =
                {
                    Position = definition.Position,
                    Scale = new Vector3(0.18f),
                }
            };

            if (sphereModel is not null)
            {
                entity.Add(new ModelComponent(sphereModel));
            }

            var light = new LightComponent
            {
                Type = new LightPoint { Radius = 2f },
                Intensity = 0.15f,
            };

            entity.Add(light);
            scene.Entities.Add(entity);
            omenMarkers.Add(new OmenMarker(entity, light, (LightPoint)light.Type));
        }
    }

    private void DrawHud()
    {
        if (!hudVisible)
        {
            return;
        }

        DebugTextSystem? debugText = (Game as Stride.Engine.Game)?.DebugTextSystem;
        if (debugText is null)
        {
            return;
        }

        debugText.Print("Game58date vertical slice", new Int2(20, 20), HudTitleColor);
        debugText.Print($"Hero stage: {GetStageTitle(heroStage)}", new Int2(20, 44), HudTextColor);
        debugText.Print($"Target biome: {worldState.TargetBiome}", new Int2(20, 68), HudTextColor);
        debugText.Print(
            $"World law  explore {AsPct(worldState.ExplorationDrive)}  karma {worldState.Karma:+0.00;-0.00;0.00}  blessing {AsPct(worldState.BlessingWeight)}  pressure {AsPct(worldState.ResourcePressure)}",
            new Int2(20, 96),
            HudTextColor);
        debugText.Print(
            $"Omens      {GetOmenTitle(worldState.LastOmen)}  path {AsPct(worldState.PathVisibility)}  social {AsPct(worldState.SocialFlux)}  mood {AsPct(worldState.Atmosphere)}",
            new Int2(20, 120),
            HudTextColor);
        debugText.Print(
            $"Profile    violence {AsPct(worldState.Violence)}  faith {AsPct(worldState.Faith)}  curiosity {AsPct(worldState.Curiosity)}  horizon {AsPct(worldState.BorderLonging)}",
            new Int2(20, 144),
            HudTextColor);
        debugText.Print("Tab input toggle  Enter submit  F2 sea  F3 loss  F4 violent  F5 peaceful  F6 mentor  F1 HUD", new Int2(20, 176), HudHintColor);
        debugText.Print($"> {intentBuffer}", new Int2(20, 200), textInputEnabled ? HudInputColor : HudDisabledColor);

        int line = 236;
        foreach (string entry in eventLog)
        {
            debugText.Print(entry, new Int2(20, line), HudLogColor);
            line += 22;
        }
    }

    private void EnableTextInput()
    {
        if (textInputEnabled)
        {
            return;
        }

        textInputEnabled = true;
        Input.AddListener(this);
        Input.TextInput?.EnabledTextInput();
        Game.IsMouseVisible = true;
    }

    private void DisableTextInput()
    {
        if (!textInputEnabled)
        {
            return;
        }

        textInputEnabled = false;
        Input.TextInput?.DisableTextInput();
        Input.RemoveListener(this);
    }

    private Entity? FindEntity(string name)
    {
        return Entity.Scene?.Entities.FirstOrDefault(entity => entity.Name == name);
    }

    private void PushLog(string message)
    {
        eventLog.Enqueue($"[{worldTime,5:0.0}s] {message}");
        while (eventLog.Count > 7)
        {
            eventLog.Dequeue();
        }
    }

    private static bool ContainsAny(string source, params string[] keywords)
    {
        foreach (string keyword in keywords)
        {
            if (source.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string AsPct(float value)
    {
        return $"{Clamp01(value) * 100f:0}%";
    }

    private static float Clamp01(float value)
    {
        return MathUtil.Clamp(value, 0f, 1f);
    }

    private static Color3 LerpColor(Color3 from, Color3 to, float amount)
    {
        amount = Clamp01(amount);
        return new Color3(
            from.R + (to.R - from.R) * amount,
            from.G + (to.G - from.G) * amount,
            from.B + (to.B - from.B) * amount);
    }

    private static Color3 GetMarkerColor(int index, bool highlighted)
    {
        float intensity = highlighted ? 1.00f : 0.25f;
        return index switch
        {
            0 => new Color3(1.00f * intensity, 0.45f * intensity, 0.35f * intensity),
            1 => new Color3(1.00f * intensity, 0.82f * intensity, 0.36f * intensity),
            2 => new Color3(0.55f * intensity, 1.00f * intensity, 0.72f * intensity),
            3 => new Color3(0.68f * intensity, 0.82f * intensity, 1.00f * intensity),
            _ => new Color3(0.88f * intensity, 0.96f * intensity, 1.00f * intensity),
        };
    }

    private static string GetStageTitle(HeroJourneyStage stage)
    {
        return stage switch
        {
            HeroJourneyStage.OrdinaryWorld => "Ordinary World",
            HeroJourneyStage.CallToAdventure => "Call To Adventure",
            HeroJourneyStage.CrossingTheThreshold => "Crossing The Threshold",
            HeroJourneyStage.RoadOfTrials => "Road Of Trials",
            HeroJourneyStage.MeetingTheMentor => "Meeting The Mentor",
            HeroJourneyStage.ApproachToTheInmostCave => "Approach To The Inmost Cave",
            HeroJourneyStage.Transformation => "Transformation",
            _ => "Unknown",
        };
    }

    private static string GetOmenTitle(OmenType omen)
    {
        return omen switch
        {
            OmenType.NaturalAnomaly => "Natural Anomaly",
            OmenType.SocialShift => "Social Shift",
            OmenType.GuideArrival => "Guide Arrival",
            OmenType.Divination => "Divination",
            OmenType.PathRevelation => "Path Revelation",
            _ => "None",
        };
    }

    private enum HeroJourneyStage
    {
        OrdinaryWorld,
        CallToAdventure,
        CrossingTheThreshold,
        RoadOfTrials,
        MeetingTheMentor,
        ApproachToTheInmostCave,
        Transformation,
    }

    private enum OmenType
    {
        None,
        NaturalAnomaly,
        SocialShift,
        GuideArrival,
        Divination,
        PathRevelation,
    }

    private sealed class WorldLawState
    {
        public float ExplorationDrive { get; set; } = 0.08f;
        public float BorderLonging { get; set; } = 0.05f;
        public float BlessingWeight { get; set; } = 0.10f;
        public float ResourcePressure { get; set; } = 0.10f;
        public float Karma { get; set; }
        public float Violence { get; set; } = 0.10f;
        public float Faith { get; set; } = 0.08f;
        public float Curiosity { get; set; } = 0.12f;
        public float PathVisibility { get; set; }
        public float SocialFlux { get; set; } = 0.05f;
        public float Atmosphere { get; set; } = 0.10f;
        public float LossMemory { get; set; }
        public int IntentCount { get; set; }
        public int OmenCount { get; set; }
        public OmenType LastOmen { get; set; }
        public string LastOmenDescription { get; set; } = "No omen yet.";
        public string TargetBiome { get; set; } = "Morning Plain";
    }

    private sealed class OmenMarker
    {
        public OmenMarker(Entity entity, LightComponent lightComponent, LightPoint light)
        {
            Entity = entity;
            LightComponent = lightComponent;
            Light = light;
        }

        public Entity Entity { get; }

        public LightComponent LightComponent { get; }

        public LightPoint Light { get; }
    }
}
