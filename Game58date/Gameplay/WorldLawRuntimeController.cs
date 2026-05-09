#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using Game58date.Terrain;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Rendering.Lights;

namespace Game58date.Gameplay;

public sealed class WorldLawRuntimeController : SyncScript, IInputEventListener, IInputEventListener<TextInputEvent>
{
    private static readonly Color4 HudTitleColor = new(1.00f, 0.95f, 0.60f, 1.00f);
    private static readonly Color4 HudTextColor = new(0.95f, 0.95f, 0.95f, 1.00f);
    private static readonly Color4 HudHintColor = new(0.75f, 0.75f, 0.75f, 0.95f);
    private static readonly Color4 HudInputColor = new(0.50f, 0.95f, 1.00f, 1.00f);
    private static readonly Color4 HudDisabledColor = new(0.45f, 0.45f, 0.45f, 1.00f);
    private static readonly Color4 HudLogColor = new(0.85f, 0.85f, 0.85f, 1.00f);

    private readonly Queue<string> eventLog = new();
    private readonly StringBuilder intentBuffer = new();

    private Entity? cameraEntity;
    private Entity? lightEntity;
    private LightComponent? lightComponent;
    private Color3 baseLightColor = new(1.0f, 0.98f, 0.94f);
    private float baseLightIntensity;
    private Vector3 lastTrackedEyePosition;
    private bool hasTrackedEyePosition;
    private bool hudVisible = true;
    private bool textInputEnabled;
    private PlayerIntentSystem? intentSystem;
    private HeroJourneyDirector? heroJourneyDirector;

    public WorldLawEngine? Engine { get; private set; }

    public WorldLawRuntimeState RuntimeState => Engine?.State ?? new WorldLawRuntimeState();

    public void Initialize(WorldLawRuntimeState initialState, Entity camera, Entity? directionalLight)
    {
        cameraEntity = camera ?? throw new ArgumentNullException(nameof(camera));
        lightEntity = directionalLight;
        lightComponent = directionalLight?.Get<LightComponent>();
        if (lightComponent is not null)
        {
            baseLightColor = lightComponent.GetColor();
            baseLightIntensity = lightComponent.Intensity;
        }

        Engine = new WorldLawEngine(initialState.Clone());
        intentSystem = new PlayerIntentSystem(Engine.State.Intent);
        heroJourneyDirector = new HeroJourneyDirector(Engine.State.Narrative);
        Engine.LogGenerated += HandleLogGenerated;
        Engine.HeroStageAdvanced += HandleHeroStageAdvanced;
        Engine.OmenTriggered += HandleOmenTriggered;
        heroJourneyDirector.StageAdvanced += HandleNarrativeStageAdvanced;
    }

    public override void Start()
    {
        if (Engine is null)
        {
            throw new InvalidOperationException("World law runtime controller must be initialized before Start.");
        }

        textInputEnabled = Engine.State.Intent.TextInputEnabled;
        if (textInputEnabled)
        {
            EnableTextInput();
        }
        PushLog("World law runtime ready.");
        PushLog("Tab input toggle. Enter submit intent. F2 sea. F3 loss. F4 violent. F5 peaceful. F6 mentor.");
        UpdateLighting();
    }

    public override void Update()
    {
        if (Engine is null || cameraEntity is null)
        {
            return;
        }

        float deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
        HandleHotkeys();
        TrackExplorationProgress();
        Engine.Tick(deltaTime);
        SyncNarrativeState();
        UpdateLighting();
        DrawHud();
    }

    public override void Cancel()
    {
        DisableTextInput();

        if (Engine is not null)
        {
            Engine.LogGenerated -= HandleLogGenerated;
            Engine.HeroStageAdvanced -= HandleHeroStageAdvanced;
            Engine.OmenTriggered -= HandleOmenTriggered;
        }

        if (heroJourneyDirector is not null)
        {
            heroJourneyDirector.StageAdvanced -= HandleNarrativeStageAdvanced;
        }

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

    public WorldLawRuntimeState CreateSnapshot()
    {
        if (Engine is null)
        {
            return new WorldLawRuntimeState();
        }

        return Engine.State.Clone();
    }

    private void HandleHotkeys()
    {
        if (Engine is null)
        {
            return;
        }

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
            string intent = intentBuffer.ToString();
            intentBuffer.Clear();
            SubmitIntent(intent);
        }

        if (Input.IsKeyPressed(Keys.F2))
        {
            SubmitIntent("I want to cross the sea and reach a new trial.");
        }

        if (Input.IsKeyPressed(Keys.F3))
        {
            Engine.RegisterLoss("A serious setback was recorded.");
        }

        if (Input.IsKeyPressed(Keys.F4))
        {
            Engine.RegisterViolentChoice();
        }

        if (Input.IsKeyPressed(Keys.F5))
        {
            Engine.RegisterPeacefulChoice();
        }

        if (Input.IsKeyPressed(Keys.F6))
        {
            SubmitIntent("I want to find a mentor and a sign.");
        }
    }

