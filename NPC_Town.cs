using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JustAFrogger;
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
        internal ZNetView m_view;

        [Header("Settings")] public List<NPC_Profile> npcs = new List<NPC_Profile>();
        public HashSet<NPC_House> houses = new HashSet<NPC_House>();
        public float radius;
        [SerializeField] private int housesNeded = 5;
        internal List<Request> requests = new();
        internal Action onRequestsChanged;

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
            //MainInit();
        }

        private void OnDestroy()
        {
            towns.Remove(this);
        }

        private void MainInit()
        {
            TrySpawnNpcs();
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
                if (!sleepHouse)
                {
                    DebugWarning($"Can't find sleepHouse for {profile}");
                    continue;
                }

                npc = Instantiate(profile.m_prefab, sleepHouse.GetBedPos(profile.name), Quaternion.identity)
                    .GetComponent<NPC_Brain>();
                npc.SetHouse(sleepHouse, null);
                npc.Init(profile);
            }

            Debug("NPCs spawned");
        }

        internal NPC_House FindWorkHouse(NPC_Profile profile)
        {
            List<NPC_House> returnHouses = new();

            foreach (var house in houses)
            {
                var ht = house.GetHouseType();
                if (!house.IsAvailable() || ht == None) continue;
                if (house.IsProfessionHouse())
                {
                    if (profile.m_profession == house.professionForProfessionHouse) returnHouses.Add(house);
                }
            }

            return returnHouses.FirstOrDefault();
        }

        internal NPC_House FindEntertainmentHouse()
        {
            List<NPC_House> returnHouses = new();

            foreach (var house in houses)
            {
                var ht = house.GetHouseType();
                if (!house.IsAvailable() || ht == None) continue;
                if (house.IsEntertainmentHouse())
                {
                    returnHouses.Add(house);
                }
            }

            return returnHouses.FirstOrDefault();
        }

        private NPC_House FindSleepHouse(NPC_Profile profile)
        {
            List<NPC_House> returnHouses = new();

            foreach (var house in houses)
            {
                var ht = house.GetHouseType();
                if (!house.IsAvailable() || ht == None) continue;

                if (house.IsHousingHouse())
                {
                    returnHouses.Add(house);
                }
            }

            return returnHouses.Find(x => x.IsAvailable());
        }

        public NPC_House FindWarehouse(List<(ItemDrop.ItemData.SharedData, int)> items)
        {
            List<NPC_House> returnHouses = new();

            foreach (var house in houses)
            {
                var itemsCheck = true;
                if (house.IsWarehouse())
                {
                    var houseInventory = house.GetHouseInventory();
                    if (items != null)
                    {
                        foreach (var item in items)
                        {
                            if (!houseInventory.Exists(x =>
                                    x.m_shared.m_name == item.Item1.m_name && x.m_stack >= item.Item2))
                            {
                                itemsCheck = false;
                                continue;
                            }
                        }

                        if (!itemsCheck) continue;
                    }

                    returnHouses.Add(house);
                }
            }

            return returnHouses.FirstOrDefault();
        }

        internal List<NPC_House> FindFoodHouses()
        {
            List<NPC_House> returnHouses = new();
            foreach (var house in houses)
            {
                var ht = house.GetHouseType();
                if (ht == None) continue;
                if (house.IsWarehouse() && house.HaveFood())
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
                MainInit();
                InitAllNPCs();
            }
        }

        private void InitAllNPCs()
        {
            foreach (var npc in NPC_Brain.allNPCs)
            {
                npc.workHouse = FindWorkHouse(npc.profile);
                if (!npc.workHouse && npc.profile.HasProfession() && npc.profile.m_profession != NPC_Profession.Builder)
                    DebugWarning($"Can't find workHouse for {npc.profile}");
            }
        }

        public WearNTear FindWornBuilding()
        {
            var wornBuildings = FindAllWornBuilding();

            return JFHelper.Nearest(wornBuildings, transform.position);
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

        public List<WearNTear> FindAllBuildings(NPC_House house = null)
        {
            var wearNTears = new List<WearNTear>();
            var buildings = PieceExtention.GetAllPiecesInRadius(house.transform.position, house ? house.GetRadius() : GetRadius());
            foreach (var piece in buildings)
            {
                var wearNTear = piece.GetComponent<WearNTear>();
                if (!wearNTear) continue;
                wearNTears.Add(wearNTear);
            }

            return wearNTears;
        }

        public bool RegisterNPCRequest(Request request)
        {
            var haveRequest = HaveRequest(request.requestType, request.npcName);
            if (haveRequest) return false;
            requests.Add(request);
            onRequestsChanged?.Invoke();
            return true;
        }

        public bool HaveRequest(RequestType requestType, string npcName)
        {
            return requests.Any(x =>
                x.npcName == npcName && x.requestType == requestType);
        }

        public List<Request> GetRequests() => requests;

        public void CompleteRequest(Request request_, bool emote = false)
        {
            var request = FindRequest(request_);
            if (request == null) return;
            Debug($"Complete request {request}");
            requests.Remove(request);
            onRequestsChanged?.Invoke();
            if (!emote) return;
            var npc = GetNPC(request.npcName);
            if (npc)
            {
                npc.Emote($"CompleteRequest {request}");
            }
        }

        internal Request FindRequest(Request request)
        {
            return requests.Find(x =>
                x.npcName == request.npcName && x.requestType == request.requestType &&
                x.thingName == request.thingName && x.items == request.items);
        }

        private NPC_Brain GetNPC(string npcName)
        {
            return NPC_Brain.allNPCs.ToList().Find(x => x.profile.name == npcName);
        }
    }
}