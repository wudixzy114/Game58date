using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace Game58date.Terrain;

public sealed class VoxelChunkCollisionBuilder
{
    private readonly TerrainGenerationSettings settings;

    public VoxelChunkCollisionBuilder(TerrainGenerationSettings settings)
    {
        this.settings = settings;
    }

    public VoxelChunkCollisionData Build(VoxelChunkData chunk)
    {
        var collisionData = new VoxelChunkCollisionData();
        var runsByColumn = BuildRuns(chunk);
        var visited = new HashSet<RunKey>();
        float scale = settings.VoxelScale;

        for (int z = 0; z < chunk.Size; z++)
        {
            for (int x = 0; x < chunk.Size; x++)
            {
                List<VerticalRun> runs = runsByColumn[x, z];
                for (int runIndex = 0; runIndex < runs.Count; runIndex++)
                {
                    VerticalRun run = runs[runIndex];
                    var originKey = new RunKey(x, z, run.MinY, run.MaxY);
                    if (!visited.Add(originKey))
                    {
                        continue;
                    }

                    int width = 1;
                    while (x + width < chunk.Size && HasRun(runsByColumn[x + width, z], run) && visited.Add(new RunKey(x + width, z, run.MinY, run.MaxY)))
                    {
                        width++;
                    }

                    int depth = 1;
                    bool canGrowDepth = true;
                    while (z + depth < chunk.Size && canGrowDepth)
                    {
                        for (int dx = 0; dx < width; dx++)
                        {
                            if (!HasRun(runsByColumn[x + dx, z + depth], run))
                            {
                                canGrowDepth = false;
                                break;
                            }
                        }

                        if (!canGrowDepth)
                        {
                            break;
                        }

                        for (int dx = 0; dx < width; dx++)
                        {
                            visited.Add(new RunKey(x + dx, z + depth, run.MinY, run.MaxY));
                        }

                        depth++;
                    }

                    collisionData.Boxes.Add(CreateBox(x, z, width, depth, run, scale));
                }
            }
        }

        return collisionData;
    }

    private static List<VerticalRun>[,] BuildRuns(VoxelChunkData chunk)
    {
        var result = new List<VerticalRun>[chunk.Size, chunk.Size];

        for (int z = 0; z < chunk.Size; z++)
        {
            for (int x = 0; x < chunk.Size; x++)
            {
                var runs = new List<VerticalRun>();
                int y = 0;
                while (y < chunk.Height)
                {
                    if (!IsCollisionSolid(chunk.GetBlock(x, y, z)))
                    {
                        y++;
                        continue;
                    }

                    int minY = y;
                    while (y + 1 < chunk.Height && IsCollisionSolid(chunk.GetBlock(x, y + 1, z)))
                    {
                        y++;
                    }

                    runs.Add(new VerticalRun(minY, y));
                    y++;
                }

                result[x, z] = runs;
            }
        }

        return result;
    }

    private static bool HasRun(List<VerticalRun> runs, VerticalRun target)
    {
        for (int i = 0; i < runs.Count; i++)
        {
            if (runs[i].Equals(target))
            {
                return true;
            }
        }

        return false;
    }

    private static VoxelCollisionBox CreateBox(int x, int z, int width, int depth, VerticalRun run, float scale)
    {
        Vector3 size = new(width * scale, (run.MaxY - run.MinY + 1) * scale, depth * scale);
        Vector3 center = new(
            (x + width * 0.5f) * scale,
            (run.MinY + (run.MaxY - run.MinY + 1) * 0.5f) * scale,
            (z + depth * 0.5f) * scale);
        return new VoxelCollisionBox(size, center);
    }

    private static bool IsCollisionSolid(BlockKind block)
    {
        return block is not BlockKind.Air and not BlockKind.Water;
    }

    private readonly record struct VerticalRun(int MinY, int MaxY);

    private readonly record struct RunKey(int X, int Z, int MinY, int MaxY);
}
