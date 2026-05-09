using System;
using Stride.Physics;
Console.WriteLine("CollisionFilterGroups:");
foreach (var v in Enum.GetValues<CollisionFilterGroups>()) Console.WriteLine($"{v}={(int)v}");
Console.WriteLine("CollisionFilterGroupFlags:");
foreach (var v in Enum.GetValues<CollisionFilterGroupFlags>()) Console.WriteLine($"{v}={(int)v}");
