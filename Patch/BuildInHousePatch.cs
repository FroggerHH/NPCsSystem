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
public class BuildInHousePatch
{
    [HarmonyPatch(typeof(Piece), nameof(Piece.Awake)), HarmonyPostfix]
    public static void PiecePlace(Piece __instance)
    {
        __instance.StartCoroutine(WaitForPlace(__instance));
    }

    [HarmonyPatch(typeof(Piece), nameof(Piece.OnDestroy)), HarmonyPrefix]
    public static void PieceDestroy(Piece __instance)
    {
        if(!__instance.IsPlacedByPlayer()) return;
        var house = NPC_House.FindHouse(__instance.transform.position);
        if (!house) return;

        if (__instance.TryGetComponent(out Bed _))
        {
            house.SetBed(null);
        }

        if (__instance.TryGetComponent(out CraftingStation craftingStatione))
        {
            house.RemoveCraftingStation(craftingStatione);
        }
    }

    private static IEnumerator WaitForPlace(Piece piece)
    {
        yield return new WaitUntil(() => piece.IsPlacedByPlayer());

        PlacePieceInTown(piece);
    }

    private static void PlacePieceInTown(Piece piece)
    {
        var house = NPC_House.FindHouse(piece.transform.position);
        if (!house) return;

        if (piece.TryGetComponent(out Bed bed))
        {
            house.SetBed(bed);
        }

        if (piece.TryGetComponent(out CraftingStation craftingStatione))
        {
            house.AddCraftingStation(craftingStatione);
        }
    }
}