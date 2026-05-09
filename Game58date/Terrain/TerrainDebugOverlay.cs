#nullable enable
using Stride.Core.Mathematics;
using Stride.Profiling;

namespace Game58date.Terrain;

public sealed class TerrainDebugOverlay
{
    private static readonly Color4 TitleColor = new(1f, 0.95f, 0.65f, 1f);
    private static readonly Color4 TextColor = new(1f, 1f, 1f, 1f);
    private static readonly Color4 HintColor = new(0.75f, 0.75f, 0.75f, 1f);

    public void Draw(DebugTextSystem? debugText, TerrainRuntimeStats stats, TerrainGenerationSettings settings)
    {
        if (debugText is null)
        {
            return;
        }

        debugText.Print("Voxel terrain runtime", new Int2(20, 20), TitleColor);
        debugText.Print($"Chunks loaded: {stats.LoadedChunkCount}", new Int2(20, 44), TextColor);
        debugText.Print($"Faces built: {stats.VisibleFaceCount} total", new Int2(20, 68), TextColor);
        debugText.Print($"Solid/Water: {stats.SolidFaceCount} / {stats.WaterFaceCount}", new Int2(20, 92), TextColor);
        debugText.Print($"Chunk size: {settings.ChunkSize}x{settings.ChunkHeight}x{settings.ChunkSize}", new Int2(20, 116), TextColor);
        debugText.Print($"View distance: {settings.ViewDistanceInChunks} chunks", new Int2(20, 140), TextColor);
        debugText.Print($"Build queue: {stats.QueuedBuildCount} queued / {stats.RunningBuildCount} running", new Int2(20, 164), TextColor);
        debugText.Print($"Last error: {stats.LastErrorMessage}", new Int2(20, 188), TextColor);
        debugText.Print("F7 rebuilds visible terrain", new Int2(20, 212), HintColor);
    }
}
