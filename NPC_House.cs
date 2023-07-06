using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using fastJSON;
using UnityEngine;
using static NPCsSystem.Plugin;
using static ItemDrop;
using static ItemDrop.ItemData;
using static NPCsSystem.HouseType;

namespace NPCsSystem
{
    [RequireComponent(typeof(ZNetView))]
    public class NPC_House : MonoBehaviour
    {
        internal static List<NPC_House> allHouses = new List<NPC_House>();

        //private Location m_location;
        internal ZNetView m_view;
        internal NPC_Town town;
        private Dictionary<string, Bed> beds = new();
        private Dictionary<Sign, Bed> bedSigns = new();
        private HashSet<Bed> bedObjs = new();
        private List<CraftingStation> craftingStations = new List<CraftingStation>();
        private List<Container> chests = new List<Container>();
        private List<Door> doors = new List<Door>();
        private List<Sign> signs = new List<Sign>();
        internal List<NPC_Brain> currentnpcs = new List<NPC_Brain>();

        public NPC_Profession professionForProfessionHouse;
        public int maxNPCs = 1;
        [SerializeField] private float radius = 10;
        public HouseType houseType = HouseType.Housing;

        public float GetRadius() => radius;

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


        private void Awake()
        {
            allHouses.Add(this);
            SetReferenses();
            StartCoroutine(RegisterHouse());
        }

        private void Start()
        {
            OpenAllDoors();
            FindAllBeds();
            FindAllChests();


            var signOrig = ZNetScene.instance.GetPrefab("Sign");

            foreach (var bed in bedObjs)
            {
                foreach (var sign in bed.GetComponentsInChildren<GameObject>())
                {
                    if (sign.name.StartsWith("sign_REPLACE_"))
                    {
                        var newSign = Instantiate(signOrig, sign.transform.position, sign.transform.rotation);
                        Destroy(sign.gameObject);
                        AddBedSign(newSign.GetComponent<Sign>(), bed);
                    }
                }
            }
        }

        private void AddBedSign(Sign sign, Bed bed)
        {
            if (bedSigns.ContainsKey(sign)) return;
            bedSigns.Add(sign, bed);
            MarkBedSigns();
        }

        private IEnumerator RegisterHouse()
        {
            yield return new WaitForSeconds(3f);
            var npcTown = NPC_Town.FindTown(transform.position);
            if (!npcTown) StartCoroutine(RegisterHouse());
            npcTown.RegisterHouse(this);
        }

        private void OnDestroy()
        {
            allHouses.Remove(this);
        }

        public void Init(NPC_Town town)
        {
            this.town = town;
            var pieces = new List<Piece>();
            Piece.GetAllPiecesInRadius(transform.position, GetRadius(), pieces);
            foreach (var piece in pieces)
            {
                if (piece.m_name == "$piece_bed")
                {
                    //SetBed(piece.GetComponent<Bed>());
                    continue;
                }

                if (piece.m_description == "$piece_craftingstation")
                {
                    AddCraftingStation(piece.GetComponent<CraftingStation>());
                    continue;
                }

                if (piece.TryGetComponent(out Container container))
                {
                    AddChest(container);
                    continue;
                }

                if (piece.TryGetComponent(out Door door))
                {
                    AddDoor(door);
                    continue;
                }
            }

            Load();
        }

        public static NPC_House FindHouse(Vector3 position)
        {
            NPC_Town town = null;
            foreach (var town_ in NPC_Town.towns)
            {
                if (Utils.DistanceXZ(town_.transform.position, position) >= town_.GetRadius()) continue;
                town = town_;
                break;
            }

            if (!town) return null;

            foreach (var house in allHouses)
            {
                if (Utils.DistanceXZ(house.transform.position, position) >= town.GetRadius()) continue;
                return house;
            }

            return null;
        }

        public static HashSet<NPC_House> FindAllHousesForTown(NPC_Town town)
        {
            var retList = new HashSet<NPC_House>();
            foreach (var house in allHouses)
            {
                if (Utils.DistanceXZ(house.transform.position, town.transform.position) >=
                    town.GetRadius() + house.GetRadius()) continue;
                retList.Add(house);
            }

            return retList;
        }