    private void TrackExplorationProgress()
    {
        if (Engine is null || cameraEntity is null)
        {
            return;
        }

        Vector3 currentEyePosition = cameraEntity.Transform.Position;
        if (!hasTrackedEyePosition)
        {
            lastTrackedEyePosition = currentEyePosition;
            hasTrackedEyePosition = true;
            return;
        }

        Vector3 delta = currentEyePosition - lastTrackedEyePosition;
        delta.Y = 0f;
        float distance = delta.Length();
        if (distance < 6f)
        {
            return;
        }

        lastTrackedEyePosition = currentEyePosition;
        Engine.RegisterExplorationProgress(distance);
        SyncNarrativeState();
    }

    private void UpdateLighting()
    {
        if (Engine is null || lightComponent is null)
        {
            return;
        }

        WorldLawRuntimeState state = Engine.State;
        float worldTime = state.WorldTimeSeconds;
        float baseWave = 0.88f + MathF.Sin(worldTime * 0.8f) * 0.12f;
        float omenBoost = state.World.LastOmen switch
        {
            OmenType.PathRevelation => 4.5f,
            OmenType.GuideArrival => 2.2f,
            OmenType.NaturalAnomaly => -4.0f,
            OmenType.SocialShift => 1.2f,
            OmenType.Divination => 3.0f,
            _ => 0f,
        };

        lightComponent.Intensity = MathF.Max(5f, baseLightIntensity * baseWave + omenBoost + state.World.Karma * 1.4f);

        Color3 stageColor = state.Narrative.CurrentStage switch
        {
            HeroJourneyStage.OrdinaryWorld => new Color3(1.00f, 1.00f, 1.00f),
            HeroJourneyStage.CallToAdventure => new Color3(1.00f, 0.92f, 0.80f),
            HeroJourneyStage.CrossingTheThreshold => new Color3(0.88f, 0.95f, 1.00f),
            HeroJourneyStage.RoadOfTrials => new Color3(1.00f, 0.80f, 0.55f),
            HeroJourneyStage.MeetingTheMentor => new Color3(0.82f, 1.00f, 0.84f),
            HeroJourneyStage.ApproachToTheInmostCave => new Color3(0.75f, 0.82f, 1.00f),
            HeroJourneyStage.Transformation => new Color3(1.00f, 0.96f, 0.66f),
            _ => baseLightColor,
        };

        float blend = MathUtil.Clamp(state.World.Atmosphere * 0.55f + state.World.PathVisibility * 0.45f, 0f, 1f);
        lightComponent.SetColor(LerpColor(baseLightColor, stageColor, blend));
    }

