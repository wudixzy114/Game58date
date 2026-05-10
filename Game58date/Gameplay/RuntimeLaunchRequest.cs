#nullable enable
using Game58date.Save;
using Game58date.Terrain;

namespace Game58date.Gameplay;

public sealed class RuntimeLaunchRequest
{
    public RuntimeLaunchTarget Target { get; init; }

    public TerrainRuntimeLaunchOverrides? TerrainOverrides { get; init; }

    public string LoadingTitleText { get; init; } = "Loading";

    public string LoadingDetailText { get; init; } = "Preparing the next scene.";

    public string LoadingStatusText { get; init; } = "Please wait";

    public static RuntimeLaunchRequest Create(RuntimeLaunchTarget target)
    {
        return target switch
        {
            RuntimeLaunchTarget.DevRouter => new RuntimeLaunchRequest
            {
                Target = target,
                LoadingTitleText = "Returning To Router",
                LoadingDetailText = "Detaching gameplay systems and reopening the runtime selector.",
                LoadingStatusText = "Resetting scene state",
            },
            RuntimeLaunchTarget.MainMenu => new RuntimeLaunchRequest
            {
                Target = target,
                LoadingTitleText = "Opening Main Menu",
                LoadingDetailText = "Preparing the front-door menu scene and cinematic UI surface.",
                LoadingStatusText = "Building menu scene",
            },
            RuntimeLaunchTarget.UiShowcase => new RuntimeLaunchRequest
            {
                Target = target,
                LoadingTitleText = "Opening UI Showcase",
                LoadingDetailText = "Loading the dedicated interface presentation scene.",
                LoadingStatusText = "Assembling showcase",
            },
            RuntimeLaunchTarget.Prototype => new RuntimeLaunchRequest
            {
                Target = target,
                LoadingTitleText = "Opening Legacy Prototype",
                LoadingDetailText = "Switching to the earlier experimental runtime chain.",
                LoadingStatusText = "Attaching prototype systems",
            },
            _ => new RuntimeLaunchRequest
            {
                Target = target,
                LoadingTitleText = "Entering Terrain Runtime",
                LoadingDetailText = "Loading terrain chunks, world-law systems, and the exploration runtime.",
                LoadingStatusText = "Initializing world",
            },
        };
    }

    public static RuntimeLaunchRequest CreateContinueJourney()
    {
        return new RuntimeLaunchRequest
        {
            Target = RuntimeLaunchTarget.Terrain,
            TerrainOverrides = new TerrainRuntimeLaunchOverrides
            {
                SaveSlotName = GameSaveRepository.DefaultSlotName,
                StartFreshJourney = false,
                UseSavedPlayerPose = true,
            },
            LoadingTitleText = "Continuing Journey",
            LoadingDetailText = "Recovering the saved world state, terrain edits, and your last recorded position.",
            LoadingStatusText = "Restoring expedition state",
        };
    }

    public static RuntimeLaunchRequest CreateNewJourney()
    {
        return new RuntimeLaunchRequest
        {
            Target = RuntimeLaunchTarget.Terrain,
            TerrainOverrides = new TerrainRuntimeLaunchOverrides
            {
                SaveSlotName = GameSaveRepository.DefaultSlotName,
                StartFreshJourney = true,
                UseSavedPlayerPose = false,
            },
            LoadingTitleText = "Beginning New Journey",
            LoadingDetailText = "Clearing prior field memory and returning to the default spawn point for a fresh start.",
            LoadingStatusText = "Resetting save state",
        };
    }
}
