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
            foreach (var profile in npcs)
            {
                NPC_Brain npc = null;
                var sleepHouse = FindSleepHouse(profile);
                var workHouse = FindWorkHouse(profile);
                if (!sleepHouse)
                {
                    DebugWarning($"Can't find sleepHouse for {profile}");
                    continue;
                }

                if (!workHouse && profile.HasProfession())
                {
                    DebugWarning($"Can't find workHouse for {profile}");
                    continue;
                }

                npc = Instantiate(profile.m_prefab, sleepHouse.GetBedPos(profile.name), Quaternion.identity)
                    .GetComponent<NPC_Brain>();
                npc.SetHouse(sleepHouse, workHouse);
                npc.Init(profile);
            }

            Debug("NPCs spawned");
        }

        internal NPC_House FindWorkHouse(NPC_Profile profile)
        {
            List<NPC_House> returnHouses = new();

            foreach (var house in houses)
            {
                var ht = house.houseType;
                if (!house.IsAvailable() || ht == None) continue;
                if (house.IsProfessionHouse())
                {
                    if (profile.m_profession == house.professionForProfessionHouse) returnHouses.Add(house);
                }
            }

            return returnHouses.FirstOrDefault();
        }

        private NPC_House FindSleepHouse(NPC_Profile profile)
        {
            List<NPC_House> returnHouses = new();

            foreach (var house in houses)
            {
                var ht = house.houseType;
                if (!house.IsAvailable() || ht == None) continue;

                if (house.IsHousingHouse())
                {
                    returnHouses.Add(house);
                }
            }

            return returnHouses.Find(x => x.IsAvailable());
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
            var buildings = FindAllBuildings();
            foreach (var wearNTear in buildings)
            {
                if (wearNTear.GetHealthPercentage() < 0.8f)
                {
                    wearNTears.Add(wearNTear);
                }
            }

            return wearNTears;
        }

        public List<WearNTear> FindAllBuildings()
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

        public List<WearNTear> FindAllBuildings(NPC_House house)
        {
            var buildings = new List<Piece>();
            var wearNTears = new List<WearNTear>();
            Piece.GetAllPiecesInRadius(house.transform.position, house.GetRadius(), buildings);
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