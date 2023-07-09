using System;
using System.Collections.Generic;
using UnityEngine;
using static NPCsSystem.CrafterItem;

namespace NPCsSystem
{
    [CreateAssetMenu(fileName = "NPC_Profile", menuName = "NPC_Profile")]
    public class NPC_Profile : ScriptableObject
    {
        [Header("MPC settings")] public GameObject m_prefab;

        [Tooltip("If set, it will automatically get the prefab and overwrite it.")]
        public string prefabByName;

        //[Range(0f, 1f)] public float m_chance = 1;
        public NPC_Profession m_profession;
        public NPC_Gender m_gender;

        public List<CrafterItem> itemsToCraft = new();
        public int startinglevel = 1;
        public List<TradeItem.BuyItem> itemsToBuy = new();
        public List<TradeItem.SellItem> itemsToSell = new();

        [Header("Warrior")] [SerializeField] public string[] startWeapons;


        public List<string> talksClearWeather_Day = new()
        {
            "$npcTalk_ClearWeather_Day1",
            "$npcTalk_ClearWeather_Day2",
            "$npcTalk_ClearWeather_Day3",
            "$npcTalk_ClearWeather_Day4",
            "$npcTalk_ClearWeather_Day5",
            "$npcTalk_ClearWeather_Day6",
            "$npcTalk_ClearWeather_Day7",
            "$npcTalk_ClearWeather_Day8",
            "$npcTalk_ClearWeather_Day9",
            "$npcTalk_ClearWeather_Day10"
        };

        public List<string> talksClearWeather_Night = new()
        {
            "$npcTalk_ClearWeather_Night1",
            "$npcTalk_ClearWeather_Night2",
            "$npcTalk_ClearWeather_Night3",
            "$npcTalk_ClearWeather_Night4",
            "$npcTalk_ClearWeather_Night5",
            "$npcTalk_ClearWeather_Night6",
            "$npcTalk_ClearWeather_Night7",
            "$npcTalk_ClearWeather_Night8",
            "$npcTalk_ClearWeather_Night9",
            "$npcTalk_ClearWeather_Night10"
        };

        public List<string> talksBadWeather_Day = new()
        {
            "$npcTalk_BadWeather_Day1",
            "$npcTalk_BadWeather_Day2",
            "$npcTalk_BadWeather_Day3",
            "$npcTalk_BadWeather_Day4",
            "$npcTalk_BadWeather_Day5",
            "$npcTalk_BadWeather_Day6",
            "$npcTalk_BadWeather_Day7",
            "$npcTalk_BadWeather_Day8",
            "$npcTalk_BadWeather_Day9",
            "$npcTalk_BadWeather_Day10"
        };

        public List<string> talksBadWeather_Night = new()
        {
            "$npcTalk_BadWeather_Night1",
            "$npcTalk_BadWeather_Night2",
            "$npcTalk_BadWeather_Night3",
            "$npcTalk_BadWeather_Night4",
            "$npcTalk_BadWeather_Night5",
            "$npcTalk_BadWeather_Night6",
            "$npcTalk_BadWeather_Night7",
            "$npcTalk_BadWeather_Night8",
            "$npcTalk_BadWeather_Night9",
            "$npcTalk_BadWeather_Night10"
        };


        public override string ToString()
        {
            return
                $"{nameof(m_prefab)}: {m_prefab.name}, {nameof(name)}: {name}, {nameof(m_profession)}: {m_profession}";
        }

        public bool IsWarrior() => m_profession == NPC_Profession.Warrior;
        public bool IsCrafter() => m_profession == NPC_Profession.Crafter;


        public void AddCrafterItem(CrafterItem item)
        {
            itemsToCraft.Add(item);
        }

        public void AddCrafterItem(string item)
        {
            if (m_profession != NPC_Profession.Crafter)
            {
                Plugin.DebugError($"{name} is not a Crafter! He cant craft items.");
                return;
            }

            itemsToCraft.Add(new(item));
        }

        public bool HasProfession() => m_profession != null && m_profession != NPC_Profession.None;
    }
}