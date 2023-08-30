using HarmonyLib;

namespace NPCsSystem;

[HarmonyPatch]
public class NotkillNPCs
{
    [HarmonyPatch(typeof(Character), nameof(Character.Damage))]
    [HarmonyPrefix]
    [HarmonyWrapSafe]
    public static bool Patch(Character __instance, HitData hit)
    {
        if (hit.m_damage.m_damage == 1E+10f && __instance.GetComponent<NPC_Brain>()) return false;
        return true;
    }
}