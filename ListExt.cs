using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;

namespace NPCsSystem;

public static class ListExt
{
    public static T Random<T>(this List<T> list)
    {
        if (list == null || list.Count == 0) return default(T);
        return list[UnityEngine.Random.Range(0, list.Count)];
    }

    public static T Random<T>(this T[] array)
    {
        if (array == null || array.Length == 0) return default(T);
        return array[UnityEngine.Random.Range(0, array.Length)];
    }
}