#nullable enable
using System;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Lights;

namespace Game58date.Gameplay.UI;

public sealed class GameUiShowcaseScript : SyncScript, IGameUiCommandSink
{
    private static readonly string[] ShowcasePrompts =
    {
        "I want to cross the sea and reach a new trial.",
        "I want to find a mentor and a sign.",
        "I am searching for a relic hidden in ancient ruins.",
        "I want to help the wounded and calm the village.",
        "I want power, dominion, and whatever must be taken.",
    };

    private readonly GameUiTheme theme = GameUiTheme.Default;
    private readonly GameUiContextState uiContext = new()
    {
        ModeTag = "UI Showcase",
        Subtitle = "A dedicated demonstration scene for the formal contextual UI system, including intent, omen, perception, atlas, and state gradients.",
        HelpText = "Space next vignette  Q sense  Tab input  D draft  Esc atlas  H debug  F2..F6 quick motifs",
        MenuHintText = "This atlas is part showcase, part standards reference. Evaluate typography, density, hierarchy, and transitions against the production target.",
    };

    private readonly OmenPresentationController omenPresentation = new();

    private GameUiComposer? composer;
    private Entity? lightEntity;
    private LightComponent? lightComponent;
    private Color3 baseLightColor = new(1.0f, 0.98f, 0.94f);
    private float baseLightIntensity = 14f;
    private WorldLawRuntimeState state = new();
    private int vignetteIndex;
    private float autoCycleSeconds = 8f;
    private float pulseSeconds;
    private bool wasSpacePressed;
    private bool wasEscPressed;
    private bool wasTabPressed;
    private bool wasQPressed;
    private bool wasDPressed;
    private bool debugHudVisible;

    public override void Start()
    {
        Scene scene = Entity.Scene ?? throw new InvalidOperationException("UI showcase requires an active scene.");
        SetupSceneReferences(scene);

        composer = new GameUiComposer(theme);
        composer.Attach(Entity, this);

        ApplyVignette(0);
        UpdateDraftText(ShowcasePrompts[0]);
        composer.Update(GameUiStateMapper.Map(state, uiContext, theme));
    }

    public override void Update()
    {
        if (composer is null)
        {
            return;
        }

        float deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
        state.WorldTimeSeconds += deltaTime;
        pulseSeconds += deltaTime;
        autoCycleSeconds -= deltaTime;

        HandleInput();
        AnimateState(deltaTime);
        omenPresentation.Update(state, deltaTime);
        ApplySceneLighting();

        uiContext.DebugHudVisible = debugHudVisible;
        composer.Update(GameUiStateMapper.Map(state, uiContext, theme));
        DrawOverlayDebug();
    }

    public void ToggleUiMenu()
    {
        uiContext.MenuVisible = !uiContext.MenuVisible;
    }

    public void ToggleNarrativeInput()
    {
        state.Intent.TextInputEnabled = !state.Intent.TextInputEnabled;
    }

    public void ToggleDebugHud()
    {
        debugHudVisible = !debugHudVisible;
    }

    private void HandleInput()
    {
        bool spacePressed = Input.IsKeyPressed(Keys.Space);
        if (spacePressed && !wasSpacePressed)
        {
            AdvanceVignette();
        }
        wasSpacePressed = spacePressed;

        bool escPressed = Input.IsKeyPressed(Keys.Escape) || Input.IsKeyPressed(Keys.F10);
        if (escPressed && !wasEscPressed)
        {
            ToggleUiMenu();
        }
        wasEscPressed = escPressed;

        bool tabPressed = Input.IsKeyPressed(Keys.Tab);
        if (tabPressed && !wasTabPressed)
        {
            ToggleNarrativeInput();
        }
        wasTabPressed = tabPressed;

        bool qPressed = Input.IsKeyPressed(Keys.Q);
        if (qPressed && !wasQPressed)
        {
            state.Perception.IsActive = !state.Perception.IsActive;
            state.Perception.Intensity = state.Perception.IsActive ? 0.82f : 0.16f;
            state.Perception.ActiveSecondsRemaining = state.Perception.IsActive ? 5.4f : 0f;
            state.Perception.CooldownSecondsRemaining = state.Perception.IsActive ? 11.5f : 2.2f;
            if (state.Perception.IsActive)
            {
                state.Perception.ActivationCount++;
                omenPresentation.HandlePerceptionActivated(state.Perception.Intensity);
            }
        }
        wasQPressed = qPressed;

        bool dPressed = Input.IsKeyPressed(Keys.D);
        if (dPressed && !wasDPressed)
        {
            RotateDraftText();
        }
        wasDPressed = dPressed;

        if (Input.IsKeyPressed(Keys.H))
        {
            debugHudVisible = !debugHudVisible;
        }

        if (Input.IsKeyPressed(Keys.F2))
        {
            ApplyVignette(0);
        }
        else if (Input.IsKeyPressed(Keys.F3))
        {
            ApplyVignette(1);
        }
        else if (Input.IsKeyPressed(Keys.F4))
        {
            ApplyVignette(2);
        }
        else if (Input.IsKeyPressed(Keys.F5))
        {
            ApplyVignette(3);
        }
        else if (Input.IsKeyPressed(Keys.F6))
        {
            ApplyVignette(4);
        }

        if (autoCycleSeconds <= 0f)
        {
            AdvanceVignette();
        }
    }

