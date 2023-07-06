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
public class OnContainerChanged
{
    [HarmonyPatch(typeof(Container), nameof(Container.OnContainerChanged)), HarmonyPostfix, HarmonyWrapSafe]
    public static void Patch(Container __instance)
    {
        //var sleepHouse = NPC_House.FindHouse(__instance.transform.position);
        //if (!sleepHouse) return;

        //sleepHouse.ChestInventoryChanged();
    }
}