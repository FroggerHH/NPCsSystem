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
    [HarmonyPatch(typeof(Piece), nameof(Piece.Awake)), HarmonyPostfix, HarmonyWrapSafe]
    public static void PiecePlace(Piece __instance)
    {
        //__instance.StartCoroutine(WaitForPlace(__instance));
        PlacePieceInTown(__instance);
    }

    [HarmonyPatch(typeof(Piece), nameof(Piece.OnDestroy)), HarmonyPrefix, HarmonyWrapSafe]
    public static void PieceDestroy(Piece __instance)
    {
        if (!__instance.IsPlacedByPlayer() || Game.instance.IsShuttingDown()) return;
        var house = NPC_House.FindHouse(__instance.transform.position);
        if (!house) return;

        if (__instance.TryGetComponent(out Bed bed))
        {
            house.RemoveBed(bed);
        }

        if (__instance.TryGetComponent(out CraftingStation craftingStatione))
        {
            house.RemoveCraftingStation(craftingStatione);
        }

        if (__instance.TryGetComponent(out Container container))
        {
            house.RemoveChest(container);
        }

        if (__instance.TryGetComponent(out Door door))
        {
            house.RemoveDoor(door);
        }


        if (__instance.TryGetComponent(out Sign sign))
        {
            house.RemoveSign(sign);
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
            house.AddBed(bed);
        }

        if (piece.TryGetComponent(out CraftingStation craftingStatione))
        {
            house.AddCraftingStation(craftingStatione);
        }

        if (piece.TryGetComponent(out Container container))
        {
            house.AddChest(container);
        }

        if (piece.TryGetComponent(out Sign sign))
        {
            house.AddSign(sign);
        }
    }
}