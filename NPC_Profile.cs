using UnityEngine;

namespace NPCsSystem
{
    [CreateAssetMenu(fileName = "NPC_Profile", menuName = "NPC_Profile", order = 0)]
    public class NPC_Profile : ScriptableObject
    {
        public GameObject m_prefab;
        [Tooltip("If set, it will automatically get the prefab and overwrite it.")]public string prefabByName;
        public string m_name = "name";
        [Range(0f, 1f)] public float m_chance = 1;
        public NPC_Profession m_profession;
        public NPC_Gender m_gender;

        public override string ToString()
        {
            return $"{nameof(m_prefab)}: {m_prefab.name}, {nameof(m_name)}: {m_name}, {nameof(m_profession)}: {m_profession}";
        }
    }
}