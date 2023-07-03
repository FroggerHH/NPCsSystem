using System.Collections;
using System.Collections.Generic;
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
public class MonsterAIPatch
{
    [HarmonyPatch(typeof(Character), nameof(Character.GetHoverName)), HarmonyPostfix]
    public static void Patch(Character __instance, ref string __result)
    {
        if(!__instance.name.StartsWith("Bandit")) return;
        __result = __instance.GetComponent<NPC_Brain>().GetHoverName();
    }
}