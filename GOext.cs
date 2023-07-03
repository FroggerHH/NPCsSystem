using UnityEngine;

namespace NPCsSystem;

public static class GOext
{
    public static string GetPrefabName(this GameObject gameObject)
    {
        return Utils.GetPrefabName(gameObject);
    }
}