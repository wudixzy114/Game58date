using System.Text;
using System.Text.RegularExpressions;

string repoRoot = ResolveRepoRoot(args);
string assetsRoot = Path.Combine(repoRoot, "Game58date", "Assets");
string modelsRoot = Path.Combine(assetsRoot, "Env", "Models");
string prefabsRoot = Path.Combine(assetsRoot, "Env", "Prefabs");

Directory.CreateDirectory(prefabsRoot);

var modelIds = LoadAssetIds(modelsRoot, ".sdpromodel");

var definitions = new[]
{
    new PrefabDefinition("BroadleafTree", [
        Part("Trunk", "Cylinder_Bark", 0f, 1.25f, 0f, 0.42f, 2.5f, 0.42f),
        Part("CanopyA", "Sphere_Leaf", 0f, 3.25f, 0f, 1.85f, 1.45f, 1.85f),
        Part("CanopyB", "Sphere_Leaf", 0.35f, 4.15f, -0.15f, 1.15f, 0.95f, 1.15f),
    ]),
    new PrefabDefinition("PineTree", [
        Part("Trunk", "Cylinder_Bark", 0f, 1.5f, 0f, 0.35f, 3.0f, 0.35f),
        Part("NeedlesA", "Cone_Needle", 0f, 2.25f, 0f, 1.30f, 1.00f, 1.30f),
        Part("NeedlesB", "Cone_Needle", 0f, 3.35f, 0f, 1.00f, 0.95f, 1.00f),
        Part("NeedlesC", "Cone_Needle", 0f, 4.20f, 0f, 0.66f, 0.78f, 0.66f),
    ]),
    new PrefabDefinition("WetlandTree", [
        Part("Trunk", "Cylinder_Bark", 0f, 1.05f, 0f, 0.30f, 2.10f, 0.30f),
        Part("CanopyA", "Sphere_Leaf", 0f, 2.75f, 0f, 1.40f, 1.10f, 1.40f),
        Part("CanopyB", "Sphere_Leaf", 0.28f, 3.35f, 0.12f, 0.88f, 0.72f, 0.88f),
    ]),
    new PrefabDefinition("Bush", [
        Part("BushA", "Sphere_Leaf", 0f, 0.55f, 0f, 1.05f, 0.70f, 1.05f),
        Part("BushB", "Sphere_Leaf", 0.35f, 0.78f, -0.15f, 0.70f, 0.48f, 0.70f),
    ]),
    new PrefabDefinition("ReedPatch", [
        Part("ReedA", "Cylinder_Reed", -0.20f, 0.65f, 0.05f, 0.08f, 1.30f, 0.08f),
        Part("ReedB", "Cylinder_Reed", 0.15f, 0.52f, -0.15f, 0.08f, 1.05f, 0.08f),
        Part("ReedC", "Cylinder_Reed", 0.05f, 0.75f, 0.20f, 0.08f, 1.50f, 0.08f),
        Part("ReedD", "Cylinder_Reed", -0.10f, 0.42f, -0.20f, 0.08f, 0.84f, 0.08f),
    ]),
    new PrefabDefinition("RockCluster", [
        Part("RockA", "Sphere_Stone", -0.25f, 0.28f, 0.12f, 0.75f, 0.56f, 0.55f),
        Part("RockB", "Sphere_Stone", 0.18f, 0.20f, -0.12f, 0.56f, 0.40f, 0.46f),
        Part("RockC", "Cube_Stone", 0.02f, 0.42f, 0.25f, 0.40f, 0.30f, 0.32f),
    ]),
    new PrefabDefinition("Cairn", [
        Part("Base", "Cube_RuinStone", 0f, 0.24f, 0f, 1.10f, 0.48f, 1.10f),
        Part("Mid", "Sphere_Stone", 0.08f, 0.74f, -0.05f, 0.74f, 0.36f, 0.74f),
        Part("Top", "Sphere_Stone", -0.04f, 1.15f, 0.08f, 0.44f, 0.26f, 0.44f),
    ]),
    new PrefabDefinition("RuinArch", [
        Part("PillarA", "Cube_RuinStone", -0.70f, 1.25f, 0f, 0.42f, 2.50f, 0.42f),
        Part("PillarB", "Cube_RuinStone", 0.70f, 1.12f, 0f, 0.42f, 2.24f, 0.42f),
        Part("Lintel", "Cube_RuinStone", 0f, 2.42f, 0f, 1.72f, 0.34f, 0.52f),
        Part("BrokenStone", "Cube_Stone", 1.22f, 0.34f, -0.18f, 0.42f, 0.32f, 0.46f),
    ]),
    new PrefabDefinition("Deer", [
        Part("Body", "Capsule_Deer", 0f, 1.00f, 0f, 1.20f, 1.20f, 0.85f),
        Part("Head", "Cube_Deer", 0.68f, 1.44f, 0f, 0.46f, 0.28f, 0.24f),
    ]),
    new PrefabDefinition("Goat", [
        Part("Body", "Capsule_Goat", 0f, 0.88f, 0f, 0.95f, 1.00f, 0.75f),
        Part("Head", "Cube_Goat", 0.52f, 1.08f, 0f, 0.34f, 0.26f, 0.22f),
    ]),
    new PrefabDefinition("Gull", [
        Part("Body", "Capsule_Gull", 0f, 0.20f, 0f, 0.42f, 0.30f, 0.22f),
        Part("WingL", "Cube_Gull", 0f, 0.18f, -0.22f, 0.82f, 0.06f, 0.12f),
        Part("WingR", "Cube_Gull", 0f, 0.18f, 0.22f, 0.82f, 0.06f, 0.12f),
    ]),
};

