using System;
using System.Collections.Generic;
using UnityEngine;
using static Trader;

namespace NPCsSystem;

[Serializable]
public abstract class TradeItem
{
    [HideInInspector]
    internal ItemDrop prefab;
    public string prefabName;
    public int m_priceForItem = 100;
    public string m_requiredGlobalKey;
    public int lovePoints = 1;
}

[Serializable]
public class CrafterItem
{
    [HideInInspector]
    internal ItemDrop prefab;
    public string prefabName;
    public string m_requiredGlobalKey;
    public int minLevelToMake = 1;
    public int minLevelToUpgrade = 2;

    [Tooltip("Crafter can upgrade weapons to levels exceeding the maximum vanilla level")]
    public int maxQuantity = 10;

    public class CrafterUpgradeItem
    {
        public int toQuantity = 2;
        public int minLevel = 2;
    }


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