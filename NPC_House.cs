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
        private Bed bed;
        private List<CraftingStation> craftingStations = new List<CraftingStation>();
        private List<Container> chests = new List<Container>();
        internal bool isAvailable = true;
        internal List<NPC_Brain> currentnpcs = new();

        public NPC_Profession professionForProfessionHouse;
        public int maxNPCs = 1;
        [SerializeField] private float radius = 10;
        public HouseType houseType = HouseType.Housing;

        private void Awake()
        {
            allHouses.Add(this);
            SetReferenses();
            StartCoroutine(RegisterHouse());
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

        public void Init(NPC_Town town)
        {
            this.town = town;
            var pieces = new List<Piece>();
            Piece.GetAllPiecesInRadius(transform.position, GetRadius(), pieces);
            foreach (var piece in pieces)
            {
                if (piece.m_name == "$piece_bed")
                {
                    SetBed(piece.GetComponent<Bed>());
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
        public float GetRadius() => radius;

        public void SetBed(Bed bed)
        {
            this.bed = bed;
        }

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

        public void RegisterNPC(NPC_Brain npc)
        {
            currentnpcs.Add(npc);
            isAvailable = false;
            Save();
        }

        public Vector3 GetBedPos() => bed ? bed.transform.position : transform.position;

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

            Debug($"House saved {save}");
        }

        private void Load()
        {
            string save = m_view.GetZDO().GetString("save");
            if (string.IsNullOrEmpty(save)) return;
            var savedProfiles = JSON.ToObject<string[]>(save);

            StartCoroutine(LoadNPCsIEnumerator(savedProfiles));
            Debug($"House loaded {save}");
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

                brain.SetHouse(this);
            }
        }

        private static void ShowHousesVisual(bool flag)
        {
            foreach (var house in allHouses)
            {
                Utils.FindChild(house.transform, "houseWalls").gameObject.SetActive(flag);
            }
        }

        internal bool IsProfessionHouse() => (houseType == ProfessionHouse || houseType == HousingAndProfessionHouse ||
                                              houseType == FoodAndProfessionHouse || houseType == All)
        //&& craftingStations.Count > 0
        ;

        internal bool IsFoodHouse() => (houseType == Food || houseType == FoodAndProfessionHouse ||
                                        houseType == HousingAndFood || houseType == All)
        //&&  chests.Count > 0
        ;

        public bool IsHousingHouse() => houseType == Housing || houseType == HousingAndFood ||
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

        public bool HasBed() => bed;
        public Bed GetBed() => bed;

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

        public bool AddItem(string prefabName)
        {
            foreach (var chest in chests)
            {
                if (chest.GetInventory().AddItem(ZNetScene.instance.GetPrefab(prefabName), 1))
                    return true;
            }

            return false;
        }
    }
}