    private void AdvanceVignette()
    {
        ApplyVignette((vignetteIndex + 1) % ShowcasePrompts.Length);
    }

    private void ApplyVignette(int index)
    {
        vignetteIndex = index;
        autoCycleSeconds = 9.5f;
        pulseSeconds = 0f;
        state = CreateBaseState();

        switch (index)
        {
            case 0:
                uiContext.ModeTag = "Exploration HUD";
                UpdateDraftText(ShowcasePrompts[index]);
                state.Narrative.CurrentStage = HeroJourneyStage.RoadOfTrials;
                state.Narrative.LastStageReason = "Long-range travel intent and visible routes turned the clean HUD into an active exploration state.";
                state.World.TargetBiome = "Desert Crossing";
                state.World.ExplorationDrive = 0.78f;
                state.World.BorderLonging = 0.66f;
                state.World.PathVisibility = 0.74f;
                state.World.BlessingWeight = 0.28f;
                state.Behavior.ExplorationTendency = 0.84f;
                state.Intent.SubmittedIntentCount = 3;
                state.Intent.LastIntent = CreateIntent(IntentTopic.Exploration, 0.92f, ShowcasePrompts[index], "Boundary-crossing exploration intent focused on the sea horizon.", "Desert Crossing");
                PushOmen(OmenType.PathRevelation, OmenSource.Intent, 0.90f, "The horizon answered the crossing request with a revealed route toward a hidden dock.");
                break;
            case 1:
                uiContext.ModeTag = "Mentor Encounter";
                UpdateDraftText(ShowcasePrompts[index]);
                state.Narrative.CurrentStage = HeroJourneyStage.MeetingTheMentor;
                state.Narrative.LastStageReason = "Faith, positive karma, and guidance-seeking behavior converged into a mentor-facing interface state.";
                state.World.TargetBiome = "Pilgrim Road";
                state.World.Karma = 0.42f;
                state.World.Faith = 0.72f;
                state.World.PathVisibility = 0.48f;
                state.Behavior.PeacefulTendency = 0.68f;
                state.Behavior.FaithTendency = 0.72f;
                state.Intent.SubmittedIntentCount = 4;
                state.Intent.LastIntent = CreateIntent(IntentTopic.Mentor, 0.90f, ShowcasePrompts[index], "Guidance-seeking intent focused on mentors and signs.", "Pilgrim Road");
                state.Perception.IsActive = true;
                state.Perception.Intensity = 0.88f;
                state.Perception.ActiveSecondsRemaining = 4.8f;
                state.Perception.CooldownSecondsRemaining = 13.6f;
                state.Perception.ActivationCount = 2;
                PushOmen(OmenType.GuideArrival, OmenSource.Narrative, 0.84f, "A patient guide stepped into view as the mentor stage resolved.");
                break;
            case 2:
                uiContext.ModeTag = "Divination Layer";
                UpdateDraftText(ShowcasePrompts[index]);
                state.Narrative.CurrentStage = HeroJourneyStage.ApproachToTheInmostCave;
                state.Narrative.LastStageReason = "Commitment, repeated intent, and high path clarity unlocked the divinatory approach state.";
                state.World.TargetBiome = "Ruined Basin";
                state.World.Curiosity = 0.86f;
                state.World.PathVisibility = 0.78f;
                state.World.BlessingWeight = 0.58f;
                state.World.Atmosphere = 0.64f;
                state.Behavior.CuriosityTendency = 0.88f;
                state.Intent.SubmittedIntentCount = 5;
                state.Intent.LastIntent = CreateIntent(IntentTopic.Knowledge, 0.88f, ShowcasePrompts[index], "Discovery intent focused on relics, ruins, and hidden geometry.", "Ruined Basin");
                PushOmen(OmenType.Divination, OmenSource.Intent, 0.87f, "The world reflected the search for hidden knowledge through luminous divinatory structure.");
                state.RecentCausality.Add(new CausalityRecord
                {
                    TimestampUtc = DateTimeOffset.UtcNow,
                    ActionKind = PlayerActionKind.LossRegistered,
                    Summary = "A prior loss increased the likelihood of a revelatory signal.",
                    BlessingDelta = 0.36f,
                    TriggeredOmen = OmenType.Divination,
                });
                break;
            case 3:
                uiContext.ModeTag = "Compassion State";
                UpdateDraftText(ShowcasePrompts[index]);
                state.Narrative.CurrentStage = HeroJourneyStage.CrossingTheThreshold;
                state.Narrative.LastStageReason = "Compassionate intent reshaped the social field and produced a lighter contextual surface.";
                state.World.TargetBiome = "Market Refuge";
                state.World.Karma = 0.58f;
                state.World.SocialFlux = 0.72f;
                state.World.Faith = 0.48f;
                state.Behavior.PeacefulTendency = 0.82f;
                state.Behavior.IntentActions = 3;
                state.Behavior.PeacefulActions = 4;
                state.Intent.SubmittedIntentCount = 2;
                state.Intent.LastIntent = CreateIntent(IntentTopic.Compassion, 0.86f, ShowcasePrompts[index], "Compassion intent focused on peace, aid, and restoring social order.", "Market Refuge");
                PushOmen(OmenType.SocialShift, OmenSource.Causality, 0.81f, "Lanterns returned to the market after repeated compassionate choices.");
                break;
            case 4:
                uiContext.ModeTag = "Backlash State";
                UpdateDraftText(ShowcasePrompts[index]);
                state.Narrative.CurrentStage = HeroJourneyStage.RoadOfTrials;
                state.Narrative.LastStageReason = "Aggressive intent and accumulated pressure forced the UI into a danger-weighted omen state.";
                state.World.TargetBiome = "Ash Frontier";
                state.World.Karma = -0.62f;
                state.World.ResourcePressure = 0.84f;
                state.World.Atmosphere = 0.76f;
                state.World.PathVisibility = 0.22f;
                state.Behavior.ViolentTendency = 0.86f;
                state.Behavior.ViolentActions = 5;
                state.Intent.SubmittedIntentCount = 3;
                state.Intent.LastIntent = CreateIntent(IntentTopic.Domination, 0.87f, ShowcasePrompts[index], "Dominance intent focused on violence, control, and plunder.", "Ash Frontier");
                PushOmen(OmenType.NaturalAnomaly, OmenSource.EmergentWorldLaw, 0.92f, "Resource pressure and hostile karma condensed into a violent natural backlash.");
                break;
        }

        state.HeroStage = state.Narrative.CurrentStage;
        state.Behavior.RecordedActions = Math.Max(state.Behavior.RecordedActions, 6);
        uiContext.MenuVisible = false;
        uiContext.DebugHudVisible = debugHudVisible;
    }

