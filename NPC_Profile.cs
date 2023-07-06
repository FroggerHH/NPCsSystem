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