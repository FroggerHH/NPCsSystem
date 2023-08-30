namespace Extensions.Valheim;

public static class ObjectDBExtension
{
    public static ItemDrop GetItem(this ObjectDB objectDB, string name)
    {
        return objectDB.GetItemPrefab(name)?.GetComponent<ItemDrop>();
    }

    public static ItemDrop GetItem(this ObjectDB objectDB, int hash)
    {
        return objectDB.GetItemPrefab(hash)?.GetComponent<ItemDrop>();
    }
}