    private void AnimateState(float deltaTime)
    {
        float pulse = 0.5f + MathF.Sin(pulseSeconds * 1.4f) * 0.5f;
        state.World.Atmosphere = MathUtil.Clamp(state.World.Atmosphere + MathF.Sin(pulseSeconds * 0.7f) * 0.0025f, 0f, 1f);
        state.Omen.VisualSeconds = MathF.Max(0f, state.Omen.VisualSeconds - deltaTime);
        state.Perception.ActiveSecondsRemaining = state.Perception.IsActive
            ? MathF.Max(0f, state.Perception.ActiveSecondsRemaining - deltaTime)
            : 0f;
        state.Perception.CooldownSecondsRemaining = MathF.Max(0f, state.Perception.CooldownSecondsRemaining - deltaTime);

        if (state.Perception.IsActive && state.Perception.ActiveSecondsRemaining <= 0f)
        {
            state.Perception.IsActive = false;
        }

        if (state.Perception.IsActive)
        {
            state.Perception.Intensity = MathUtil.Clamp(0.60f + pulse * 0.28f, 0f, 1f);
        }
        else
        {
            state.Perception.Intensity = MathF.Max(0.10f, state.Perception.Intensity - deltaTime * 0.12f);
        }

        if (state.Omen.ActiveOmen is not null)
        {
            state.Omen.LastScore = MathUtil.Clamp(state.Omen.ActiveOmen.Score - 0.04f + pulse * 0.08f, 0f, 1f);
        }
    }

    private void ApplySceneLighting()
    {
        if (lightComponent is not null)
        {
            GameUiSceneLighting.Apply(lightComponent, baseLightColor, baseLightIntensity, state);
        }
    }

