#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Game58date.Terrain;

public sealed class VoxelChunkBuildPipeline
{
    private readonly TerrainChunkGenerator generator;
    private readonly VoxelChunkMesher mesher;
    private readonly int maxConcurrentBuilds;
    private readonly Queue<ChunkBuildRequest> queuedRequests = new();
    private readonly Queue<ChunkBuildResult> readyResults = new();
    private readonly Dictionary<VoxelChunkCoordinate, int> latestRequestedRevision = new();
    private readonly Dictionary<VoxelChunkCoordinate, int> queuedRevisions = new();
    private readonly Dictionary<VoxelChunkCoordinate, Task<ChunkBuildResult>> runningBuilds = new();
    private readonly Dictionary<VoxelChunkCoordinate, int> readyRevisions = new();

    public VoxelChunkBuildPipeline(TerrainChunkGenerator generator, VoxelChunkMesher mesher, int maxConcurrentBuilds)
    {
        this.generator = generator;
        this.mesher = mesher;
        this.maxConcurrentBuilds = Math.Max(1, maxConcurrentBuilds);
    }

    public int QueuedCount => queuedRequests.Count;

    public int RunningCount => runningBuilds.Count;

    public string LastErrorMessage { get; private set; } = "None";

    public bool HasOutstandingRequest(VoxelChunkCoordinate coordinate)
    {
        return queuedRevisions.ContainsKey(coordinate)
            || runningBuilds.ContainsKey(coordinate)
            || readyRevisions.ContainsKey(coordinate);
    }

    public void Enqueue(VoxelChunkCoordinate coordinate, int revision)
    {
        latestRequestedRevision[coordinate] = revision;

        if (queuedRevisions.TryGetValue(coordinate, out int queuedRevision) && queuedRevision >= revision)
        {
            return;
        }

        if (readyRevisions.TryGetValue(coordinate, out int readyRevision) && readyRevision >= revision)
        {
            return;
        }

        queuedRequests.Enqueue(new ChunkBuildRequest(coordinate, revision));
        queuedRevisions[coordinate] = revision;
    }

    public void Pump()
    {
        CollectFinishedBuilds();

        while (runningBuilds.Count < maxConcurrentBuilds && queuedRequests.Count > 0)
        {
            ChunkBuildRequest request = queuedRequests.Dequeue();
            queuedRevisions.Remove(request.Coordinate);

            if (!latestRequestedRevision.TryGetValue(request.Coordinate, out int latestRevision) || latestRevision != request.Revision)
            {
                continue;
            }

            runningBuilds[request.Coordinate] = Task.Run(() => BuildChunk(request));
        }
    }

    public List<ChunkBuildResult> DequeueReadyResults(int maxResults)
    {
        var results = new List<ChunkBuildResult>(Math.Max(0, maxResults));
        int limit = Math.Max(0, maxResults);

        while (results.Count < limit && readyResults.Count > 0)
        {
            ChunkBuildResult result = readyResults.Dequeue();
            readyRevisions.Remove(result.Coordinate);
            results.Add(result);
        }

        return results;
    }

    private void CollectFinishedBuilds()
    {
        var finishedCoordinates = new List<VoxelChunkCoordinate>();
        foreach ((VoxelChunkCoordinate coordinate, Task<ChunkBuildResult> task) in runningBuilds)
        {
            if (task.IsCompleted)
            {
                finishedCoordinates.Add(coordinate);
            }
        }

        foreach (VoxelChunkCoordinate coordinate in finishedCoordinates)
        {
            Task<ChunkBuildResult> task = runningBuilds[coordinate];
            runningBuilds.Remove(coordinate);

            try
            {
                ChunkBuildResult result = task.GetAwaiter().GetResult();
                if (!latestRequestedRevision.TryGetValue(result.Coordinate, out int latestRevision) || latestRevision != result.Revision)
                {
                    continue;
                }

                readyResults.Enqueue(result);
                readyRevisions[result.Coordinate] = result.Revision;
            }
            catch (Exception exception)
            {
                LastErrorMessage = exception.GetBaseException().Message;
            }
        }
    }

    private ChunkBuildResult BuildChunk(ChunkBuildRequest request)
    {
        VoxelChunkData data = generator.Generate(request.Coordinate);
        VoxelChunkMeshData meshData = mesher.Build(data);
        return new ChunkBuildResult(request.Revision, request.Coordinate, data, meshData);
    }
}