foreach (PrefabDefinition definition in definitions)
{
    string yaml = BuildPrefabYaml(definition, modelIds);
    string path = Path.Combine(prefabsRoot, $"{definition.Name}.sdprefab");
    File.WriteAllText(path, yaml, new UTF8Encoding(false));
    Console.WriteLine($"Generated {definition.Name}.sdprefab");
}

return;

static PrefabPart Part(string name, string modelName, float x, float y, float z, float sx, float sy, float sz)
    => new(name, modelName, x, y, z, sx, sy, sz);

static string ResolveRepoRoot(string[] args)
{
    if (args.Length > 0 && Directory.Exists(args[0]))
    {
        return Path.GetFullPath(args[0]);
    }

    string current = AppContext.BaseDirectory;
    while (!string.IsNullOrWhiteSpace(current))
    {
        if (Directory.Exists(Path.Combine(current, "Game58date")) &&
            File.Exists(Path.Combine(current, "Game58date.sln")))
        {
            return current;
        }

        DirectoryInfo? parent = Directory.GetParent(current);
        if (parent is null)
        {
            break;
        }

        current = parent.FullName;
    }

    throw new InvalidOperationException("Could not locate repository root.");
}

static Dictionary<string, string> LoadAssetIds(string directory, string extension)
{
    var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    Regex idRegex = new(@"^Id:\s*(?<id>[0-9a-fA-F\-]+)\s*$", RegexOptions.Multiline);
    foreach (string path in Directory.GetFiles(directory, $"*{extension}", SearchOption.TopDirectoryOnly))
    {
        string yaml = File.ReadAllText(path);
        Match match = idRegex.Match(yaml);
        if (!match.Success)
        {
            throw new InvalidOperationException($"Missing asset Id in {path}");
        }

        result[Path.GetFileNameWithoutExtension(path)] = match.Groups["id"].Value;
    }

    return result;
}

static string BuildPrefabYaml(PrefabDefinition definition, IReadOnlyDictionary<string, string> modelIds)
{
    var sb = new StringBuilder();
    sb.AppendLine("!PrefabAsset");
    sb.AppendLine($"Id: {Guid.NewGuid()}");
    sb.AppendLine("SerializedVersion: {Stride: 3.1.0.1}");
    sb.AppendLine("Tags: []");
    sb.AppendLine("Hierarchy:");
    sb.AppendLine("    RootParts:");

    var parts = definition.Parts.Select(CreateEntity).ToList();
    foreach (PrefabEntity part in parts)
    {
        sb.AppendLine($"        - ref!! {part.EntityId}");
    }

    sb.AppendLine("    Parts:");
    foreach (PrefabEntity part in parts)
    {
        string modelId = modelIds[part.ModelName];
        sb.AppendLine("        -   Entity:");
        sb.AppendLine($"                Id: {part.EntityId}");
        sb.AppendLine($"                Name: {part.Name}");
        sb.AppendLine("                Components:");
        sb.AppendLine($"                    {part.TransformKey}: !TransformComponent");
        sb.AppendLine($"                        Id: {part.TransformId}");
        sb.AppendLine($"                        Position: {{X: {Format(part.X)}, Y: {Format(part.Y)}, Z: {Format(part.Z)}}}");
        sb.AppendLine("                        Rotation: {X: 0.0, Y: 0.0, Z: 0.0, W: 1.0}");
        sb.AppendLine($"                        Scale: {{X: {Format(part.ScaleX)}, Y: {Format(part.ScaleY)}, Z: {Format(part.ScaleZ)}}}");
        sb.AppendLine("                        Children: {}");
        sb.AppendLine($"                    {part.ModelKey}: !ModelComponent");
        sb.AppendLine($"                        Id: {part.ModelComponentId}");
        sb.AppendLine($"                        Model: {modelId}:Env/Models/{part.ModelName}");
        sb.AppendLine("                        Materials: {}");
    }

    return sb.ToString();
}

static PrefabEntity CreateEntity(PrefabPart part)
{
    return new PrefabEntity(
        part.Name,
        part.ModelName,
        Guid.NewGuid().ToString(),
        Guid.NewGuid().ToString("N"),
        Guid.NewGuid().ToString(),
        Guid.NewGuid().ToString("N"),
        Guid.NewGuid().ToString(),
        part.X,
        part.Y,
        part.Z,
        part.ScaleX,
        part.ScaleY,
        part.ScaleZ);
}

static string Format(float value)
{
    return value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
}

internal readonly record struct PrefabDefinition(string Name, IReadOnlyList<PrefabPart> Parts);

internal readonly record struct PrefabPart(
    string Name,
    string ModelName,
    float X,
    float Y,
    float Z,
    float ScaleX,
    float ScaleY,
    float ScaleZ);

internal readonly record struct PrefabEntity(
    string Name,
    string ModelName,
    string EntityId,
    string TransformKey,
    string TransformId,
    string ModelKey,
    string ModelComponentId,
    float X,
    float Y,
    float Z,
    float ScaleX,
    float ScaleY,
    float ScaleZ);
