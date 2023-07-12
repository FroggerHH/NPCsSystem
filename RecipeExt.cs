using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using System.Threading;

namespace NPCsSystem;

public static class RecipeExt
{
    public static List<(ItemDrop.ItemData.SharedData, int)> ToList(this Recipe recipe)
    {
        var result = new List<(ItemDrop.ItemData.SharedData, int)>();
        foreach (var resource in recipe.m_resources)
        {
            result.Add((resource.m_resItem.m_itemData.m_shared, resource.m_amount));
        }

        return result;
    }

    public static List<(string, int)> ToListStr(this Recipe recipe)
    {
        var result = new List<(string, int)>();
        foreach (var resource in recipe.m_resources)
        {
            result.Add((resource.m_resItem.name, resource.m_amount));
        }

        return result;
    }
}