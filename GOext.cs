using UnityEngine;

namespace NPCsSystem;

public static class GOext
{
    public static string GetPrefabName(this GameObject gameObject)
    {
        return Utils.GetPrefabName(gameObject);
    }
    
    public static string GetPrefabName<T>(this T gameObject) where T : MonoBehaviour
    {
        return Utils.GetPrefabName((gameObject as MonoBehaviour).gameObject);
    }
}