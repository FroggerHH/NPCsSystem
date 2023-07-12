using System;
using System.Collections.Generic;
using System.ComponentModel;
using BepInEx;
using UnityEngine;
using UnityEngine.Audio;

namespace JustAFrogger;

public class JFHelper
{
    private static readonly Dictionary<string, AudioMixerGroup> allAudioMixers = new();
    private static readonly Dictionary<string, AudioClip> allAudioClips = new();
    private static PluginInfo mod;
    internal static bool isInitialized = false;

    internal const string notInitializedErrorMsg =
        $"[JFHelper] _JFHelper is not initialized. Please initialize it Awake like this: JustAFrogger.JFHelper.Initialize(Info);";

    internal static string helperNameMsg => $"[{mod.Instance.name}_JFHelper]";

    internal static void Initialize(PluginInfo mod)
    {
        if (isInitialized) return;
        isInitialized = true;
        foreach (var audioMixerGroup in Resources.FindObjectsOfTypeAll<AudioMixerGroup>())
        {
            allAudioMixers.Add(audioMixerGroup.name, audioMixerGroup);
        }

        foreach (var audioClip in Resources.FindObjectsOfTypeAll<AudioClip>())
        {
            allAudioClips.Add(audioClip.name, audioClip);
        }
    }

    [Description("Returns the nearest nearestTo [nearestTo] object from [all] list.")]
    public static T Nearest<T>(List<T> all, Vector3 nearestTo) where T : MonoBehaviour
    {
        T current = default(T);
        if (mod == null) throw new UnityException(notInitializedErrorMsg);

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
        if (mod == null) throw new UnityException(notInitializedErrorMsg);
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
        if (mod == null) throw new UnityException(notInitializedErrorMsg);

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
        if (mod == null) throw new UnityException(notInitializedErrorMsg);

        musicLocation.m_audioSource.outputAudioMixerGroup =
            GetVanilaAudioMixer(musicLocation.m_audioSource.outputAudioMixerGroup.name);

        var vanilaMusic = GetVanilaMusic(musicLocation.m_audioSource.clip.name, showErrorIfCantFindAudioClip);
        if (vanilaMusic) musicLocation.m_audioSource.clip = vanilaMusic;
    }
}