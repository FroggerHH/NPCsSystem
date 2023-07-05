using System.Collections.Generic;
using UnityEngine;

namespace NPCsSystem;

public static class Helper
{
    public static T Nearest<T>(List<T> all, Vector3 to) where T : MonoBehaviour
    {
        T current = default(T);
        float oldDistance = int.MaxValue;
        if (all == null || all.Count == 0) return current;
        foreach (T pos_ in all)
        {
            var pos = (pos_ as MonoBehaviour).transform.position;
            float dist = Utils.DistanceXZ(to, pos);
            if (dist < oldDistance)
            {
                current = pos_;
                oldDistance = dist;
            }
        }

        return current;
    }
}