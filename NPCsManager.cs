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
        var NPSHouseCrafter =
            PrefabManager.RegisterPrefab(bundle, "NPSHouseCrafter");


        var NPSHouseHotel = PrefabManager.RegisterPrefab(bundle, "NPSHouseHotel");
        var NPSHouseWarehouse = PrefabManager.RegisterPrefab(bundle, "NPSHouseWarehouse")
            .GetComponentInChildren<NPC_House>();
        var TestTown = PrefabManager.RegisterPrefab(bundle, "TestTown").GetComponentInChildren<NPC_Town>();
        var profiles = bundle.LoadAllAssets<NPC_Profile>().ToList();
        foreach (var profile in profiles)
        {
            if (!string.IsNullOrEmpty(profile.prefabByName) && !string.IsNullOrWhiteSpace(profile.prefabByName))
            {
                profile.m_prefab = ZNetScene.instance.GetPrefab(profile.prefabByName);
            }

            foreach (var item in profile.itemsToBuy)
            {
                if (!string.IsNullOrEmpty(item.prefabName) && !string.IsNullOrWhiteSpace(item.prefabName))
                    item.prefab = ZNetScene.instance.GetPrefab(item.prefabName).GetComponent<ItemDrop>();
            }

            foreach (var item in profile.itemsToSell)
            {
                if (!string.IsNullOrEmpty(item.prefabName) && !string.IsNullOrWhiteSpace(item.prefabName))
                    item.prefab = ZNetScene.instance.GetPrefab(item.prefabName).GetComponent<ItemDrop>();
            }

            foreach (var item in profile.itemsToCraft)
            {
                if (!string.IsNullOrEmpty(item.prefabName) && !string.IsNullOrWhiteSpace(item.prefabName))
                    item.prefab = ZNetScene.instance.GetPrefab(item.prefabName).GetComponent<ItemDrop>();
            }
        }

        allProfiles = profiles;
    }

    public static NPC_Profile GetNPCProfile(string profileName)
    {
        return allProfiles.Find(x => x.name == profileName);
    }

    public static List<NPC_Profile> GetAllProfiles()
    {
        return allProfiles;
    }

    [Description("Use it to have items in the chests by default.")]
    public static void AddDefaultItemToHouse(string houseName, string itemName, int count)
    {
        itemsToAddToHouses.Add((houseName, itemName, count));
    }
}