using HarmonyLib;
using UnityEngine;

namespace NPCsSystem;

[HarmonyPatch]
public class FixOtherLocationsInTown
{
    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.SpawnLocation))]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    public static bool Patch(ZoneSystem __instance, ZoneSystem.ZoneLocation location, Vector3 pos)
    {
        if (ZoneSystem.instance.HaveLocationInRange("", "NPCTowns", pos, location.m_exteriorRadius))
            return false;

        return true;
    }

    [HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.IsBlocked))]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    public static bool Patch(ZoneSystem __instance, Vector3 p, ref bool __result)
    {
        if (ZoneSystem.instance.HaveLocationInRange("", "NPCTowns", p, 1))
        {
            __result = true;
            return false;
        }

        return true;
    }
}