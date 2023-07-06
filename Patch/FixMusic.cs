using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ItemManager;
using UnityEngine;
using static NPCsSystem.Plugin;
using static Heightmap;
using static Heightmap.Biome;
using static ZoneSystem;
using static ZoneSystem.ZoneVegetation;

namespace NPCsSystem;

[HarmonyPatch]
public class FixMusic
{
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPostfix, HarmonyWrapSafe]
    public static void Patch(ZNetScene __instance)
    {
        var Music_FulingCamp = ZNetScene.instance.GetPrefab("Music_FulingCamp");
        if (!Music_FulingCamp) return;
        var audioSource = Music_FulingCamp.GetComponent<AudioSource>();
        if (!audioSource) return;
        var MyCoolCastle = ZNetScene.instance.GetPrefab("MyCoolCastle");
        if (!MyCoolCastle) return;

        var mixerGroup = MyCoolCastle.GetComponentInChildren<AudioSource>();
        if (!mixerGroup) return;

        mixerGroup.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
    }
}