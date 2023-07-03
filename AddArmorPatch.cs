using HarmonyLib;
using ItemManager;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bandidos;

[HarmonyPatch]
internal class AddArmorPatch
{
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.GiveDefaultItems)), HarmonyPrefix]
    public static bool Patch(Humanoid __instance)
    {
        if (!__instance.name.StartsWith("Bandit")) return true;
        var ArmorBronzeChest = ZNetScene.instance.GetPrefab("ArmorBronzeChest"); //Obtendo itens
        var ArmorBronzeLegs = ZNetScene.instance.GetPrefab("ArmorBronzeLegs");
        var HelmetBronze = ZNetScene.instance.GetPrefab("HelmetBronze");
        var AxeBronze = ZNetScene.instance.GetPrefab("AxeBronze");
        var ShieldBronzeBuckler = ZNetScene.instance.GetPrefab("ShieldBronzeBuckler");
        
        __instance.GiveDefaultItem(ArmorBronzeChest);
        __instance.GiveDefaultItem(ArmorBronzeLegs);
        __instance.GiveDefaultItem(AxeBronze);//Eu dou itens para a turba
        __instance.GiveDefaultItem(HelmetBronze);
        __instance.GiveDefaultItem(ShieldBronzeBuckler);
        return false;
    }
    
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPostfix]
    public static void ZNetScenePatch(ZNetScene __instance)
    {
        var Bandit = __instance.GetPrefab("Bandit");

        Bandit.GetComponent<VisEquipment>().m_bodyModel.material = //Estou mudando o material do mob para o original
            __instance.GetPrefab("Player").GetComponent<VisEquipment>().m_bodyModel.material;
    }
}