        //public float GetRadius() => m_location.GetMaxRadius();
        public void AddBed(Bed bed)
        {
            // StartCoroutine(DistributeBeds(bed));
            if (!bedObjs.Contains(bed)) bedObjs.Add(bed);
            DistributeBeds();
        }

        public void RemoveBed(Bed bed)
        {
            if (bedObjs.Contains(bed))
                bedObjs.Remove(bed);
            var newBeds = new Dictionary<string, Bed>();
            // StartCoroutine(DistributeBeds(bed));
            foreach (var bed1 in beds)
            {
                if (bed1.Value != null) newBeds.Add(bed1.Key, bed1.Value);
            }

            beds = newBeds;

            DistributeBeds();
        }

        // ReSharper disable Unity.PerformanceAnalysis
        private void DistributeBeds()
        {
            Debug($"Distributing beds, currentnpcs is {currentnpcs.Count}");
            if (currentnpcs.Count == 0) return;
            beds.Clear();
            foreach (var npc in currentnpcs)
            {
                var npcName = npc.profile.name;

                FindAllBeds();
                foreach (var bed in bedObjs)
                {
                    if (beds.ContainsValue(bed) || beds.ContainsKey(npcName)) continue;

                    beds.Add(npcName, bed);
                    bed.SetOwner(88, npcName);
                    MarkBedSigns();
                    continue;
                }
            }
        }

        private void MarkBedSigns()
        {
            foreach (var bedSign in bedSigns)
            {
                var sign = bedSign.Key;
                sign.m_nview.ClaimOwnership();
                sign.m_nview.GetZDO().Set(ZDOVars.s_text, bedSign.Value.GetOwnerName());
                sign.m_nview.GetZDO().Set(ZDOVars.s_author, PrivilegeManager.GetNetworkUserId());
                sign.UpdateText();
            }
        }

        // private bool IsFirsLoad()
        // {
        //     return m_view.GetZDO().GetBool("FirsLoad", true);
        // }

        public void AddCraftingStation(CraftingStation craftingStatione)
        {
            craftingStations.Add(craftingStatione);
        }

        public void RemoveCraftingStation(CraftingStation craftingStatione)
        {
            craftingStations.Remove(craftingStatione);
        }

        public List<CraftingStation> GetCraftingStations()
        {
            return craftingStations;
        }

        public void AddChest(Container container)
        {
            chests.Add(container);
        }

        public void RemoveChest(Container craftingStatione)
        {
            chests.Remove(craftingStatione);
        }

        public void AddDoor(Door door)
        {
            doors.Add(door);
        }

        public void RemoveDoor(Door door)
        {
            doors.Remove(door);
        }

        public void AddSign(Sign sign)
        {
            signs.Add(sign);
        }

        public void RemoveSign(Sign sign)
        {
            signs.Remove(sign);
        }


        public void RegisterNPC(NPC_Brain npc)
        {
            if (currentnpcs.Contains(npc)) return;
            currentnpcs.Add(npc);

            Save();
            DistributeBeds();
        }

        public Vector3 GetBedPos(string npcName)
        {
            var bed = GetBedFor(npcName);
            return bed ? bed.transform.position : transform.position;
        }

        private void Save()
        {
            string save = string.Empty;
            // var saveDatas = new List<NPC_SaveData>();
            // foreach (var npc in currentnpcs)
            // {
            //     saveDatas.Add(new(npc.GetNPC_ID(), npc.profile.name));
            // }

            var toSave = new string[currentnpcs.Count];
            for (var index = 0; index < currentnpcs.Count; index++)
            {
                var brain = currentnpcs[index];
                toSave[index] = brain.profile.name;
            }

            save = JSON.ToJSON(toSave);

            m_view.GetZDO().Set("save", save);
        }

        private void Load()
        {
            string save = m_view.GetZDO().GetString("save");
            if (string.IsNullOrEmpty(save)) return;
            var savedProfiles = JSON.ToObject<string[]>(save);
            if (savedProfiles == null || savedProfiles.Length == 0) return;

            StartCoroutine(LoadNPCsIEnumerator(savedProfiles));
        }

        private IEnumerator LoadNPCsIEnumerator(string[] savedProfiles)
        {
            yield return new WaitForSeconds(3f);
            LoadNPCs(savedProfiles);
        }

