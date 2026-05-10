namespace Game58date.Terrain;

public sealed class TerrainRuntimeStats
{
    public int LoadedChunkCount { get; set; }

    public int VisibleFaceCount { get; set; }

    public int SolidFaceCount { get; set; }

    public int WaterFaceCount { get; set; }

    public int QueuedBuildCount { get; set; }

    public int RunningBuildCount { get; set; }

    public int EnvironmentEntityCount { get; set; }

    public int EnvironmentChunkCount { get; set; }

    public int EnvironmentPooledEntityCount { get; set; }

    public string WeatherSummary { get; set; } = "clear";

    public string LastErrorMessage { get; set; } = "None";
}
