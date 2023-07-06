using UnityEngine;

namespace NPCsSystem;

public static class GOext
{
    public static string GetPrefabName(this GameObject gameObject)
    {
        var prefabName = Utils.GetPrefabName(gameObject);
        for (int i = 0; i < 80; i++)
        {
            prefabName = prefabName.Replace($" ({i})", "");
        }

        return prefabName;
    }

    public static string GetPrefabName<T>(this T gameObject) where T : MonoBehaviour
    {
        var prefabName = Utils.GetPrefabName((gameObject as MonoBehaviour).gameObject);
        for (int i = 0; i < 80; i++)
        {
            prefabName = prefabName.Replace($" ({i})", "");
        }

        return prefabName;
    }
}