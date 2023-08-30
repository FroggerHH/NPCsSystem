using HarmonyLib;

namespace NPCsSystem;

[HarmonyPatch]
public class FixPlantsInTown
{
    [HarmonyPatch(typeof(Plant), nameof(Plant.UpdateHealth))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    public static void Patch(Plant __instance)
    {
        if (NPC_Town.FindTown(__instance.transform.position)) __instance.m_status = Plant.Status.Healthy;
    }
}