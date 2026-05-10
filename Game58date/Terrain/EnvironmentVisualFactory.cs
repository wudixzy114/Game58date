#nullable enable
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Engine;

namespace Game58date.Terrain;

public sealed class EnvironmentVisualFactory
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly IContentManager? content;
    private readonly EnvironmentAssetRegistry assetRegistry = new();
    private readonly Dictionary<EnvironmentMaterialKind, Material> materials = new();
    private readonly Dictionary<EnvironmentMaterialKind, Model> boxModels = new();
    private readonly Dictionary<string, Prefab?> prefabCache = new();

    public EnvironmentVisualFactory(GraphicsDevice graphicsDevice, IContentManager? content = null)
    {
        this.graphicsDevice = graphicsDevice;
        this.content = content;
    }

    public Entity CreateBoxEntity(
        string name,
        EnvironmentMaterialKind materialKind,
        Vector3 localPosition,
        Vector3 localScale,
        Vector3? localRotationEuler = null)
    {
        var entity = new Entity(name);
        entity.Transform.Position = localPosition;
        entity.Transform.Scale = localScale;
        if (localRotationEuler.HasValue)
        {
            entity.Transform.RotationEulerXYZ = localRotationEuler.Value;
        }

        entity.Add(new ModelComponent(GetBoxModel(materialKind)));
        return entity;
    }

    public Entity CreateEnvironmentEntity(
        string name,
        EnvironmentAssetDescriptor asset,
        Vector3 localPosition,
        Vector3 localScale,
        Vector3? localRotationEuler = null)
    {
        EnvironmentAssetDescriptor resolvedAsset = assetRegistry.Resolve(asset);
        if (TryCreatePrefabEntity(name, resolvedAsset, localPosition, localScale, localRotationEuler, out Entity? prefabEntity))
        {
            return prefabEntity!;
        }

        return CreateBoxEntity(name, resolvedAsset.FallbackMaterial, localPosition, localScale, localRotationEuler);
    }

    private bool TryCreatePrefabEntity(
        string name,
        EnvironmentAssetDescriptor asset,
        Vector3 localPosition,
        Vector3 localScale,
        Vector3? localRotationEuler,
        out Entity? entity)
    {
        entity = null;
        if (asset.UsesPlaceholderGeometry || content is null || string.IsNullOrWhiteSpace(asset.AssetKey))
        {
            return false;
        }

        if (!prefabCache.TryGetValue(asset.AssetKey, out Prefab? prefab))
        {
            try
            {
                prefab = content.Load<Prefab>(asset.AssetKey);
            }
            catch
            {
                prefab = null;
            }

            prefabCache[asset.AssetKey] = prefab;
        }

        if (prefab is null)
        {
            return false;
        }

        entity = new Entity(name);
        entity.Transform.Position = localPosition;
        entity.Transform.Scale = localScale;
        if (localRotationEuler.HasValue)
        {
            entity.Transform.RotationEulerXYZ = localRotationEuler.Value;
        }

        foreach (Entity child in prefab.Instantiate())
        {
            entity.AddChild(child);
        }

        return true;
    }

    public void ApplyBoxModel(Entity entity, EnvironmentMaterialKind materialKind)
    {
        ModelComponent? modelComponent = entity.Get<ModelComponent>();
        if (modelComponent is null)
        {
            entity.Add(new ModelComponent(GetBoxModel(materialKind)));
            return;
        }

        modelComponent.Model = GetBoxModel(materialKind);
    }

    private Model GetBoxModel(EnvironmentMaterialKind materialKind)
    {
        if (boxModels.TryGetValue(materialKind, out Model? cachedModel))
        {
            return cachedModel;
        }

        Material material = GetMaterial(materialKind);
        var vertices = new List<VertexPositionNormalTexture>(24);
        var indices = new List<int>(36);

        AddFace(vertices, indices, Vector3.UnitX,
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, -0.5f));
        AddFace(vertices, indices, -Vector3.UnitX,
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f));
        AddFace(vertices, indices, Vector3.UnitY,
            new Vector3(-0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f));
        AddFace(vertices, indices, -Vector3.UnitY,
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f, -0.5f));
        AddFace(vertices, indices, Vector3.UnitZ,
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.5f),
            new Vector3(0.5f, 0.5f, 0.5f));
        AddFace(vertices, indices, -Vector3.UnitZ,
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(-0.5f, 0.5f, -0.5f));

        var vertexBuffer = Buffer.New(graphicsDevice, vertices.ToArray(), BufferFlags.VertexBuffer, GraphicsResourceUsage.Immutable);
        var indexBuffer = Buffer.New(graphicsDevice, indices.ToArray(), BufferFlags.IndexBuffer, GraphicsResourceUsage.Immutable);
        var draw = new MeshDraw
        {
            PrimitiveType = PrimitiveType.TriangleList,
            DrawCount = indices.Count,
            VertexBuffers = new[]
            {
                new VertexBufferBinding(vertexBuffer, VertexPositionNormalTexture.Layout, vertices.Count),
            },
            IndexBuffer = new IndexBufferBinding(indexBuffer, true, indices.Count),
        };

        BoundingBox bounds = new(new Vector3(-0.5f), new Vector3(0.5f));
        var mesh = new Mesh(draw, new ParameterCollection())
        {
            MaterialIndex = 0,
            BoundingBox = bounds,
            BoundingSphere = BoundingSphere.FromBox(bounds),
        };

        var model = new Model();
        model.Add(mesh);
        model.Add(material);
        model.BoundingBox = bounds;
        model.BoundingSphere = BoundingSphere.FromBox(bounds);

        boxModels[materialKind] = model;
        return model;
    }

    private Material GetMaterial(EnvironmentMaterialKind materialKind)
    {
        if (materials.TryGetValue(materialKind, out Material? material))
        {
            return material;
        }

        ResolveMaterialProperties(materialKind, out Color4 diffuse, out Color4 emissive, out float glossiness, out bool doubleSided, out float alpha);

        var descriptor = new MaterialDescriptor();
        descriptor.Attributes.Diffuse = new MaterialDiffuseMapFeature(
            new ComputeColor(diffuse));
        descriptor.Attributes.DiffuseModel = new MaterialDiffuseLambertModelFeature();
        descriptor.Attributes.Emissive = new MaterialEmissiveMapFeature(
            new ComputeColor(emissive))
        {
            Intensity = new ComputeFloat(1f),
        };
        descriptor.Attributes.MicroSurface = new MaterialGlossinessMapFeature(
            new ComputeFloat(glossiness));
        descriptor.Attributes.Specular = new MaterialMetalnessMapFeature(
            new ComputeFloat(0.02f));
        descriptor.Attributes.SpecularModel = new MaterialSpecularMicrofacetModelFeature();
        descriptor.Attributes.CullMode = doubleSided ? CullMode.None : CullMode.Back;

        if (alpha < 0.999f)
        {
            descriptor.Attributes.Transparency = new MaterialTransparencyBlendFeature
            {
                Alpha = new ComputeFloat(alpha),
                Tint = new ComputeColor(Color4.White),
            };
        }

        material = Material.New(graphicsDevice, descriptor);
        materials[materialKind] = material;
        return material;
    }

    private static void AddFace(
        List<VertexPositionNormalTexture> vertices,
        List<int> indices,
        Vector3 normal,
        Vector3 p0,
        Vector3 p1,
        Vector3 p2,
        Vector3 p3)
    {
        int baseVertex = vertices.Count;
        vertices.Add(new VertexPositionNormalTexture(p0, normal, new Vector2(0f, 1f)));
        vertices.Add(new VertexPositionNormalTexture(p1, normal, new Vector2(0f, 0f)));
        vertices.Add(new VertexPositionNormalTexture(p2, normal, new Vector2(1f, 0f)));
        vertices.Add(new VertexPositionNormalTexture(p3, normal, new Vector2(1f, 1f)));

        indices.Add(baseVertex + 0);
        indices.Add(baseVertex + 1);
        indices.Add(baseVertex + 2);
        indices.Add(baseVertex + 0);
        indices.Add(baseVertex + 2);
        indices.Add(baseVertex + 3);
    }

    private static void ResolveMaterialProperties(
        EnvironmentMaterialKind materialKind,
        out Color4 diffuse,
        out Color4 emissive,
        out float glossiness,
        out bool doubleSided,
        out float alpha)
    {
        switch (materialKind)
        {
            case EnvironmentMaterialKind.Bark:
                diffuse = new Color4(0.39f, 0.26f, 0.16f, 1f);
                emissive = new Color4(0.03f, 0.02f, 0.01f, 1f);
                glossiness = 0.18f;
                doubleSided = false;
                alpha = 1f;
                return;

            case EnvironmentMaterialKind.Leaf:
                diffuse = new Color4(0.28f, 0.51f, 0.20f, 1f);
                emissive = new Color4(0.04f, 0.08f, 0.03f, 1f);
                glossiness = 0.24f;
                doubleSided = false;
                alpha = 1f;
                return;

            case EnvironmentMaterialKind.Needle:
                diffuse = new Color4(0.20f, 0.36f, 0.19f, 1f);
                emissive = new Color4(0.03f, 0.06f, 0.03f, 1f);
                glossiness = 0.20f;
                doubleSided = false;
                alpha = 1f;
                return;

            case EnvironmentMaterialKind.Reed:
                diffuse = new Color4(0.48f, 0.57f, 0.26f, 1f);
                emissive = new Color4(0.05f, 0.07f, 0.03f, 1f);
                glossiness = 0.16f;
                doubleSided = false;
                alpha = 1f;
                return;

            case EnvironmentMaterialKind.Stone:
                diffuse = new Color4(0.47f, 0.49f, 0.47f, 1f);
                emissive = new Color4(0.03f, 0.03f, 0.03f, 1f);
                glossiness = 0.14f;
                doubleSided = false;
                alpha = 1f;
                return;

            case EnvironmentMaterialKind.RuinStone:
                diffuse = new Color4(0.57f, 0.53f, 0.44f, 1f);
                emissive = new Color4(0.03f, 0.03f, 0.02f, 1f);
                glossiness = 0.12f;
                doubleSided = false;
                alpha = 1f;
                return;

            case EnvironmentMaterialKind.Driftwood:
                diffuse = new Color4(0.63f, 0.53f, 0.35f, 1f);
                emissive = new Color4(0.03f, 0.02f, 0.02f, 1f);
                glossiness = 0.10f;
                doubleSided = false;
                alpha = 1f;
                return;

            case EnvironmentMaterialKind.Deer:
                diffuse = new Color4(0.46f, 0.28f, 0.17f, 1f);
                emissive = new Color4(0.03f, 0.02f, 0.01f, 1f);
                glossiness = 0.18f;
                doubleSided = false;
                alpha = 1f;
                return;

            case EnvironmentMaterialKind.Goat:
                diffuse = new Color4(0.58f, 0.58f, 0.58f, 1f);
                emissive = new Color4(0.03f, 0.03f, 0.03f, 1f);
                glossiness = 0.18f;
                doubleSided = false;
                alpha = 1f;
                return;

            case EnvironmentMaterialKind.Gull:
                diffuse = new Color4(0.86f, 0.88f, 0.90f, 1f);
                emissive = new Color4(0.05f, 0.05f, 0.06f, 1f);
                glossiness = 0.26f;
                doubleSided = false;
                alpha = 1f;
                return;

            case EnvironmentMaterialKind.Rain:
                diffuse = new Color4(0.63f, 0.78f, 0.92f, 1f);
                emissive = new Color4(0.08f, 0.12f, 0.16f, 1f);
                glossiness = 0.78f;
                doubleSided = true;
                alpha = 0.35f;
                return;

            case EnvironmentMaterialKind.Snow:
                diffuse = new Color4(0.95f, 0.97f, 1.00f, 1f);
                emissive = new Color4(0.12f, 0.12f, 0.14f, 1f);
                glossiness = 0.62f;
                doubleSided = true;
                alpha = 0.58f;
                return;

            case EnvironmentMaterialKind.Fog:
                diffuse = new Color4(0.76f, 0.80f, 0.82f, 1f);
                emissive = new Color4(0.08f, 0.09f, 0.10f, 1f);
                glossiness = 0.30f;
                doubleSided = true;
                alpha = 0.18f;
                return;

            default:
                diffuse = new Color4(0.84f, 0.74f, 0.58f, 1f);
                emissive = new Color4(0.06f, 0.05f, 0.03f, 1f);
                glossiness = 0.12f;
                doubleSided = true;
                alpha = 0.32f;
                return;
        }
    }
}
