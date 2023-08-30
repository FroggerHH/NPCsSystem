using HarmonyLib;

namespace NPCsSystem;

[HarmonyPatch]
public class UpdateNPC_AI
{
    [HarmonyPatch(typeof(BaseAI), nameof(BaseAI.UpdateAI))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    public static void Patch(BaseAI __instance, float dt)
    {
        if (__instance is not NPC_Brain brain) return;
        brain.UpdateAI(dt);
    }
}