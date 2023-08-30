using System;
using UnityEngine;

namespace NPCsSystem;

[Serializable]
[HideInInspector]
public class CrafterItem
{
    public string prefabName;
    public string m_requiredGlobalKey;
    public int minLevelToMake = 1;
    public int minLevelToUpgrade = 2;
    public int maxCountToCraft;

    [Tooltip("Crafter can upgrade weapons to levels exceeding the maximum vanilla level")]
    public int maxQuantity = 10;

    [HideInInspector] internal ItemDrop prefab;
    [HideInInspector] internal Recipe recipe;

    public CrafterItem(string prefabName, string m_requiredGlobalKey, int minLevelToMake, int minLevelToUpgrade,
        int maxQuantity)
    {
        this.prefabName = prefabName;
        this.m_requiredGlobalKey = m_requiredGlobalKey;
        this.minLevelToMake = minLevelToMake;
        this.minLevelToUpgrade = minLevelToUpgrade;
        this.maxQuantity = maxQuantity;
    }

    public CrafterItem(string prefabName, int maxCountToCraft = 1)
    {
        this.prefabName = prefabName;
        this.maxCountToCraft = maxCountToCraft;
    }


    [Serializable]
    public class CrafterUpgradeItem
    {
        public int toQuantity = 2;
        public int minLevel = 2;
    }
}