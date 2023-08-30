using System.Linq;

namespace Extensions.Valheim;

public static class ZoneSystemExtension
{
    public static void SetGlobalKey(this ZoneSystem zoneSystem, string key, object value)
    {
        zoneSystem.SetGlobalKey($"{key} {value}");
    }

    public static string GetGlobalKeyValue(this ZoneSystem zoneSystem, string key)
    {
        zoneSystem.GetGlobalKey(key, out var value);
        return value;
    }

    public static string GetOrAddGlobalKey(this ZoneSystem zoneSystem, string key, string defaultValue)
    {
        if (zoneSystem.GetGlobalKey(key, out var value)) return value;

        zoneSystem.SetGlobalKey(key, defaultValue);
        return defaultValue;
    }

    public static (Vector2i, ZoneSystem.LocationInstance)[] GetGeneratedLocationsByName(this ZoneSystem zoneSystem,
        string key)
    {
        return ZoneSystem.instance.m_locationInstances
            .Where(x => x.Value.m_location.m_prefabName == key)
            .Select(x => (x.Key, x.Value))
            .ToArray();
    }
}