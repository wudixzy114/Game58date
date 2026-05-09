using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

string[] paths =
{
  @"C:\Users\17126\.nuget\packages\stride.core\4.3.0.2507\lib\net10.0\Stride.Core.dll",
  @"C:\Users\17126\.nuget\packages\stride.core.io\4.3.0.2507\lib\net10.0\Stride.Core.IO.dll",
  @"C:\Users\17126\.nuget\packages\stride.core.reflection\4.3.0.2507\lib\net10.0\Stride.Core.Reflection.dll",
  @"C:\Users\17126\.nuget\packages\stride.core.mathematics\4.3.0.2507\lib\net10.0\Stride.Core.Mathematics.dll",
  @"C:\Users\17126\.nuget\packages\stride.core.microthreading\4.3.0.2507\lib\net10.0\Stride.Core.MicroThreading.dll",
  @"C:\Users\17126\.nuget\packages\stride.graphics\4.3.0.2507\lib\net10.0\Stride.Graphics.dll",
  @"C:\Users\17126\.nuget\packages\stride.rendering\4.3.0.2507\lib\net10.0\Stride.Rendering.dll",
  @"C:\Users\17126\.nuget\packages\stride.games\4.3.0.2507\lib\net10.0\Stride.Games.dll",
  @"C:\Users\17126\.nuget\packages\stride.input\4.3.0.2507\lib\net10.0\Stride.Input.dll",
  @"C:\Users\17126\.nuget\packages\stride.engine\4.3.0.2507\lib\net10.0\Stride.Engine.dll",
  @"C:\Users\17126\.nuget\packages\stride.physics\4.3.0.2507\lib\net10.0\Stride.Physics.dll"
};
foreach (var p in paths) { try { AssemblyLoadContext.Default.LoadFromAssemblyPath(p); } catch { } }
var asm = AppDomain.CurrentDomain.GetAssemblies().First(a => a.GetName().Name == "Stride.Physics");
foreach (var t in asm.GetExportedTypes().Where(t => t.FullName!.Contains("Collider") || t.FullName!.Contains("Rigidbody") || t.FullName!.Contains("PhysicsComponent")).OrderBy(t => t.FullName))
    Console.WriteLine(t.FullName);
