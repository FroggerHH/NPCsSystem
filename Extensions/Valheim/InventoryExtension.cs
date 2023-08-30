namespace Extensions.Valheim;

public static class InventoryExtension
{
    public static bool RemoveOneItemByLocKey(this Inventory inventory, string itemLocKey)
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

    public static bool GiveItemIfNotHave(this Inventory inventory, string prefabName, int count = 1)
    {
        var item = ObjectDB.instance.GetItemPrefab(prefabName)?.GetComponent<ItemDrop>();
        if (!item) return false;
        if (inventory.ContainsItemByName(item.m_itemData.m_shared.m_name)) return false;
        inventory.AddItem(item.gameObject, count);

        return true;
    }

    public static bool ContainsItemByLocKey(this Inventory inventory, string localizedName)
    {
        foreach (var itemData in inventory.m_inventory)
            if (itemData.m_shared.m_name == localizedName)
                return true;

        return false;
    }
}