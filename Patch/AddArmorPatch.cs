using HarmonyLib;
using ItemManager;
using UnityEngine;
using static NPCsSystem.Plugin;
using UnityEngine.SceneManagement;

namespace NPCsSystem;

[HarmonyPatch]
internal class AddArmorPatch
{
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GiveDefaultItems)), HarmonyPrefix]
    public static bool Patch(Humanoid __instance)
    {
        if (!__instance.name.StartsWith("PlayerNPS")) return true;
        var ArmorBronzeChest = ZNetScene.instance.GetPrefab("ArmorBronzeChest"); //Obtendo itens
        var ArmorBronzeLegs = ZNetScene.instance.GetPrefab("ArmorBronzeLegs");
        var HelmetBronze = ZNetScene.instance.GetPrefab("HelmetBronze");
        var AxeBronze = ZNetScene.instance.GetPrefab("AxeBronze");
        var ShieldBronzeBuckler = ZNetScene.instance.GetPrefab("ShieldBronzeBuckler");

        __instance.GiveDefaultItem(ArmorBronzeChest);
        __instance.GiveDefaultItem(ArmorBronzeLegs);
        __instance.GiveDefaultItem(AxeBronze); //Eu dou itens para a turba
        __instance.GiveDefaultItem(HelmetBronze);
        __instance.GiveDefaultItem(ShieldBronzeBuckler);
        return false;
    }

    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPostfix, HarmonyWrapSafe]
    public static void ZNetScenePatch(ZNetScene __instance)
    {
        var PlayerNPS = bundle.LoadAsset<GameObject>("PlayerNPS");
        Debug($"PlayerNPS is {PlayerNPS}");

        var visEquipment = PlayerNPS.GetComponent<VisEquipment>();
        Debug($"visEquipment is {visEquipment}");
        Debug($"m_bodyModel is {visEquipment.m_bodyModel}");
        Debug($"material is {visEquipment.m_bodyModel.material}");
        var player = __instance.GetPrefab("Player");
        Debug($"player is {player}");
        visEquipment.m_bodyModel.material = //Estou mudando o material do mob para o original
            player.GetComponent<VisEquipment>().m_bodyModel.material;
    }
}