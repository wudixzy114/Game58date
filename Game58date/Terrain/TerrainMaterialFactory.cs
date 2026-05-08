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
    private Material? material;

    public TerrainMaterialFactory(GraphicsDevice graphicsDevice)
    {
        this.graphicsDevice = graphicsDevice;
    }

    public Material GetOrCreate()
    {
        material ??= BuildTerrainMaterial();
        return material;
    }

    private Material BuildTerrainMaterial()
    {
        var descriptor = new MaterialDescriptor();
        descriptor.Attributes.Diffuse = new MaterialDiffuseMapFeature(
            new ComputeColor(new Color4(0.58f, 0.53f, 0.47f, 1f)));
        descriptor.Attributes.DiffuseModel = new MaterialDiffuseLambertModelFeature();
        descriptor.Attributes.MicroSurface = new MaterialGlossinessMapFeature(
            new ComputeFloat(0.18f));
        descriptor.Attributes.Specular = new MaterialMetalnessMapFeature(
            new ComputeFloat(0.02f));
        descriptor.Attributes.SpecularModel = new MaterialSpecularMicrofacetModelFeature();

        return Material.New(graphicsDevice, descriptor);
    }
}
