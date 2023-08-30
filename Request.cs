using System;
using System.Collections.Generic;

namespace NPCsSystem;

[Serializable]
public class Request
{
    //private static int ID_counter = 0;

    //public int ID;
    public RequestType requestType;
    public string npcName;
    public string thingName;
    public Dictionary<string, int> items = new();

    public Request(RequestType requestType, string npcName, Dictionary<string, int> items = null,
        string thingName = "")
    {
        this.requestType = requestType;
        this.npcName = npcName;
        this.items = items == null ? new Dictionary<string, int>() : items;
        this.thingName = thingName;
        //ID = ID_counter;
        //ID_counter++;
    }

    public override string ToString()
    {
        var result = $"" +
                     //$"ID: {ID}, " +
                     $"RequestType: {requestType}, " +
                     $"NpcName: {npcName}";
        if (!string.IsNullOrEmpty(thingName)) result += $", ThingName: {thingName}";
        return result;
    }
}