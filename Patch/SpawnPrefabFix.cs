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
public class SpawnPrefabFix
{
    [HarmonyPatch(typeof(SpawnPrefab), nameof(SpawnPrefab.Start)), HarmonyPrefix, HarmonyWrapSafe]
    public static void Patch(SpawnPrefab __instance)
    {
        if (!__instance.name.StartsWith("SpawnPrefab_")) return;
        __instance.m_prefab = ZNetScene.instance.GetPrefab(__instance.GetPrefabName().Replace("SpawnPrefab_", ""));
    }

    [HarmonyPatch(typeof(SpawnPrefab), nameof(SpawnPrefab.TrySpawn)), HarmonyPrefix, HarmonyWrapSafe]
    public static bool PatchTrySpawn(SpawnPrefab __instance)
    {
        if (!__instance.m_prefab) return false;
        return true;
    }
}