    private void SetupSceneReferences(Scene scene)
    {
        lightEntity = FindEntity(scene, "Directional light");
        lightComponent = lightEntity?.Get<LightComponent>();
        if (lightComponent is not null)
        {
            baseLightColor = lightComponent.GetColor();
            baseLightIntensity = lightComponent.Intensity;
        }

        omenPresentation.Initialize(scene);
    }

    private void UpdateDraftText(string text)
    {
        uiContext.IntentDraftText = state.Intent.TextInputEnabled ? text : string.Empty;
    }

    private void RotateDraftText()
    {
        int promptIndex = (vignetteIndex + 2) % ShowcasePrompts.Length;
        UpdateDraftText(ShowcasePrompts[promptIndex]);
    }

    private static WorldLawRuntimeState CreateBaseState()
    {
        return new WorldLawRuntimeState
        {
            WorldTimeSeconds = 18f,
            World = new WorldLawState
            {
                ExplorationDrive = 0.34f,
                BorderLonging = 0.18f,
                BlessingWeight = 0.20f,
                ResourcePressure = 0.14f,
                Karma = 0.08f,
                Violence = 0.12f,
                Faith = 0.18f,
                Curiosity = 0.28f,
                PathVisibility = 0.24f,
                SocialFlux = 0.18f,
                Atmosphere = 0.22f,
                IntentCount = 2,
                OmenCount = 1,
                LastOmen = OmenType.Divination,
                LastOmenDescription = "A dormant omen waits in the atmospheric field.",
                TargetBiome = "Morning Plain",
            },
            Behavior = new PlayerBehaviorProfile
            {
                PeacefulTendency = 0.24f,
                ViolentTendency = 0.18f,
                ExplorationTendency = 0.42f,
                FaithTendency = 0.22f,
                CuriosityTendency = 0.34f,
                RecordedActions = 4,
            },
            Narrative = new HeroJourneyRuntimeState
            {
                CurrentStage = HeroJourneyStage.CallToAdventure,
                LastStageReason = "A showcase baseline state before a focused vignette takes over.",
            },
            Intent = new PlayerIntentRuntimeState
            {
                TextInputEnabled = true,
                SubmittedIntentCount = 1,
            },
            Omen = new OmenRuntimeState
            {
                CooldownSeconds = 3f,
                VisualSeconds = 6f,
                LastSource = OmenSource.Intent,
                LastScore = 0.65f,
            },
            Perception = new PerceptionRuntimeState
            {
                Intensity = 0.16f,
                CooldownSecondsRemaining = 2.6f,
            },
        };
    }

    private void PushOmen(OmenType type, OmenSource source, float score, string description)
    {
        var omen = new OmenRecord
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            OmenType = type,
            Source = source,
            Score = score,
            Description = description,
        };

        state.Omen.ActiveOmen = omen;
        state.Omen.LastScore = score;
        state.Omen.LastSource = source;
        state.Omen.VisualSeconds = 6f;
        state.Omen.History.Add(omen);
        state.World.LastOmen = type;
        state.World.LastOmenDescription = description;
        state.World.OmenCount = Math.Max(state.World.OmenCount, state.Omen.History.Count);
        omenPresentation.HandleOmenActivated(omen);
    }

    private static PlayerIntentRecord CreateIntent(IntentTopic topic, float confidence, string rawText, string summary, string biome)
    {
        return new PlayerIntentRecord
        {
            TimestampUtc = DateTimeOffset.UtcNow,
            Topic = topic,
            Confidence = confidence,
            RawText = rawText,
            Summary = summary,
            SuggestedTargetBiome = biome,
        };
    }

    private static Entity? FindEntity(Scene scene, string name)
    {
        foreach (Entity entity in scene.Entities)
        {
            if (entity.Name == name)
            {
                return entity;
            }
        }

        return null;
    }

    private void DrawOverlayDebug()
    {
        if (!debugHudVisible)
        {
            return;
        }

        var debugText = (Game as Stride.Engine.Game)?.DebugTextSystem;
        if (debugText is null)
        {
            return;
        }

        debugText.Print("UI Showcase", new Int2(20, 20), new Color4(1.0f, 0.95f, 0.60f, 1.0f));
        debugText.Print($"Vignette {vignetteIndex + 1}/{ShowcasePrompts.Length}  Mode {uiContext.ModeTag}", new Int2(20, 44), Color4.White);
        debugText.Print($"Prompt: {uiContext.IntentDraftText}", new Int2(20, 68), new Color4(0.65f, 0.90f, 1.0f, 1.0f));
        debugText.Print("Space next  Q perception  Tab input  D draft rotate  Esc atlas  H debug", new Int2(20, 92), new Color4(0.80f, 0.80f, 0.80f, 1.0f));
    }
}
