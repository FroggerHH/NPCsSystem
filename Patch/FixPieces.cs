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
public class FixPieces
{
    [HarmonyPatch(typeof(Piece), nameof(Piece.Awake)), HarmonyPostfix, HarmonyWrapSafe]
    public static void FixPiece(Piece __instance)
    {
        Materials.FixPiece(__instance);
    }

    [HarmonyPatch(typeof(Sign), nameof(Sign.Awake)), HarmonyPrefix, HarmonyWrapSafe]
    public static bool FixSign(Sign __instance)
    {
        if (__instance.name.Contains("REPLACE")) return false;
        return true;
    }
}