using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NPCsSystem
{
    [Serializable, HideInInspector]
    public class TradeItem
    {
        public static List<TradeItem> all = new();

        internal NPC_Profile npc;
        internal ItemDrop prefab;
        internal ItemDrop moneyItem;
        internal List<string> npcNames;
        internal string prefabName;
        internal string moneyItemName;
        internal int price = 100;
        internal string m_requiredGlobalKey;
        internal int lovePoints = 1;
        internal int stack = 1;
        private string npcNames_;

        public TradeItem(string npcNames, string prefabName, int price, string moneyItemName = "Coins",
            string globalKey = "",
            int lovePoints = 0, int stack = 1)
        {
            this.npcNames_ = npcNames;
            this.npcNames = npcNames.Split(',').ToList();
            this.prefabName = prefabName;
            this.moneyItemName = moneyItemName;
            this.price = price;
            this.m_requiredGlobalKey = globalKey;
            this.lovePoints = lovePoints;
            this.stack = stack;

            all.Add(this);
        }

        public override string ToString()
        {
            return
                $"NpcNames: {npcNames_}, " +
                $"PrefabName: {prefabName}, " +
                $"MoneyItemName: {moneyItemName}, " +
                $"Price: {price}, " +
                $"GlobalKey: {m_requiredGlobalKey}, " +
                $"LovePoints: {lovePoints}, " +
                $"Stack: {stack}";
        }
    }
}