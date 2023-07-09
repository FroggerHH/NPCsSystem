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
    public static void NPCsSystemFixMaterials()
    {
        FixSmt(bundle.LoadAsset<GameObject>("NPSHouseCrafter"));
        FixSmt(bundle.LoadAsset<GameObject>("NPSHouseHotel"));
        FixSmt(bundle.LoadAsset<GameObject>("NPSHouseWarehouse"));
        FixSmt(bundle.LoadAsset<GameObject>("NPSPark"));
        FixSmt(bundle.LoadAsset<GameObject>("PlayerNPS"));
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
        FixInstanceRenderer(child);
        FixPiece(child);
        FixWearNTear(child);
        FixCharacter(child);
        FixDestructible(child);
        FixPickable(child);
        FixContainer(child);
        FixFireplace(child);
        FixTerrainMod(child);
        FixNPC(child);
        FixTree(child);
        FixDropOnDestroyed(child);
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
            var prefab = ZNetScene.instance.GetPrefab(asset.GetPrefabName());
            if (prefab)
            {
                var pickableOrig = prefab.GetComponent<Pickable>();
                pickable.m_itemPrefab = pickableOrig.m_itemPrefab;
                pickable.m_amount = pickableOrig.m_amount;
                pickable.m_extraDrops = pickableOrig.m_extraDrops;
                pickable.m_overrideName = pickableOrig.m_overrideName;
                pickable.m_respawnTimeMinutes = pickableOrig.m_respawnTimeMinutes;
                pickable.m_spawnOffset = pickableOrig.m_spawnOffset;
                pickable.m_pickEffector = pickableOrig.m_pickEffector;
                pickable.m_pickEffectAtSpawnPoint = pickableOrig.m_pickEffectAtSpawnPoint;
                pickable.m_useInteractAnimation = pickableOrig.m_useInteractAnimation;
                pickable.m_tarPreventsPicking = pickableOrig.m_tarPreventsPicking;
                pickable.m_aggravateRange = pickableOrig.m_aggravateRange;
            }
        }
    }

    private static void FixDestructible(GameObject asset)
    {
        var destructible = asset.GetComponent<Destructible>();
        if (destructible != null)
        {
            var prefab = ZNetScene.instance.GetPrefab(asset.GetPrefabName());
            if (prefab)
            {
                var destructibleOrig = prefab.GetComponent<Destructible>();
                destructible.m_destroyedEffect = destructibleOrig.m_destroyedEffect;
                destructible.m_hitEffect = destructibleOrig.m_hitEffect;
                destructible.m_spawnWhenDestroyed = destructibleOrig.m_spawnWhenDestroyed;
            }
        }
    }

    private static void FixDropOnDestroyed(GameObject asset)
    {
        var dropOnDestroyed = asset.GetComponent<DropOnDestroyed>();
        if (dropOnDestroyed != null)
        {
            var prefab = ZNetScene.instance.GetPrefab(asset.GetPrefabName());
            if (prefab)
            {
                var dropOnDestroyedOrig = prefab.GetComponent<DropOnDestroyed>();
                dropOnDestroyed.m_dropWhenDestroyed = dropOnDestroyedOrig.m_dropWhenDestroyed;
            }
        }
    }

    private static void FixTree(GameObject asset)
    {
        var treeBase = asset.GetComponent<TreeBase>();
        if (treeBase != null)
        {
            var prefab = ZNetScene.instance.GetPrefab(asset.GetPrefabName());
            if (prefab)
            {
                var treeBaseOrig = prefab.GetComponent<TreeBase>();
                treeBase.m_destroyedEffect = treeBaseOrig.m_destroyedEffect;
                treeBase.m_hitEffect = treeBaseOrig.m_hitEffect;
                treeBase.m_respawnEffect = treeBaseOrig.m_respawnEffect;
                treeBase.m_stubPrefab = treeBaseOrig.m_stubPrefab;
                treeBase.m_logPrefab = treeBaseOrig.m_logPrefab;
                treeBase.m_dropWhenDestroyed = treeBaseOrig.m_dropWhenDestroyed;
            }
        }
    }

    internal static void FixCharacter(GameObject asset)
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
        var wearNTear = asset.GetComponent<WearNTear>();
        if (wearNTear != null)
        {
            FixEffect(wearNTear.m_destroyedEffect, asset.name);
            FixEffect(wearNTear.m_hitEffect, asset.name);
            FixEffect(wearNTear.m_switchEffect, asset.name);
        }
    }

    internal static void FixPiece(GameObject asset)
    {
        var piece = asset.GetComponent<Piece>();
        FixPiece(piece);
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
                    DebugError($"No material found for InstanceRenderer {renderer.name}", true);
                    continue;
                }

                renderer.m_material.shader = Shader.Find(renderer.m_material.shader.name);
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