using System.Collections;
using Extensions;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NPCsSystem;

[HarmonyPatch]
public class BuildInHousePatch
{
    [HarmonyPatch(typeof(Piece), nameof(Piece.Awake))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    public static void PiecePlace([NotNull] Piece __instance)
    {
        if (SceneManager.GetActiveScene().name != "main") return;
        __instance.StartCoroutine(WaitForPlace(__instance));
    }


    [HarmonyPatch(typeof(Piece), nameof(Piece.OnDestroy))]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    public static void PieceDestroy([NotNull] Piece __instance)
    {
        if (SceneManager.GetActiveScene().name != "main") return;
        OnDestroy(__instance);
    }

    private static void OnDestroy(Piece piece)
    {
        if (!piece.m_nview || piece.m_nview.m_ghost || Game.instance.IsShuttingDown()) return;
        RemoveRefs(piece);
    }

    private static void RemoveRefs(Piece piece)
    {
        Plugin.Debug($"RemoveRefs called on {piece.GetPrefabName()}");
        var house = NPC_House.FindHouse(piece.transform.position);
        if (!house) return;
        if (piece.TryGetComponent(out Bed bed)) house.RemoveBed(bed);

        else if (piece.m_category == Piece.PieceCategory.Crafting &&
                 piece.TryGetComponent(out CraftingStation craftingStation))
            house.RemoveCraftingStation(craftingStation);

        else if (piece.TryGetComponent(out Container container)) house.RemoveChest(container);

        else if (piece.TryGetComponent(out Door door)) house.RemoveDoor(door);

        else if (piece.TryGetComponent(out Sign sign)) house.RemoveSign(sign);
    }

    private static IEnumerator WaitForPlace(Piece piece)
    {
        if (piece && piece.m_nview)
        {
            yield return new WaitUntil(() => piece.m_nview.m_ghost == false);

            PlacePieceInTown(piece);
        }
    }

    private static void PlacePieceInTown(Piece piece)
    {
        var house = NPC_House.FindHouse(piece.transform.position);
        if (!house) return;

        if (piece.TryGetComponent(out Bed bed)) house.AddBed(bed);

        else if (piece.m_category == Piece.PieceCategory.Crafting &&
                 piece.TryGetComponent(out CraftingStation craftingStation)) house.AddCraftingStation(craftingStation);

        else if (piece.TryGetComponent(out Container container)) house.AddChest(container);

        else if (piece.TryGetComponent(out Sign sign)) house.AddSign(sign);
    }
}