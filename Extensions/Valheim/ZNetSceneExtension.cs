namespace Extensions.Valheim;

public static class ZNetSceneExtension
{
    public static ItemDrop GetItem(this ZNetScene zNetScene, string name)
    {
        return zNetScene.GetPrefab(name)?.GetComponent<ItemDrop>();
    }

    public static ItemDrop GetItem(this ZNetScene zNetScene, int hash)
    {
        return zNetScene.GetPrefab(hash)?.GetComponent<ItemDrop>();
    }
}