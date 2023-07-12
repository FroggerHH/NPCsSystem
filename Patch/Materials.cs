﻿using System.Collections.Generic;
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
    public static void NPCsSystemFixMaterials()
    {
        FixSmt(bundle.LoadAsset<GameObject>("NPSHouseCrafter"));
        FixSmt(bundle.LoadAsset<GameObject>("NPSHouseHotel"));
        FixSmt(bundle.LoadAsset<GameObject>("NPSHouseWarehouse"));
        FixSmt(bundle.LoadAsset<GameObject>("NPSPark"));
        var npc = bundle.LoadAsset<GameObject>("PlayerNPS");
        FixSmt(npc);
        Utils.FindChild(npc.transform, "HammerMark").GetComponent<Renderer>().sharedMaterial =
            Utils.FindChild(ZNetScene.instance.GetPrefab("piece_workbench").transform, "Particle System")
                .GetComponent<Renderer>().sharedMaterial;
    }

    private static void FixBundle(AssetBundle assetBundle)
    {
        foreach (var asset in assetBundle.LoadAllAssets<GameObject>())
        {
            foreach (var child in asset.GetComponentsInChildren<GameObject>())
            {
                FixSmt(child);
            }
        }
    }

    private static void FixSmt(GameObject child)
    {
        FixRenderers(child);
        FixPiece(child);
        FixWearNTear(child);
        FixCharacter(child);
        FixDestructible(child);
        FixPickable(child);
        FixContainer(child);
        FixFireplace(child);
        FixNPC(child);
        FixTree(child);
        FixDropOnDestroyed(child);
    }

    private static void FixContainer(GameObject asset)
    {
        var all = asset.GetComponentsInChildren<Container>();
        if (all != null && all.Length > 0)
        {
            foreach (var obj in all)
            {
                FixEffect(obj.m_openEffects, obj.name);
                FixEffect(obj.m_closeEffects, obj.name);
            }
        }
    }

    private static void FixFireplace(GameObject asset)
    {
        var fireplaces = asset.GetComponentsInChildren<Fireplace>();
        if (fireplaces != null && fireplaces.Length > 0)
        {
            foreach (var fireplace in fireplaces)
            {
                FixEffect(fireplace.m_fuelAddedEffects, asset.name);
            }
        }
    }

    private static void FixPickable(GameObject asset)
    {
        var pickable = asset.GetComponentsInChildren<Pickable>();
        if (pickable != null && pickable.Length > 0)
        {
            foreach (var pickable1 in pickable)
            {
                var prefab = ZNetScene.instance.GetPrefab(pickable1.GetPrefabName());
                if (prefab)
                {
                    var pickableOrig = prefab.GetComponent<Pickable>();
                    pickable1.m_itemPrefab = pickableOrig.m_itemPrefab;
                    pickable1.m_amount = pickableOrig.m_amount;
                    pickable1.m_extraDrops = pickableOrig.m_extraDrops;
                    pickable1.m_overrideName = pickableOrig.m_overrideName;
                    pickable1.m_respawnTimeMinutes = pickableOrig.m_respawnTimeMinutes;
                    pickable1.m_spawnOffset = pickableOrig.m_spawnOffset;
                    pickable1.m_pickEffector = pickableOrig.m_pickEffector;
                    pickable1.m_pickEffectAtSpawnPoint = pickableOrig.m_pickEffectAtSpawnPoint;
                    pickable1.m_useInteractAnimation = pickableOrig.m_useInteractAnimation;
                    pickable1.m_tarPreventsPicking = pickableOrig.m_tarPreventsPicking;
                    pickable1.m_aggravateRange = pickableOrig.m_aggravateRange;
                }
            }
        }
    }

    private static void FixDestructible(GameObject asset)
    {
        var destructibles = asset.GetComponentsInChildren<Destructible>();
        if (destructibles != null && destructibles.Length > 0)
        {
            foreach (var destructible in destructibles)
            {
                var prefab = ZNetScene.instance.GetPrefab(destructible.GetPrefabName());
                var destructibleOrig = prefab.GetComponent<Destructible>();
                destructible.m_destroyedEffect = destructibleOrig.m_destroyedEffect;
                destructible.m_hitEffect = destructibleOrig.m_hitEffect;
                destructible.m_spawnWhenDestroyed = destructibleOrig.m_spawnWhenDestroyed;
            }
        }
    }

    private static void FixDropOnDestroyed(GameObject asset)
    {
        var all = asset.GetComponentsInChildren<DropOnDestroyed>();
        if (all != null && all.Length > 0)
        {
            foreach (var obj in all)
            {
                var orig = ZNetScene.instance.GetPrefab(obj.GetPrefabName()).GetComponent<DropOnDestroyed>();
                obj.m_dropWhenDestroyed = orig.m_dropWhenDestroyed;
            }
        }
    }

    private static void FixTree(GameObject asset)
    {
        var all = asset.GetComponentsInChildren<TreeBase>();
        if (all != null && all.Length > 0)
        {
            foreach (var obj in all)
            {
                var orig = ZNetScene.instance.GetPrefab(obj.GetPrefabName()).GetComponent<TreeBase>();
                obj.m_destroyedEffect = orig.m_destroyedEffect;
                obj.m_hitEffect = orig.m_hitEffect;
                obj.m_respawnEffect = orig.m_respawnEffect;
                obj.m_stubPrefab = orig.m_stubPrefab;
                obj.m_logPrefab = orig.m_logPrefab;
                obj.m_dropWhenDestroyed = orig.m_dropWhenDestroyed;
            }
        }
    }

    internal static void FixCharacter(GameObject asset)
    {
        var all = asset.GetComponentsInChildren<Character>();
        if (all != null && all.Length > 0)
        {
            foreach (var obj in all)
            {
                FixEffect(obj.m_deathEffects, obj.name);
                FixEffect(obj.m_hitEffects, obj.name);
                FixEffect(obj.m_jumpEffects, obj.name);
                FixEffect(obj.m_slideEffects, obj.name);
                FixEffect(obj.m_tarEffects, obj.name);
                FixEffect(obj.m_waterEffects, obj.name);
                FixEffect(obj.m_backstabHitEffects, obj.name);
                FixEffect(obj.m_critHitEffects, obj.name);
                FixEffect(obj.m_flyingContinuousEffect, obj.name);
            }
        }
    }

    internal static void FixNPC(GameObject asset)
    {
        var brain = asset.GetComponent<NPC_Brain>();
        if (brain != null)
        {
            FixEffect(brain.m_sootheEffect, asset.name);
            FixEffect(brain.m_wakeupEffects, asset.name);
            FixEffect(brain.m_alertedEffects, asset.name);
        }
    }

    internal static void FixWearNTear(GameObject asset)
    {
        var all = asset.GetComponentsInChildren<WearNTear>();
        if (all != null && all.Length > 0)
        {
            foreach (var obj in all)
            {
                var orig = ZNetScene.instance.GetPrefab(obj.GetPrefabName()).GetComponent<WearNTear>();
                FixEffect(obj.m_destroyedEffect, orig.name);
                FixEffect(obj.m_hitEffect, orig.name);
                FixEffect(obj.m_switchEffect, orig.name);
            }
        }
    }

    internal static void FixPiece(GameObject asset)
    {
        var all = asset.GetComponentsInChildren<Piece>();
        if (all != null && all.Length > 0)
        {
            foreach (var obj in all)
            {
                FixPiece(obj);
            }
        }
    }

    internal static void FixPiece(Piece piece)
    {
        if (piece != null)
        {
            FixEffect(piece.m_placeEffect, piece.name);
            var prefab = ZNetScene.instance.GetPrefab(piece.GetPrefabName());
            if (prefab)
            {
                var pieceOrig = prefab.GetComponent<Piece>();
                piece.m_icon = pieceOrig.m_icon;
                piece.m_craftingStation = pieceOrig.m_craftingStation;
                piece.m_blockingPieces = pieceOrig.m_blockingPieces;
                piece.m_placeEffect = pieceOrig.m_placeEffect;
                piece.m_resources = pieceOrig.m_resources;
                piece.m_destroyedLootPrefab = pieceOrig.m_destroyedLootPrefab;


                var extensionOrig = prefab.GetComponent<StationExtension>();
                if (extensionOrig)
                {
                    var extension = piece.GetComponent<StationExtension>();
                    extension.m_craftingStation = extensionOrig.m_craftingStation;
                    extension.m_connectionPrefab = extensionOrig.m_connectionPrefab;
                }

                var craftingStationOrig = prefab.GetComponent<CraftingStation>();
                if (craftingStationOrig)
                {
                    var craftingStation = piece.GetComponent<CraftingStation>();
                    craftingStation.m_icon = craftingStationOrig.m_icon;
                    craftingStation.m_craftItemEffects = craftingStationOrig.m_craftItemEffects;
                    craftingStation.m_craftItemDoneEffects = craftingStationOrig.m_craftItemDoneEffects;
                    craftingStation.m_repairItemDoneEffects = craftingStationOrig.m_repairItemDoneEffects;
                }
            }
        }
    }

    private static void FixRenderers(GameObject asset)
    {
        if (!asset) return;
        var renderers = asset.GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) return;

        foreach (Renderer? renderer in renderers)
        {
            if (!renderer || renderer.name == "HammerMark") continue;
            foreach (Material? material in renderer.sharedMaterials)
            {
                if (!material) continue;
                var shader = material.shader;
                if (!shader) return;
                string name = shader.name;
                material.shader = Shader.Find(name);
            }
        }
    }

    private static void FixEffect(EffectList effectList, string objName)
    {
        if (effectList == null || effectList.m_effectPrefabs == null || effectList.m_effectPrefabs.Length == 0) return;
        foreach (EffectList.EffectData effectData in effectList.m_effectPrefabs)
        {
            if (effectData == null) continue;
            if (!effectData.m_prefab == null)
            {
                DebugError($"No prefab found for place effect of {objName}", true);
                continue;
            }

            FixRenderers(effectData.m_prefab);
        }
    }
}