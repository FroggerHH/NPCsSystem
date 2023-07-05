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
public class NPCHoverText
{
    [HarmonyPatch(typeof(Character), nameof(Character.GetHoverText)), HarmonyPostfix, HarmonyWrapSafe]
    public static void HoverText(Character __instance, ref string __result)
    {
        var npcBrain = __instance.GetComponent<NPC_Brain>();
        if (!npcBrain) return;
        __result = npcBrain.GetHoverText();
    }

    [HarmonyPatch(typeof(Character), nameof(Character.GetHoverName)), HarmonyPostfix, HarmonyWrapSafe]
    public static void HoverName(Character __instance, ref string __result)
    {
        var npcBrain = __instance.GetComponent<NPC_Brain>();
        if (!npcBrain) return;
        __result = npcBrain.GetHoverName();
    }
}