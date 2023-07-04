using System;
using System.Collections.Generic;
using UnityEngine;
using static NPCsSystem.CrafterItem;

namespace NPCsSystem
{
    [CreateAssetMenu(fileName = "NPC_Profile", menuName = "NPC_Profile", order = 0)]
    public class NPC_Profile : ScriptableObject
    {
        [Header("MPC settings")] public GameObject m_prefab;

        [Tooltip("If set, it will automatically get the prefab and overwrite it.")]
        public string prefabByName;

        //[Range(0f, 1f)] public float m_chance = 1;
        public NPC_Profession m_profession;
        public NPC_Gender m_gender;

        [Header("Profession settings:")] [Space] [Header("Crafter")]
        public List<CrafterItem> itemsToCraft = new();
        public int startinglevel = 1;

        [Header("Traider")] public List<BuyItem> itemsToBuy = new();
        public List<CrafterItem.SellItem> itemsToSell = new();

        [Header("Warrior")] public List<string> startWeapons = new();


        public override string ToString()
        {
            return
                $"{nameof(m_prefab)}: {m_prefab.name}, {nameof(name)}: {name}, {nameof(m_profession)}: {m_profession}";
        }

        public bool IsWarrior() => m_profession == NPC_Profession.Warrior;
    }
}