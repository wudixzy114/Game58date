#nullable enable
using System.Collections.Generic;

namespace Game58date.Terrain;

public sealed class WeatherEffectLibrary
{
    private readonly Dictionary<WeatherKind, WeatherEffectDescriptor> descriptors = new()
    {
        [WeatherKind.Clear] = new WeatherEffectDescriptor(WeatherKind.Clear, "weather/clear", EnvironmentMaterialKind.Dust, true),
        [WeatherKind.Wind] = new WeatherEffectDescriptor(WeatherKind.Wind, "weather/wind", EnvironmentMaterialKind.Dust, true),
        [WeatherKind.Rain] = new WeatherEffectDescriptor(WeatherKind.Rain, "weather/rain", EnvironmentMaterialKind.Rain, true),
        [WeatherKind.Fog] = new WeatherEffectDescriptor(WeatherKind.Fog, "weather/fog", EnvironmentMaterialKind.Fog, true),
        [WeatherKind.Snow] = new WeatherEffectDescriptor(WeatherKind.Snow, "weather/snow", EnvironmentMaterialKind.Snow, true),
    };

    public WeatherEffectDescriptor Get(WeatherKind weatherKind)
    {
        return descriptors.TryGetValue(weatherKind, out WeatherEffectDescriptor descriptor)
            ? descriptor
            : descriptors[WeatherKind.Clear];
    }
}
