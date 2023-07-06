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
    public static void Fix(Piece __instance)
    {
        Materials.FixPiece(__instance);
    }
}