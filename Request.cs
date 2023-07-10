using System;
using System.Collections.Generic;

namespace NPCsSystem;

[Serializable]
public class Request
{
    public RequestType requestType;
    public string npcName;
    public List<(ItemDrop.ItemData.SharedData, int)> items = new();
    public string thingName;

    public Request(RequestType requestType, string npcName, List<(ItemDrop.ItemData.SharedData, int)> items)
    {
        this.requestType = requestType;
        this.npcName = npcName;
        this.items = items;
        this.thingName = thingName;
    }

    public Request(RequestType requestType, string npcName, List<(ItemDrop.ItemData.SharedData, int)> items,
        string thingName)
    {
        this.requestType = requestType;
        this.npcName = npcName;
        this.items = items;
        this.thingName = thingName;
    }

    public Request(RequestType requestType, string npcName, string thingName)
    {
        this.requestType = requestType;
        this.npcName = npcName;
        this.thingName = thingName;
    }

    public Request(RequestType requestType, string npcName)
    {
        this.requestType = requestType;
        this.npcName = npcName;
        this.thingName = thingName;
    }
}