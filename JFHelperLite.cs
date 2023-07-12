using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Audio;
using static JustAFrogger.JFHelper;

namespace JustAFrogger;

[PublicAPI]
public class JFHelperLite
{
    private static readonly List<MusicLocation> allMusicLocations = new();

    public static void Initialize(ManualLogSource mod)
    {
        if (JFHelper.isInitialized) return;
        JFHelper.Initialize(mod);
    }

    static JFHelperLite()
    {
        Harmony harmony = new("org.bepinex.helpers.JFHelperLite");
        harmony.Patch(AccessTools.DeclaredMethod(typeof(ZNetScene), nameof(ZNetScene.Awake)),
            new HarmonyMethod(AccessTools.DeclaredMethod(typeof(JFHelperLite), nameof(Patch_ZNetSceneAwake))));
    }

    private struct BundleId
    {
        [UsedImplicitly] public string assetBundleFileName;
        [UsedImplicitly] public string folderName;
    }

    private static readonly Dictionary<BundleId, AssetBundle> bundleCache = new();

    public static AssetBundle RegisterAssetBundle(string assetBundleFileName, string folderName = "assets")
    {
        BundleId id = new() { assetBundleFileName = assetBundleFileName, folderName = folderName };
        if (!bundleCache.TryGetValue(id, out AssetBundle assets))
        {
            assets = bundleCache[id] =
                Resources.FindObjectsOfTypeAll<AssetBundle>().FirstOrDefault(a => a.name == assetBundleFileName) ??
                AssetBundle.LoadFromStream(Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream(Assembly.GetExecutingAssembly().GetName().Name + $".{folderName}." +
                                               assetBundleFileName));
        }

        return assets;
    }

    private static GameObject GetPrefab(string assetBundleFileName, string prefabName,
        string folderName = "assets") =>
        GetPrefab(RegisterAssetBundle(assetBundleFileName, folderName), prefabName);

    private static GameObject GetPrefab(AssetBundle assets, string prefabName) =>
        assets.LoadAsset<GameObject>(prefabName);


    [HarmonyPriority(Priority.VeryHigh), HarmonyWrapSafe]
    private static void Patch_ZNetSceneAwake(ZNetScene __instance)
    {
        foreach (var musicLocation in allMusicLocations)
        {
            JFHelper.FixMusicLocation(musicLocation, showErrorIfCantFindAudioClip: false);
        }
    }

    [Description(
        "Accepts a prefab that has a MusicLocation component inside it. Fixes it by making it work correctly.")]
    public static void FixMusicLocation(string assetBundleFileName, string prefabName, string folderName = "assets")
    {
        var prefab = GetPrefab(assetBundleFileName, prefabName, folderName);
        if (!prefab)
            throw new UnityException(
                $"{helperNameMsg} Can't find prefab {prefabName} in asset bundle {assetBundleFileName}");
        foreach (var musicLocation in prefab.GetComponentsInChildren<MusicLocation>())
        {
            allMusicLocations.Add(musicLocation);
        }
    }

    [Description(
        "Accepts a prefab that has a MusicLocation component inside it. Fixes it by making it work correctly.")]
    public static void FixMusicLocation(AssetBundle assets, string prefabName)
    {
        var prefab = GetPrefab(assets, prefabName);
        if (!prefab)
            throw new UnityException(
                $"{helperNameMsg} Can't find prefab {prefabName} in asset bundle {assets.name}");
        foreach (var musicLocation in prefab.GetComponentsInChildren<MusicLocation>())
        {
            allMusicLocations.Add(musicLocation);
        }
    }
}