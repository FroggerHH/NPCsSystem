using Extensions;
using HarmonyLib;
using UnityEngine;

namespace NPCsSystem;

[HarmonyPatch]
public class FixPieces
{
    [HarmonyPatch(typeof(StationExtension), nameof(StationExtension.Awake))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    public static void FixPiece(StationExtension __instance)
    {
        var extensionOrig = ZNetScene.instance.GetPrefab(__instance.GetPrefabName()).GetComponent<StationExtension>();
        ;
        __instance.m_craftingStation = extensionOrig.m_craftingStation;
        __instance.m_connectionPrefab = extensionOrig.m_connectionPrefab;
    }

    [HarmonyPatch(typeof(Sign), nameof(Sign.Awake))]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    public static bool FixSign(Sign __instance)
    {
        if (__instance.name.Contains("REPLACE"))
        {
            Object.Destroy(__instance.gameObject);
            return false;
        }

        return true;
    }
}