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
        var profiles = NPCsManager.GetAllProfiles();
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

        foreach (var container in NPSHouseWarehouse.transform.parent.GetComponentsInChildren<Container>())
        {
            NPSHouseWarehouse.AddChest(container);
        }

        foreach (var toAddToHouse in NPCsManager.itemsToAddToHouses)
        {
            var housePrefab = ZNetScene.instance.GetPrefab(toAddToHouse.Item1);
            if (!housePrefab)
            {
                DebugError($"Can't find a house with name {toAddToHouse.Item1}. Register it with PrefabManager.");
                continue;
            }

            var npcHouse = housePrefab.GetComponentInChildren<NPC_House>();
            if (!npcHouse)
            {
                DebugError($"Can't find a house component in house with name {toAddToHouse.Item1}.");
                continue;
            }

            if (!npcHouse.AddDefaultItem(toAddToHouse.Item2, toAddToHouse.Item3))
            {
                DebugError($"Can't add a default item {toAddToHouse.Item2} to a house with name {toAddToHouse.Item1}");
            }
            else
            {
                Debug($"item {toAddToHouse.Item2} added as default to a {toAddToHouse.Item1}");
            }
        }
    }
}