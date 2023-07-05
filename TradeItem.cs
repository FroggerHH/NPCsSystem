using System;
using System.Collections.Generic;
using UnityEngine;

namespace NPCsSystem
{
    [Serializable, HideInInspector]
    public class TradeItem
    {
        [HideInInspector] internal ItemDrop prefab;
        public string prefabName;
        public int m_priceForItem = 100;
        public string m_requiredGlobalKey;
        public int lovePoints = 1;


        [Serializable]
        public class BuyItem : TradeItem
        {
        }

        [Serializable]
        public class SellItem : TradeItem
        {
            public int m_stack = 1;
        }
    }

    [Serializable, HideInInspector]
    public class CrafterItem
    {
        public CrafterItem(string prefabName, string m_requiredGlobalKey, int minLevelToMake, int minLevelToUpgrade, int maxQuantity)
        {
            this.prefabName = prefabName;
            this.m_requiredGlobalKey = m_requiredGlobalKey;
            this.minLevelToMake = minLevelToMake;
            this.minLevelToUpgrade = minLevelToUpgrade;
            this.maxQuantity = maxQuantity;
        }
        public CrafterItem(string prefabName)
        {
            this.prefabName = prefabName;
        }

        [HideInInspector] internal ItemDrop prefab { get; set;}
        public string prefabName;
        public string m_requiredGlobalKey;
        public int minLevelToMake = 1;
        public int minLevelToUpgrade = 2;

        [Tooltip("Crafter can upgrade weapons to levels exceeding the maximum vanilla level")]
        public int maxQuantity = 10;

        [Serializable]
        public class CrafterUpgradeItem
        {
            public int toQuantity = 2;
            public int minLevel = 2;
        }
    }
}