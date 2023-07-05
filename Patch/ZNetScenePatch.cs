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
        var WoodNPSHouse = PrefabManager.RegisterPrefab(bundle, "WoodNPSHouse").GetComponentInChildren<NPC_House>();
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
        
    }
}