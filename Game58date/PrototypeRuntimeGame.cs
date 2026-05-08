#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Stride.Engine;
using Game58date.Terrain;

namespace Game58date;

public sealed class PrototypeRuntimeGame : Game
{
    protected override async Task LoadContent()
    {
        await base.LoadContent();

        var scene = SceneSystem.SceneInstance.RootScene;
        if (scene is null)
        {
            scene = new Scene();
            SceneSystem.SceneInstance.RootScene = scene;
        }

        var anchor = scene.Entities.FirstOrDefault(entity => entity.Name == "PrototypeRuntime");
        if (anchor is null)
        {
            anchor = new Entity("PrototypeRuntime");
            anchor.Add(new VoxelTerrainRuntimeScript());
            scene.Entities.Add(anchor);
        }
    }
}
