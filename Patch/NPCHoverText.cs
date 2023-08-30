using HarmonyLib;

namespace NPCsSystem;

[HarmonyPatch]
public class NPCHoverText
{
    [HarmonyPatch(typeof(Character), nameof(Character.GetHoverText))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    public static void HoverText(Character __instance, ref string __result)
    {
        var npcBrain = __instance.GetComponent<NPC_Brain>();
        if (!npcBrain) return;
        __result = npcBrain.GetHoverText();
    }

    [HarmonyPatch(typeof(Character), nameof(Character.GetHoverName))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    public static void HoverName(Character __instance, ref string __result)
    {
        var npcBrain = __instance.GetComponent<NPC_Brain>();
        if (!npcBrain) return;
        __result = npcBrain.GetHoverName();
    }
}