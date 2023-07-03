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
public class ZNetScenePatch
{
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPostfix, HarmonyWrapSafe]
    public static void Patch(ZNetScene __instance)
    {
        TownDB.Initialize();
        var prefab = __instance.GetPrefab("Bandit");
        if (!prefab.GetComponent<NPC_Brain>()) prefab.AddComponent<NPC_Brain>();
        var zNetView = prefab.GetComponent<ZNetView>();
        zNetView.m_persistent = true;
        zNetView.m_type = ZDO.ObjectType.Default;
    }
}