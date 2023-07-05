using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static NPCsSystem.Plugin;
using static NPCsSystem.HouseType;

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

        private void Reset()
        {
            SetReferenses();
        }

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
                var house = FindHouse(profile);
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

        private NPC_House FindHouse(NPC_Profile profile)
        {
            List<NPC_House> returnHouses = new();

            foreach (var house in houses)
            {
                var ht = house.houseType;
                if (!house.isAvailable || ht == None) continue;
                // switch (house.houseType)
                // {
                //     case None:
                //         break;
                //     case Housing:
                //         if (profile.m_profession == NPC_Profession.None) return house;
                //         break;
                //     case HouseType.ProfessionHouse:
                //         if (profile.m_profession == house.professionForProfessionHouse) return house;
                //         break;
                // }
                if (house.IsProfessionHouse())
                {
                    if (profile.m_profession == house.professionForProfessionHouse) returnHouses.Add(house);
                }

                if (house.IsHousingHouse() && profile.m_profession == NPC_Profession.None)
                {
                    returnHouses.Add(house);
                }
            }

            // var arr = returnHouses.ToArray();
            // Array.Sort(arr, DistanceComparison);
            // returnHouses = arr.ToList();
            // if (returnHouses.Count == 0) return null;
            return returnHouses.FirstOrDefault();
        }

        internal List<NPC_House> FindFoodHouses()
        {
            List<NPC_House> returnHouses = new();
            foreach (var house in houses)
            {
                var ht = house.houseType;
                if (ht == None) continue;
                if (house.IsFoodHouse() && house.HaveFood())
                {
                    returnHouses.Add(house);
                }
            }

            return returnHouses;
        }

        int DistanceComparison(NPC_House a, NPC_House b)
        {
            if (a == null) return (b == null) ? 0 : -1;
            if (b == null) return 1;

            var distanceA = (a.transform.position - this.transform.position).sqrMagnitude;
            var distanceB = (b.transform.position - this.transform.position).sqrMagnitude;
            return distanceA.CompareTo(distanceB);
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

        public WearNTear FindWornBuilding()
        {
            var wornBuildings = FindAllWornBuilding();

            return Helper.Nearest(wornBuildings, transform.position);
        }

        public List<WearNTear> FindAllWornBuilding()
        {
            var wearNTears = new List<WearNTear>();
            var buildings = FindAllBuilding();
            foreach (var wearNTear in buildings)
            {
                if (wearNTear.GetHealthPercentage() < 0.8f)
                {
                    wearNTears.Add(wearNTear);
                }
            }

            return wearNTears;
        }

        public List<WearNTear> FindAllBuilding()
        {
            var buildings = new List<Piece>();
            var wearNTears = new List<WearNTear>();
            Piece.GetAllPiecesInRadius(transform.position, GetRadius(), buildings);
            foreach (var piece in buildings)
            {
                var wearNTear = piece.GetComponent<WearNTear>();
                if (!wearNTear) continue;
                wearNTears.Add(wearNTear);
            }

            return wearNTears;
        }
    }
}