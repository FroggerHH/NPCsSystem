using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ItemManager;
using PieceManager;
using UnityEngine;
using static NPCsSystem.Plugin;
using static ItemManager.PrefabManager;

namespace NPCsSystem;

public static class TownDB
{
    private static List<NPC_Profile> allProfiles = new();
    public static void Initialize()
    {
        var WoodNPSHouse = PrefabManager.RegisterPrefab(bundle, "WoodNPSHouse").GetComponentInChildren<NPC_House>();
        var TestTown = PrefabManager.RegisterPrefab(bundle, "TestTown").GetComponentInChildren<NPC_Town>();
        var profiles = Resources.FindObjectsOfTypeAll<NPC_Profile>().ToList();
        foreach (var profile in profiles)
        {
            if(string.IsNullOrEmpty(profile.prefabByName) || string.IsNullOrWhiteSpace(profile.prefabByName)) continue;
            profile.m_prefab = ZNetScene.instance.GetPrefab(profile.prefabByName);
        }

        allProfiles = profiles;
    }

    public static NPC_Profile GetProfile(string profileName)
    {
        return allProfiles.Find(x => x.name == profileName);
    }
}