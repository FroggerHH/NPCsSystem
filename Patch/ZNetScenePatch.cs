using Extensions.Valheim;
using HarmonyLib;
using UnityEngine;

namespace NPCsSystem;

[HarmonyPatch]
public class ZNetScenePatch
{
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    public static void Patch(ZNetScene __instance)
    {
        void FillArray(ref GameObject[] to, ref string[] from)
        {
            to = new GameObject[from.Length];
            for (var i = 0; i < from.Length; i++)
            {
                var item = from[i];
                if (!string.IsNullOrEmpty(item) && !string.IsNullOrWhiteSpace(item))
                    to[i] = ZNetScene.instance.GetPrefab(item);
            }
        }

        var profiles = NPCsManager.GetAllProfiles();
        foreach (var profile in profiles)
        {
            if (!string.IsNullOrEmpty(profile.prefabByName) && !string.IsNullOrWhiteSpace(profile.prefabByName))
                profile.m_prefab = ZNetScene.instance.GetPrefab(profile.prefabByName);

            foreach (var item in profile.itemsToCraft)
            {
                if (!string.IsNullOrEmpty(item.prefabName) && !string.IsNullOrWhiteSpace(item.prefabName))
                    item.prefab = ZNetScene.instance.GetItem(item.prefabName);

                if (item.prefab) item.recipe = ObjectDB.instance.GetRecipe(item.prefab.m_itemData);
            }

            foreach (var item in profile.plantNames)
                if (!string.IsNullOrEmpty(item) && !string.IsNullOrWhiteSpace(item))
                    profile.plants.Add((item, ZNetScene.instance.GetPrefab(item)));

            FillArray(ref profile.defaultItems, ref profile.m_defaultItems);
            FillArray(ref profile.randomWeapon, ref profile.m_randomWeapon);
            FillArray(ref profile.randomArmor, ref profile.m_randomArmor);
            FillArray(ref profile.randomShield, ref profile.m_randomShield);
        }
    }
}