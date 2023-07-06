using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ItemManager;
using UnityEngine;
using static NPCsSystem.Plugin;
using static Heightmap;
using static Heightmap.Biome;
using static ZoneSystem;
using static ZoneSystem.ZoneVegetation;

namespace NPCsSystem;

[HarmonyPatch]
public class ZNetScenePatch
{
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPostfix, HarmonyWrapSafe]
    public static void Patch(ZNetScene __instance)
    {
        var NPSHouseWarehouse = PrefabManager.RegisterPrefab(bundle, "NPSHouseWarehouse")
            .GetComponentInChildren<NPC_House>();
        var NPSHouseHotel = PrefabManager.RegisterPrefab(bundle, "NPSHouseHotel").GetComponentInChildren<NPC_House>();
        var TestTown = PrefabManager.RegisterPrefab(bundle, "TestTown").GetComponentInChildren<NPC_Town>();
        var profiles = TownDB.GetAllProfiles();
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

        NPSHouseWarehouse.FindAllChests();
        NPSHouseWarehouse.AddItem("CookedDeerMeat", 40);
        NPSHouseWarehouse.AddItem("CookedDeerMeat", 40);
        NPSHouseWarehouse.AddItem("CookedMeat", 40);
        NPSHouseWarehouse.AddItem("CookedMeat", 40);
        NPSHouseWarehouse.AddItem("RawMeat", 25);

        NPSHouseWarehouse.AddItem("Bronze", 150);
        NPSHouseWarehouse.AddItem("Iron", 150);
        NPSHouseWarehouse.AddItem("Silver", 150);
        NPSHouseWarehouse.AddItem("BoneFragments", 150);
        NPSHouseWarehouse.AddItem("DeerHide", 150);
        NPSHouseWarehouse.AddItem("TrollHide", 150);
        NPSHouseWarehouse.AddItem("Feathers", 80);
        NPSHouseWarehouse.AddItem("Wood", 200);
    }
}