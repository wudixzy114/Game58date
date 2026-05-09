#nullable enable
using Stride.Core.Serialization.Contents;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Engine;

namespace Game58date.Terrain;

public sealed class VoxelChunkModelFactory
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly TerrainMaterialFactory materialFactory;

    public VoxelChunkModelFactory(GraphicsDevice graphicsDevice, IContentManager content)
    {
        this.graphicsDevice = graphicsDevice;
        materialFactory = new TerrainMaterialFactory(graphicsDevice);
    }

    public void AttachModels(Entity rootEntity, VoxelChunkMeshData meshData)
    {
        if (!meshData.Solid.IsEmpty)
        {
            rootEntity.Add(new ModelComponent(CreateModel(meshData.Solid, materialFactory.GetOrCreateTerrainMaterial())));
        }

        if (!meshData.Water.IsEmpty)
        {
            var waterEntity = new Entity("WaterSurface");
            waterEntity.Transform.Position = new Vector3(0f, 0.03f, 0f);
            waterEntity.Add(new ModelComponent(CreateModel(meshData.Water, materialFactory.GetOrCreateWaterMaterial())));
            rootEntity.AddChild(waterEntity);
        }
    }

    private Model CreateModel(VoxelSurfaceMeshData meshData, Material material)
    {
        var vertexBuffer = Buffer.New(graphicsDevice, meshData.Vertices.ToArray(), BufferFlags.VertexBuffer, GraphicsResourceUsage.Immutable);
        var indexBuffer = Buffer.New(graphicsDevice, meshData.Indices.ToArray(), BufferFlags.IndexBuffer, GraphicsResourceUsage.Immutable);

        var draw = new MeshDraw
        {
            PrimitiveType = PrimitiveType.TriangleList,
            DrawCount = meshData.Indices.Count,
            StartLocation = 0,
            VertexBuffers = new[]
            {
                new VertexBufferBinding(vertexBuffer, VertexPositionNormalTexture.Layout, meshData.Vertices.Count),
            },
            IndexBuffer = new IndexBufferBinding(indexBuffer, true, meshData.Indices.Count),
        };

        var mesh = new Mesh(draw, new ParameterCollection())
        {
            MaterialIndex = 0,
            BoundingBox = meshData.BoundingBox,
            BoundingSphere = meshData.BoundingSphere,
        };

        var model = new Model();
        model.Add(mesh);
        model.Add(material);
        model.BoundingBox = meshData.BoundingBox;
        model.BoundingSphere = meshData.BoundingSphere;
        return model;
    }
}
