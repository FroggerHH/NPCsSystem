using System.Collections.Generic;

namespace Extensions.Valheim;

public static class RecipeExtension
{
    public static IEnumerable<(ItemDrop.ItemData.SharedData, int)> ToList(this Recipe recipe)
    {
        var result = new (ItemDrop.ItemData.SharedData, int)[recipe.m_resources.Length];
        for (var i = 0; i < recipe.m_resources.Length; i++)
        {
            var resource = recipe.m_resources[i];
            result[i] = (resource.m_resItem.m_itemData.m_shared, resource.m_amount);
        }

        return result;
    }

    public static IEnumerable<(string, int)> ToListStr(this Recipe recipe)
    {
        var result = new (string, int)[recipe.m_resources.Length];
        for (var i = 0; i < recipe.m_resources.Length; i++)
        {
            var resource = recipe.m_resources[i];
            result[i] = (resource.m_resItem.name, resource.m_amount);
        }

        return result;
    }
}