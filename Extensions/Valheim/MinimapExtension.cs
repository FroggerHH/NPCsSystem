namespace Extensions.Valheim;

public static class MinimapExtension
{
    public static void ForceUpdateLocationPins(this Minimap minimap)
    {
        minimap.m_updateLocationsTimer = -1;
        minimap.UpdateLocationPins(0);
    }
}