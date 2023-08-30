using UnityEngine;

namespace Extensions;

public static class GameObjectExtension
{
    public static string GetPrefabName(this GameObject gameObject)
    {
        var prefabName = Utils.GetPrefabName(gameObject);
        for (var i = 0; i < 80; i++) prefabName = prefabName.Replace($" ({i})", "");

        return prefabName;
    }

    public static string GetPrefabName<T>(this T gameObject) where T : MonoBehaviour
    {
        var prefabName = Utils.GetPrefabName(gameObject.gameObject);
        for (var i = 0; i < 80; i++) prefabName = prefabName.Replace($" ({i})", "");

        return prefabName;
    }
}