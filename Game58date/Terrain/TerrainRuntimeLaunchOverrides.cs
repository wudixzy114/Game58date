#nullable enable

namespace Game58date.Terrain;

public sealed class TerrainRuntimeLaunchOverrides
{
    public string? SaveSlotName { get; init; }

    public int? PreferredSeed { get; init; }

    public float? AutosaveIntervalSeconds { get; init; }

    public bool? StartFreshJourney { get; init; }

    public bool? UseSavedPlayerPose { get; init; }
}
