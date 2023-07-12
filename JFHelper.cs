﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Audio;
using Logger = BepInEx.Logging.Logger;

namespace JustAFrogger;

public static class JFHelper
{
    private static readonly Dictionary<string, AudioMixerGroup> allAudioMixers = new();

    private static readonly Dictionary<string, AudioClip> allAudioClips = new();

    //private static ManualLogSource modLogger;
    internal static bool isInitialized = false;

    internal const string notInitializedErrorMsg =
        $"[JFHelper] _JFHelper is not initialized. Please initialize it Awake like this: JustAFrogger.JFHelper.Initialize(Info);";

    internal static string helperNameMsg => $"[" +
                                            //$"{modLogger.SourceName}" +
                                            $"_JFHelper]";

    internal static void Initialize(ManualLogSource mod)
    {
        if (isInitialized) return;
        isInitialized = true;
        //JFHelper.modLogger = mod;
        foreach (var audioMixerGroup in Resources.FindObjectsOfTypeAll<AudioMixerGroup>())
        {
            var name = audioMixerGroup.name;
            if (!allAudioMixers.ContainsKey(name)) allAudioMixers.Add(name, audioMixerGroup);
        }

        foreach (var audioClip in Resources.FindObjectsOfTypeAll<AudioClip>())
        {
            var name = audioClip.name;
            if (!allAudioClips.ContainsKey(name)) allAudioClips.Add(name, audioClip);
        }
    }

    [Description("Returns the nearest nearestTo [nearestTo] object from [all] list.")]
    public static T Nearest<T>(List<T> all, Vector3 nearestTo) where T : MonoBehaviour
    {
        T current = default(T);
        //if (modLogger == null) throw new UnityException(notInitializedErrorMsg);

        float oldDistance = int.MaxValue;
        if (all == null || all.Count == 0) return current;
        foreach (T pos_ in all)
        {
            var pos = (pos_ as MonoBehaviour).transform.position;
            float dist = Utils.DistanceXZ(nearestTo, pos);
            if (dist < oldDistance)
            {
                current = pos_;
                oldDistance = dist;
            }
        }

        return current;
    }

    [Description("Returns vanila audio output mixer")]
    public static AudioMixerGroup GetVanilaAudioMixer(string name)
    {
        //if (modLogger == null) throw new UnityException(notInitializedErrorMsg);
        if (allAudioMixers.TryGetValue(name, out var result))
        {
            return result;
        }
        else
        {
            throw new UnityException($"{helperNameMsg} Can't find audio mixer {name}");
        }
    }

    [Description("Returns vanila audio clip")]
    public static AudioClip GetVanilaMusic(string name, bool showErrorIfCantFindAudioClip)
    {
        //if (modLogger == null) throw new UnityException(notInitializedErrorMsg);

        if (allAudioClips.TryGetValue(name, out var result))
        {
            return result;
        }
        else if (showErrorIfCantFindAudioClip)
        {
            throw new UnityException($"{helperNameMsg} Can't find audio clip {name}");
        }

        return null;
    }

    internal static void FixMusicLocation(MusicLocation musicLocation, bool showErrorIfCantFindAudioClip = true)
    {
        Debug.Log($"0");
        if (!musicLocation) return;
        Debug.Log($"1");
        var audioSource = musicLocation.GetComponent<AudioSource>();
        Debug.Log($"2");
        if (!audioSource)
        {
            return;
        }

        Debug.Log($"3");


        var outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
        Debug.Log($"4");
        if (!outputAudioMixerGroup)
        {
            return;
        }

        Debug.Log($"5");


        audioSource.outputAudioMixerGroup = GetVanilaAudioMixer(outputAudioMixerGroup.name);
        Debug.Log($"6");

        var audioClip = audioSource.clip;
        Debug.Log($"7");
        if (!audioClip)
        {
            return;
        }

        Debug.Log($"8");

        var vanilaMusic = GetVanilaMusic(audioClip.name, showErrorIfCantFindAudioClip);
        Debug.Log($"9");
        if (vanilaMusic)
        {
            Debug.Log($"10");
            audioSource.clip = vanilaMusic;
        }

        Debug.Log($"11");
    }
}