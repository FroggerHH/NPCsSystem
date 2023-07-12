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
public class FixPlantsInTown
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.UpdateHealth)), HarmonyPrefix, HarmonyWrapSafe]
    public static void Patch(Plant __instance)
    {
        if (NPC_Town.FindTown(__instance.transform.position)) __instance.m_status = Plant.Status.Healthy;
    }
}