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
public class UpdateNPC_AI
{
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.UpdateAI)), HarmonyPostfix, HarmonyWrapSafe]
    public static void Patch(BaseAI __instance, float dt)
    {
        if (__instance is not NPC_Brain brain) return;
        brain.UpdateAI(dt);
    }
}