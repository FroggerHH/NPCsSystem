using System;
using UnityEngine;

namespace NPCsSystem;

public static class InventoryExt
{
    public static bool RemoveOneItem(this Inventory inventory, string itemLocKey)
    {
        var findItem = inventory.m_inventory.Find(x => x.m_shared.m_name == itemLocKey);
        if (findItem == null) return false;
        if (findItem.m_stack > 1)
        {
            --findItem.m_stack;
            inventory.Changed();
        }
        else
        {
            inventory.m_inventory.Remove(findItem);
            inventory.Changed();
        }

        return true;
    }

    public static bool GiveIfNotHave(this Inventory inventory, string itenName)
    {
        var item = ObjectDB.instance.GetItemPrefab(itenName)?.GetComponent<ItemDrop>();
        if (!item) return false;
        if (inventory.ContainsItemByName(item.m_itemData.m_shared.m_name)) return false;
        inventory.AddItem(item.gameObject, 1);

        return true;
    }

    public static bool ContainsItemByName(this Inventory inventory, string name)
    {
        foreach (ItemDrop.ItemData itemData in inventory.m_inventory)
        {
            if (itemData.m_shared.m_name == name)
                return true;
        }

        return false;
    }
}