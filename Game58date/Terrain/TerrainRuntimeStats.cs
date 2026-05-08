namespace Game58date.Terrain;

public sealed class TerrainRuntimeStats
{
    public int LoadedChunkCount { get; set; }

    public int VisibleFaceCount { get; set; }

    public int QueuedBuildCount { get; set; }

    public int RunningBuildCount { get; set; }

    public string LastErrorMessage { get; set; } = "None";
}
