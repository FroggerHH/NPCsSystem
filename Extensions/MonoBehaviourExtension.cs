using UnityEngine;

namespace Extensions;

public static class MonoBehaviourExtension
{
    public static T SetActiveGO<T>(this T behaviour, bool flag) where T : Component
    {
        behaviour.gameObject.SetActive(flag);
        return behaviour;
    }
}