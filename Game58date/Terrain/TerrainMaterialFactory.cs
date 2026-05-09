#nullable enable
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Game58date.Terrain;

public sealed class TerrainMaterialFactory
{
    private readonly GraphicsDevice graphicsDevice;
    private readonly TerrainTextureAtlasFactory atlasFactory;
    private Material? terrainMaterial;
    private Material? waterMaterial;

    public TerrainMaterialFactory(GraphicsDevice graphicsDevice)
    {
        this.graphicsDevice = graphicsDevice;
        atlasFactory = new TerrainTextureAtlasFactory(graphicsDevice);
    }

    public Material GetOrCreateTerrainMaterial()
    {
        terrainMaterial ??= BuildTerrainMaterial();
        return terrainMaterial;
    }

    public Material GetOrCreateWaterMaterial()
    {
        waterMaterial ??= BuildWaterMaterial();
        return waterMaterial;
    }

    private Material BuildTerrainMaterial()
    {
        var descriptor = new MaterialDescriptor();
        descriptor.Attributes.Diffuse = new MaterialDiffuseMapFeature(
            new ComputeTextureColor(atlasFactory.GetOrCreate()));
        descriptor.Attributes.DiffuseModel = new MaterialDiffuseLambertModelFeature();
        descriptor.Attributes.Emissive = new MaterialEmissiveMapFeature(
            new ComputeColor(new Color4(0.05f, 0.045f, 0.04f, 1f)))
        {
            Intensity = new ComputeFloat(1.0f),
        };
        descriptor.Attributes.MicroSurface = new MaterialGlossinessMapFeature(
            new ComputeFloat(0.18f));
        descriptor.Attributes.Specular = new MaterialMetalnessMapFeature(
            new ComputeFloat(0.02f));
        descriptor.Attributes.SpecularModel = new MaterialSpecularMicrofacetModelFeature();
        descriptor.Attributes.CullMode = CullMode.Back;
        return Material.New(graphicsDevice, descriptor);
    }

    private Material BuildWaterMaterial()
    {
        var descriptor = new MaterialDescriptor();
        descriptor.Attributes.Diffuse = new MaterialDiffuseMapFeature(
            new ComputeColor(new Color4(0.17f, 0.39f, 0.58f, 1f)));
        descriptor.Attributes.DiffuseModel = new MaterialDiffuseLambertModelFeature();
        descriptor.Attributes.Emissive = new MaterialEmissiveMapFeature(
            new ComputeColor(new Color4(0.05f, 0.12f, 0.18f, 1f)))
        {
            Intensity = new ComputeFloat(1.0f),
        };
        descriptor.Attributes.MicroSurface = new MaterialGlossinessMapFeature(
            new ComputeFloat(0.85f));
        descriptor.Attributes.Specular = new MaterialMetalnessMapFeature(
            new ComputeFloat(0.0f));
        descriptor.Attributes.SpecularModel = new MaterialSpecularMicrofacetModelFeature();
        descriptor.Attributes.Transparency = new MaterialTransparencyBlendFeature
        {
            Alpha = new ComputeFloat(0.62f),
            Tint = new ComputeColor(new Color4(0.78f, 0.88f, 0.98f, 1f)),
        };
        descriptor.Attributes.CullMode = CullMode.None;
        return Material.New(graphicsDevice, descriptor);
    }
}
