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
}