    private void DrawHud()
    {
        if (!hudVisible || Engine is null)
        {
            return;
        }

        var debugText = (Game as Stride.Engine.Game)?.DebugTextSystem;
        if (debugText is null)
        {
            return;
        }

        WorldLawRuntimeState state = Engine.State;
        debugText.Print("Game58date world law runtime", new Int2(20, 20), HudTitleColor);
        debugText.Print($"Hero stage: {WorldLawEngine.GetStageTitle(state.Narrative.CurrentStage)}", new Int2(20, 44), HudTextColor);
        debugText.Print($"Target biome: {state.World.TargetBiome}", new Int2(20, 68), HudTextColor);
        debugText.Print(
            $"World law  explore {AsPct(state.World.ExplorationDrive)}  karma {state.World.Karma:+0.00;-0.00;0.00}  blessing {AsPct(state.World.BlessingWeight)}  pressure {AsPct(state.World.ResourcePressure)}",
            new Int2(20, 96),
            HudTextColor);
        debugText.Print(
            $"Omens      {WorldLawEngine.GetOmenTitle(state.World.LastOmen)}  path {AsPct(state.World.PathVisibility)}  social {AsPct(state.World.SocialFlux)}  mood {AsPct(state.World.Atmosphere)}",
            new Int2(20, 120),
            HudTextColor);
        debugText.Print(
            $"Profile    peace {AsPct(state.Behavior.PeacefulTendency)}  violence {AsPct(state.Behavior.ViolentTendency)}  faith {AsPct(state.Behavior.FaithTendency)}  curiosity {AsPct(state.Behavior.CuriosityTendency)}",
            new Int2(20, 144),
            HudTextColor);
        debugText.Print(
            $"Counts     actions {state.Behavior.RecordedActions}  intents {state.Behavior.IntentActions}  peace {state.Behavior.PeacefulActions}  violent {state.Behavior.ViolentActions}  losses {state.Behavior.LossEvents}",
            new Int2(20, 168),
            HudTextColor);
        debugText.Print(
            $"Intent     {GetIntentTopicTitle(state.Intent.LastIntent?.Topic ?? IntentTopic.Unknown)}  conf {(state.Intent.LastIntent?.Confidence ?? 0f) * 100f:0}%  total {state.Intent.SubmittedIntentCount}",
            new Int2(20, 192),
            HudTextColor);
        debugText.Print("Tab input toggle  Enter submit  F2 sea  F3 loss  F4 violent  F5 peaceful  F6 mentor  F1 HUD", new Int2(20, 220), HudHintColor);
        debugText.Print($"> {intentBuffer}", new Int2(20, 244), textInputEnabled ? HudInputColor : HudDisabledColor);

        int line = 280;
        foreach (string entry in eventLog)
        {
            debugText.Print(entry, new Int2(20, line), HudLogColor);
            line += 22;
        }
    }

    private void HandleLogGenerated(string message)
    {
        PushLog(message);
    }

    private void HandleHeroStageAdvanced(HeroJourneyStage _)
    {
        UpdateLighting();
    }

    private void HandleOmenTriggered(OmenType _, string __)
    {
        UpdateLighting();
    }

    private void HandleNarrativeStageAdvanced(HeroJourneyStage stage, string reason)
    {
        if (Engine is null)
        {
            return;
        }

        Engine.State.HeroStage = stage;
        PushLog($"Narrative: {WorldLawEngine.GetStageTitle(stage)}");
        PushLog(reason);
        UpdateLighting();
    }

    private void PushLog(string message)
    {
        float time = Engine?.State.WorldTimeSeconds ?? 0f;
        eventLog.Enqueue($"[{time,5:0.0}s] {message}");
        while (eventLog.Count > 8)
        {
            eventLog.Dequeue();
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
        if (Engine is not null)
        {
            Engine.State.Intent.TextInputEnabled = true;
        }
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
        if (Engine is not null)
        {
            Engine.State.Intent.TextInputEnabled = false;
        }
    }

    private void SubmitIntent(string rawIntent)
    {
        if (Engine is null || intentSystem is null)
        {
            return;
        }

        PlayerIntentRecord? record = intentSystem.Submit(rawIntent);
        if (record is null)
        {
            PushLog("Empty intent ignored.");
            return;
        }

        PushLog($"Intent topic: {GetIntentTopicTitle(record.Topic)} ({record.Confidence * 100f:0}%)");
        if (!string.IsNullOrWhiteSpace(record.SuggestedTargetBiome))
        {
            Engine.State.World.TargetBiome = record.SuggestedTargetBiome;
        }

        Engine.SubmitIntent(record.RawText);
        SyncNarrativeState();
    }

    private void SyncNarrativeState()
    {
        if (Engine is null || heroJourneyDirector is null)
        {
            return;
        }

        heroJourneyDirector.Evaluate(Engine.State);
        Engine.State.HeroStage = Engine.State.Narrative.CurrentStage;
    }

    private static string AsPct(float value)
    {
        return $"{MathUtil.Clamp(value, 0f, 1f) * 100f:0}%";
    }

    private static Color3 LerpColor(Color3 from, Color3 to, float amount)
    {
        amount = MathUtil.Clamp(amount, 0f, 1f);
        return new Color3(
            from.R + (to.R - from.R) * amount,
            from.G + (to.G - from.G) * amount,
            from.B + (to.B - from.B) * amount);
    }

    private static string GetIntentTopicTitle(IntentTopic topic)
    {
        return topic switch
        {
            IntentTopic.Exploration => "Exploration",
            IntentTopic.Mentor => "Mentor",
            IntentTopic.Knowledge => "Knowledge",
            IntentTopic.Compassion => "Compassion",
            IntentTopic.Domination => "Domination",
            _ => "Unknown",
        };
    }
}
