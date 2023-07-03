using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static NPCsSystem.Plugin;

namespace NPCsSystem
{
    [RequireComponent(typeof(ZNetView))]
    public class NPC_Town : MonoBehaviour
    {
        public static HashSet<NPC_Town> towns = new HashSet<NPC_Town>();

        //private Location m_location;
        private ZNetView m_view;

        [Header("Settings")] public List<NPC_Profile> npcs = new List<NPC_Profile>();
        public HashSet<NPC_House> houses = new HashSet<NPC_House>();
        public float radius;
        [SerializeField] private int housesNeded = 5;

        private void Awake()
        {
            towns.Add(this);
            SetReferenses();
            //FindHouses();
            //TrySpawnNpcs();
        }

        private void OnDestroy()
        {
            towns.Remove(this);
        }

        private void TrySpawnNpcs()
        {
            var zdo = m_view.GetZDO();
            if (zdo.GetBool("NPCsSpawned", false)) return;
            zdo.Set("NPCsSpawned", true);

            Debug("Spawning NPCs");
            for (var i = 0; i < npcs.Count; i++)
            {
                if (i >= houses.Count) continue;
                var profile = npcs[i];
                NPC_Brain npc = null;
                var house = FindHouseForNPC(profile);
                if (!house)
                {
                    DebugError($"Can't find house for {profile}");
                    continue;
                }

                npc = Instantiate(profile.m_prefab, house.GetBedPos(), Quaternion.identity).GetComponent<NPC_Brain>();
                npc.SetHouse(house);
                npc.Init(profile);
                house.RegisterNPC(npc);
            }

            Debug("NPCs spawned");
        }

        private NPC_House FindHouseForNPC(NPC_Profile profile)
        {
            foreach (var house in houses)
            {
                if (!house.isAvailable) continue;
                switch (house.houseType)
                {
                    case HouseType.None:
                        break;
                    case HouseType.Housing:
                        if (profile.m_profession == NPC_Profession.None) return house;
                        break;
                    case HouseType.ProfessionHouse:
                        if (profile.m_profession == house.professionForProfessionHouse) return house;
                        break;
                }
            }

            return null;
        }


        private void OnValidate()
        {
        }

        private void Reset()
        {
            SetReferenses();
        }

        private void SetReferenses()
        {
            if (!m_view)
                m_view = GetComponent<ZNetView>();

            if (!m_view) DebugError($"[NPCsSystem] Can't find ZNetView component on {gameObject.name}");

            if (m_view)
            {
                m_view.m_persistent = true;
                m_view.m_type = ZDO.ObjectType.Default;
            }
        }

        public static NPC_Town FindTown(Vector3 position)
        {
            foreach (var town in towns)
            {
                if (Utils.DistanceXZ(town.transform.position, position) >= town.GetRadius()) continue;
                return town;
            }

            return null;
        }

        public float GetRadius() => radius;

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.gray;
            Gizmos.matrix = Matrix4x4.TRS(this.transform.position + new Vector3(0.0f, -0.01f, 0.0f),
                Quaternion.identity, new Vector3(1f, 1f / 1000f, 1f));
            Gizmos.DrawSphere(Vector3.zero, GetRadius());
            //Utils.DrawGizmoCircle(this.transform.position, this.m_noBuildRadiusOverride, 32);
            Gizmos.matrix = Matrix4x4.identity;
            Utils.DrawGizmoCircle(this.transform.position, GetRadius(), 32);
        }

        public void RegisterHouse(NPC_House house)
        {
            houses.Add(house);
            house.Init(this);
            if (houses.Count == housesNeded)
            {
                Debug("Initing town");
                TrySpawnNpcs();
            }
        }
    }
}