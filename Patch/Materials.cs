using System.Collections.Generic;
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
public class Materials
{
    [HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake)), HarmonyPostfix, HarmonyWrapSafe]
    public static void FixMaterials()
    {
        FixBundle(bundle);
    }

    private static void FixBundle(AssetBundle assetBundle)
    {
        foreach (var asset in assetBundle.LoadAllAssets<GameObject>())
        {
            FixRenderers(asset);
            FixInstanceRenderer(asset);
            FixPiece(asset);
            FixWearNTear(asset);
            FixCharacter(asset);
            FixDestructible(asset);
            FixPickable(asset);
            FixContainer(asset);
            FixFireplace(asset);
            FixTerrainMod(asset);
        }
    }

    private static void FixContainer(GameObject asset)
    {
        var container = asset.GetComponent<Container>();
        if (container != null)
        {
            FixEffect(container.m_openEffects, asset.name);
            FixEffect(container.m_closeEffects, asset.name);
        }
    }

    private static void FixFireplace(GameObject asset)
    {
        var fireplace = asset.GetComponent<Fireplace>();
        if (fireplace != null)
        {
            FixEffect(fireplace.m_fuelAddedEffects, asset.name);
        }
    }

    private static void FixPickable(GameObject asset)
    {
        var pickable = asset.GetComponent<Pickable>();
        if (pickable != null)
        {
            FixEffect(pickable.m_pickEffector, asset.name);
        }
    }

    private static void FixDestructible(GameObject asset)
    {
        var destructible = asset.GetComponent<Destructible>();
        if (destructible != null)
        {
            FixEffect(destructible.m_destroyedEffect, asset.name);
            FixEffect(destructible.m_hitEffect, asset.name);
        }
    }

    private static void FixCharacter(GameObject asset)
    {
        var character = asset.GetComponent<Character>();
        if (character != null)
        {
            FixEffect(character.m_deathEffects, asset.name);
            FixEffect(character.m_hitEffects, asset.name);
            FixEffect(character.m_jumpEffects, asset.name);
            FixEffect(character.m_slideEffects, asset.name);
            FixEffect(character.m_tarEffects, asset.name);
            FixEffect(character.m_waterEffects, asset.name);
            FixEffect(character.m_backstabHitEffects, asset.name);
            FixEffect(character.m_critHitEffects, asset.name);
            FixEffect(character.m_flyingContinuousEffect, asset.name);
        }
    }

    private static void FixWearNTear(GameObject asset)
    {
        var wearNTear = asset.GetComponent<WearNTear>();
        if (wearNTear != null)
        {
            FixEffect(wearNTear.m_destroyedEffect, asset.name);
            FixEffect(wearNTear.m_hitEffect, asset.name);
            FixEffect(wearNTear.m_switchEffect, asset.name);
        }
    }

    private static void FixPiece(GameObject asset)
    {
        var piece = asset.GetComponent<Piece>();
        if (piece != null)
        {
            FixEffect(piece.m_placeEffect, asset.name);
        }
    }

    private static void FixTerrainMod(GameObject asset)
    {
        var terrainOp = asset.GetComponent<TerrainOp>();
        var TerrainModifier = asset.GetComponent<TerrainModifier>();
        if (terrainOp != null)
        {
            FixEffect(terrainOp.m_onPlacedEffect, asset.name);
        }

        if (TerrainModifier != null)
        {
            FixEffect(TerrainModifier.m_onPlacedEffect, asset.name);
        }
    }

    private static void FixInstanceRenderer(GameObject asset)
    {
        var instanceRenderers = asset.GetComponentsInChildren<InstanceRenderer>();
        if (instanceRenderers != null && instanceRenderers.Length > 0)
        {
            foreach (InstanceRenderer renderer in instanceRenderers)
            {
                if (!renderer) continue;
                if (!renderer.m_material)
                {
                    //DebugError($"No material found for InstanceRenderer {renderer.name}", true);
                    continue;
                }

                renderer.m_material.shader = Shader.Find(renderer.m_material.shader.name);
            }
        }
    }

    private static void FixRenderers(GameObject asset)
    {
        var renderers = asset.GetComponentsInChildren<Renderer>();
        if (renderers != null && renderers.Length > 0)
        {
            foreach (Renderer renderer in renderers)
            {
                if (!renderer) continue;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (!material) continue;
                    string name = material.shader.name;
                    material.shader = Shader.Find(name);
                    //Debug($"Shader for {asset.name}({renderer.name}) set to {material.shader.name}");
                }
            }
        }
    }

    private static void FixEffect(EffectList effectList, string objName)
    {
        foreach (EffectList.EffectData effectData in effectList.m_effectPrefabs)
        {
            if (effectData == null) continue;
            if (!effectData.m_prefab == null)
            {
                //DebugError($"No prefab found for place effect of {objName}", true);
                continue;
            }

            FixRenderers(effectData.m_prefab);
        }
    }
}