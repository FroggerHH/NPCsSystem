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
        var profiles = NPCsManager.GetAllProfiles();
        foreach (var profile in profiles)
        {
            if (!string.IsNullOrEmpty(profile.prefabByName) && !string.IsNullOrWhiteSpace(profile.prefabByName))
            {
                profile.m_prefab = ZNetScene.instance.GetPrefab(profile.prefabByName);
            }

            foreach (var item in profile.itemsToCraft)
            {
                if (!string.IsNullOrEmpty(item.prefabName) && !string.IsNullOrWhiteSpace(item.prefabName))
                    item.prefab = ZNetScene.instance.GetPrefab(item.prefabName).GetComponent<ItemDrop>();

                item.recipe = ObjectDB.instance.GetRecipe(item.prefab.m_itemData);
            }

            foreach (var item in profile.plantNames)
            {
                if (!string.IsNullOrEmpty(item) && !string.IsNullOrWhiteSpace(item))
                    profile.plants.Add(ZNetScene.instance.GetPrefab(item));
            }
        }

        foreach (var item in TradeItem.all)
        {
            if (!string.IsNullOrEmpty(item.prefabName) && !string.IsNullOrWhiteSpace(item.prefabName))
            {
                var prefab = ZNetScene.instance.GetPrefab(item.prefabName);
                if (!prefab)
                {
                    DebugError($"Can't find item {item.prefabName} for trade {item}");
                    continue;
                }

                item.prefab = prefab.GetComponent<ItemDrop>();
            }

            if (!string.IsNullOrEmpty(item.moneyItemName) && !string.IsNullOrWhiteSpace(item.moneyItemName))
                item.moneyItem = ZNetScene.instance.GetPrefab(item.moneyItemName).GetComponent<ItemDrop>();

            foreach (var npcName in item.npcNames)
            {
                var npc = NPCsManager.GetNPCProfile(npcName);
                if (!npc)
                {
                    DebugError($"Can't find npc {npcName}");
                    continue;
                }

                item.npc = npc;
                npc.tradeItems.Add(item);
            }
        }

        TraderPatch.coinPrefab = ZNetScene.instance.GetPrefab("Coins").GetComponent<ItemDrop>();
    }
}