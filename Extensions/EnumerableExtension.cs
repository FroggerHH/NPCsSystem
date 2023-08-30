using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Extensions;

public static class EnumerableExtension
{
    public static T Random<T>(this List<T> list)
    {
        if (list == null || list.Count == 0) return default;
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    public static T Random<T>(this T[] array)
    {
        if (array == null || array.Length == 0) return default;
        return array[UnityEngine.Random.Range(0, array.Length)];
    }

    public static List<Vector3> RoundCords(this List<Vector3> list)
    {
        var result = new List<Vector3>();
        for (var i = 0; i < list.Count; i++) result.Add(new Vector3((int)list[i].x, (int)list[i].y, (int)list[i].z));

        return result;
    }

    public static List<Vector2> RoundCords(this List<Vector2> list)
    {
        var result = new List<Vector2>();
        for (var i = 0; i < list.Count; i++) result.Add(new Vector2((int)list[i].x, (int)list[i].y));

        return result;
    }

    public static string GetString<T>(this IEnumerable<T> list, string separator = ", ")
    {
        return string.Join(separator, list);
    }

    public static void TryAdd<T>(this List<T> sequence, T item)
    {
        if (!sequence.Contains(item)) sequence.Add(item);
    }

    public static T Next<T>(this List<T> list, T current)
    {
        if (list == null || list.Count == 0) return default;

        var last = list[list.Count - 1];
        if (current.Equals(last)) return list[0];
        if (!list.Contains(current))
        {
            Debug.LogWarning($"[{list}.Next] list not contains this element {current}");
            return list[0];
        }

        return list[list.IndexOf(current) + 1];
    }


    public static T Nearest<T>(this IEnumerable<T> list, Vector3 nearestTo) where T : Component
    {
        var current = default(T);

        float oldDistance = int.MaxValue;
        if (list == null || list.Count() == 0) return current;
        foreach (var pos_ in list)
        {
            var pos = pos_.transform.position;
            var dist = Utils.DistanceXZ(nearestTo, pos);
            if (dist < oldDistance)
            {
                current = pos_;
                oldDistance = dist;
            }
        }

        return current;
    }
}