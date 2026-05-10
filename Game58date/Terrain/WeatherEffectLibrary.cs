#nullable enable
using System.Collections.Generic;

namespace Game58date.Terrain;

public sealed class WeatherEffectLibrary
{
    private readonly Dictionary<WeatherKind, WeatherEffectDescriptor> descriptors = new()
    {
        [WeatherKind.Clear] = new WeatherEffectDescriptor(WeatherKind.Clear, "weather/clear", EnvironmentMaterialKind.Dust, true),
        [WeatherKind.Wind] = new WeatherEffectDescriptor(WeatherKind.Wind, "Weather/Wind", EnvironmentMaterialKind.Dust, false),
        [WeatherKind.Rain] = new WeatherEffectDescriptor(WeatherKind.Rain, "Weather/Rain", EnvironmentMaterialKind.Rain, false),
        [WeatherKind.Fog] = new WeatherEffectDescriptor(WeatherKind.Fog, "Weather/Fog", EnvironmentMaterialKind.Fog, false),
        [WeatherKind.Snow] = new WeatherEffectDescriptor(WeatherKind.Snow, "Weather/Snow", EnvironmentMaterialKind.Snow, false),
    };

    public WeatherEffectDescriptor Get(WeatherKind weatherKind)
    {
        return descriptors.TryGetValue(weatherKind, out WeatherEffectDescriptor descriptor)
            ? descriptor
            : descriptors[WeatherKind.Clear];
    }
}
