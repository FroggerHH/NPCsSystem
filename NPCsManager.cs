using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using HarmonyLib;
using ItemManager;
using PieceManager;
using UnityEngine;
using static ItemManager.PrefabManager;

namespace NPCsSystem;

public static class NPCsManager
{
    private static List<NPC_Profile> allProfiles = new();
    internal static List<(string, string, int)> itemsToAddToHouses = new();

    public static void Initialize(AssetBundle bundle)
    {
        Plugin.Debug("Initializing NPCsManager");
        var profiles = bundle.LoadAllAssets<NPC_Profile>().ToList();
        foreach (var profile in profiles)
        {
            if (allProfiles.Contains(profile)) continue;
            allProfiles.Add(profile);
        }

        Plugin._self.StartCoroutine(RandomTalk());
    }

    private static IEnumerator RandomTalk()
    {
        yield return new WaitForSeconds(15f);
        if (NPC_Brain.allNPCs != null && NPC_Brain.allNPCs.Count > 0)
        {
            NPC_Brain.allNPCs.ToList().Random().TrySaySmt();
        }
    }

    public static NPC_Profile GetNPCProfile(string profileName)
    {
        return allProfiles.Find(x => x.name == profileName);
    }

    internal static List<NPC_Profile> GetAllProfiles()
    {
        return allProfiles;
    }

    [Description("Use it to have items in the chests by default.")]
    public static void AddDefaultItemToHouse(string houseName, string itemName, int count)
    {
        itemsToAddToHouses.Add((houseName, itemName, count));
    }
}