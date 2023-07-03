namespace NPCsSystem;

public struct NPC_SaveData
{
    public int ID;
    public string profileName;

    public NPC_SaveData(int id, string profileName)
    {
        ID = id;
        this.profileName = profileName;
    }

}