        private void LoadNPCs(string[] savedProfiles)
        {
            foreach (var savedProfile in savedProfiles)
            {
                var brain = NPC_Brain.allNPCs.ToList().Find(x => x.profile.name == savedProfile);

                brain.SetHouse(this, town.FindWorkHouse(brain.profile));
            }

            m_view.GetZDO().Set("FirsLoad", false);
        }

        private static void ShowHousesVisual(bool flag)
        {
            foreach (var house in allHouses)
            {
                Utils.FindChild(house.transform, "houseWalls").gameObject.SetActive(flag);
            }
        }

        internal bool IsProfessionHouse() => (houseType == ProfessionHouse || houseType == HousingAndProfessionHouse ||
                                              houseType == WarehouseAndProfessionHouse || houseType == All)
        //&& craftingStations.Count > 0
        ;

        internal bool IsFoodHouse() => (houseType == Warehouse || houseType == WarehouseAndProfessionHouse ||
                                        houseType == HousingAndWarehouse || houseType == All)
                                       && chests.Count > 0;

        public bool IsHousingHouse() => houseType == Housing || houseType == HousingAndWarehouse ||
                                        houseType == HousingAndProfessionHouse || houseType == All;

        public List<ItemData> GetHouseInventory()
        {
            List<ItemData> houseInventory = new();
            foreach (var chest in chests)
            {
                foreach (var item in chest.GetInventory().GetAllItems())
                {
                    houseInventory.Add(item);
                }
            }

            return houseInventory;
        }

        public bool RemoveItemFromInventory(string name)
        {
            foreach (var chest in chests)
            {
                var inventory = chest.GetInventory();
                if (!inventory.ContainsItemByName(name)) continue;

                var item = inventory.m_inventory.Find(x => x.m_shared.m_name == name);
                return inventory.RemoveOneItem(item);
            }

            return false;
        }

        public List<ItemData> GetItemsByType(ItemType type, out Container container)
        {
            container = null;
            var items = new List<ItemData>();
            foreach (var chest in chests)
            {
                foreach (var item in chest.GetInventory().GetAllItems())
                {
                    if (item.m_shared.m_itemType == type)
                    {
                        container = chest;
                        items.Add(item);
                    }
                }

                if (container) return items;
            }

            return items;
        }

        public bool HasBeds() => beds.Count > 0;

        public bool HasBedFor(string npcName)
        {
            return GetBedFor(npcName);
        }

        public Bed GetBedFor(string npcName)
        {
            if (beds.TryGetValue(npcName, out var bed)) return bed;
            else return null;
        }

        public bool AddItem(ItemDrop itemData)
        {
            foreach (var chest in chests)
            {
                if (chest.GetInventory().AddItem(itemData.gameObject, 1))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HaveEmptySlot()
        {
            foreach (var chest in chests)
            {
                if (chest.GetInventory().HaveEmptySlot())
                    return true;
            }

            return false;
        }

        public bool AddItem(string prefabName, int count = 1)
        {
            foreach (var chest in chests)
            {
                if (chest.GetInventory().AddItem(ZNetScene.instance.GetPrefab(prefabName), count))
                    return true;
            }

            return false;
        }

        public bool HaveFood()
        {
            return GetItemsByType(ItemDrop.ItemData.ItemType.Consumable, out Container _).Count > 0;
        }

        public void CloseAllDoors()
        {
            foreach (var door in doors)
            {
                door.m_nview.GetZDO().Set(ZDOVars.s_state, 0, false);
                door.UpdateState();
            }
        }

        public void OpenAllDoors()
        {
            foreach (var door in doors)
            {
                door.m_nview.GetZDO().Set(ZDOVars.s_state, 1, false);
                door.UpdateState();
            }
        }

        public void FindAllChests()
        {
            var children = town.FindAllBuildings(this);
            foreach (var child in children)
            {
                if (child.TryGetComponent(out Container chest))
                {
                    chests.Add(chest);
                }
            }
        }

        public bool IsAvailable() => currentnpcs.Count < maxNPCs;

        public void FindAllBeds()
        {
            var children = town.FindAllBuildings(this);
            foreach (var child in children)
            {
                if (child.TryGetComponent(out Bed bed))
                {
                    bedObjs.Add(bed);
                }
            }
        }
    }
}