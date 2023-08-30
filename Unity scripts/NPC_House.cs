using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Bootstrap;
using Extensions;
using Extensions.Valheim.WithPatch;
using fastJSON;
using UnityEngine;
using Visualization;
using static NPCsSystem.Plugin;
using static ItemDrop;
using static ItemDrop.ItemData;
using static NPCsSystem.HouseType;

namespace NPCsSystem;

[RequireComponent(typeof(ZNetView))]
public class NPC_House : MonoBehaviour
{
    internal static List<NPC_House> allHouses = new();
    [SerializeField] private List<Transform> plantPoints = new();

    public NPC_Profession professionForProfessionHouse;
    public int maxNPCs = 1;
    [SerializeField] private float radius = 10;
    [SerializeField] private HouseType houseType = Housing;
    private readonly List<Bed> bedObjs = new();
    private readonly List<Container> chests = new();
    private readonly List<CraftingStation> craftingStations = new();
    private readonly List<(string, int)> deafaultItems = new();
    private readonly List<Door> doors = new();

    private readonly List<Sign> signs = new();

    // private readonly List<Sign> timeSigns = new();
    private Dictionary<string, Bed> beds = new();
    internal List<NPC_Brain> currentnpcs = new();

    //private Location m_location;
    internal ZNetView m_view;
    internal string myWorkerName;
    [SerializeField] internal HashSet<PlantStateInfo> plantStateInfos = new();
    private RequestBoard requestBoard;
    private StaticText staticText;
    internal NPC_Town town;

    private void Awake()
    {
        allHouses.Add(this);
        SetReferenses();
        foreach (var tuple in NPCsManager.itemsToAddToHouses)
        {
            if (!name.Contains(tuple.Item1)) continue;
            deafaultItems.Add((tuple.Item2, tuple.Item3));
        }

        StartCoroutine(RegisterHouse());
        foreach (var point in plantPoints)
            plantStateInfos.Add(new PlantStateInfo(transform.position + point.localPosition));
    }

    private void Start()
    {
        OpenAllDoors();

        if (Chainloader.PluginInfos.ContainsKey("esp"))
        {
            Visibility.SetTag("HouseVisual", true);
            var drawSphere = Draw.DrawSphere("HouseVisual", gameObject, GetRadius());
            Draw.AddText(drawSphere, "NPC house", ToString());
            staticText = drawSphere.GetComponent<StaticText>();

            InvokeRepeating(nameof(UpdateTooltip), 3, 3);
        }
    }


    internal IEnumerator PickupAllDropsIEnumerator(Humanoid human, int delay)
    {
        yield return new WaitForSeconds(delay == 0 ? 0.1f : delay);
        var itemDrops = s_instances.FindAll(x => FindHouse(x.transform.position) == this);
        if (itemDrops.Count > 0)
            itemDrops.ForEach(x =>
            {
                if (chests.Count > 0)
                {
                    AddItem(x.m_itemData);
                }
                else
                {
                    var warehouse = town.FindWarehouse(null);
                    if (warehouse) warehouse.AddItem(x.m_itemData);
                }

                x.m_nview.Destroy();
            });
    }

    internal void PickupAllDrops(Humanoid human, int delay) => StartCoroutine(PickupAllDropsIEnumerator(human, delay));

    private void OnDestroy()
    {
        allHouses.Remove(this);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.gray;
        Gizmos.matrix = Matrix4x4.TRS(transform.position + new Vector3(0.0f, -0.01f, 0.0f),
            Quaternion.identity, new Vector3(1f, 1f / 1000f, 1f));
        Gizmos.DrawSphere(Vector3.zero, GetRadius());
        //Utils.DrawGizmoCircle(this.transform.position, this.m_noBuildRadiusOverride, 32);
        Gizmos.matrix = Matrix4x4.identity;
        Utils.DrawGizmoCircle(transform.position, GetRadius(), 32);
    }

    private void UpdateTooltip()
    {
        staticText.text = ToString();
    }

    public HouseType GetHouseType()
    {
        return houseType;
    }

    public float GetRadius()
    {
        return radius;
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

        // if (name.StartsWith("HouseSettings_Crafter"))
        // {
        //     professionForProfessionHouse = NPC_Profession.Crafter;
        //     houseType = ProfessionHouse;
        // }
        // else if (name.StartsWith("HouseSettings_Hotel"))
        // {
        //     professionForProfessionHouse = NPC_Profession.None;
        //     houseType = Housing;
        // }
        // else if (name.StartsWith("HouseSettings_Warehouse"))
        // {
        //     professionForProfessionHouse = NPC_Profession.None;
        //     houseType = Warehouse;
        // }
        // else if (name.StartsWith("HouseSettings_TownHall"))
        // {
        //     professionForProfessionHouse = NPC_Profession.None;
        //     houseType = TownHall;
        // }
    }

    private IEnumerator RegisterHouse()
    {
        yield return new WaitForSeconds(3f);
        var npcTown = NPC_Town.FindTown(transform.position);
        if (!npcTown) StartCoroutine(RegisterHouse());
        npcTown.RegisterHouse(this);
    }

    public void Init(NPC_Town town)
    {
        this.town = town;
        var pieces = PieceExtention.GetAllPiecesInRadius(transform.position, GetRadius());
        foreach (var piece in pieces)
        {
            if (piece.TryGetComponent(out CraftingStation craftingStation))
            {
                AddCraftingStation(craftingStation);
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

            if (piece.TryGetComponent(out Bed bed))
            {
                bedObjs.Add(bed);
                continue;
            }

            if (piece.TryGetComponent(out Sign sign))
            {
                signs.Add(sign);
                continue;
            }

            if (piece.TryGetComponent(out RequestBoard _requestBoard))
            {
                requestBoard = _requestBoard;
                requestBoard.Init(town);
            }
        }

        if (!m_view.GetZDO().GetBool(ZDOVars.s_addedDefaultItems))
        {
            foreach (var tuple in deafaultItems) AddItem(tuple.Item1, tuple.Item2);

            m_view.GetZDO().Set(ZDOVars.s_addedDefaultItems, true);
        }
        //
        // if (houseType == TownHall)
        //     foreach (var sign in signs)
        //         timeSigns.Add(sign);

        InvokeRepeating(nameof(UpdateTimeSigns), 2, 2);
        InvokeRepeating(nameof(DistributeBeds), 10, 10);
        DistributeBeds();
        Load();
        UpdateTooltip();
    }

    private void UpdateTimeSigns()
    {
        //TODO:UpdateTimeSigns
        // foreach (var sign in timeSigns)
        // {
        //     var dinner = NPC_Brain.IsTimeToDinner();
        //     var work = NPC_Brain.IsTimeToWork();
        //     var chill = NPC_Brain.IsTimeToChill();
        //     try
        //     {
        //         if (dinner) sign.SetText("$npc_timeToDinner".Localize());
        //         if (work) sign.SetText("$npc_timeToWork".Localize());
        //         if (chill) sign.SetText("$npc_timeToChill".Localize());
        //     }
        //     catch (Exception e)
        //     {
        //         DebugError(e);
        //         Destroy(sign.gameObject);
        //     }
        // }
    }

    public static NPC_House FindHouse(Vector3 position)
    {
        NPC_Town town = null;
        if (NPC_Town.towns.Count <= 0) return null;
        foreach (var town_ in NPC_Town.towns)
            if (Utils.DistanceXZ(town_.transform.position, position) <= town_.GetRadius())
            {
                town = town_;
                break;
            }

        if (!town) return null;

        foreach (var house in allHouses)
            if (Utils.DistanceXZ(house.transform.position, position) <= house.GetRadius())
                return house;

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
            if (bed1.Value != null)
                newBeds.Add(bed1.Key, bed1.Value);

        beds = newBeds;

        DistributeBeds();
    }

    private void DistributeBeds()
    {
        beds.Clear();
        if (currentnpcs.Count == 0 || bedObjs.Count == 0) return;
        foreach (var bed in bedObjs) bed.SetOwner(0, string.Empty);

        foreach (var npc in currentnpcs)
        {
            var npcName = npc.profile.name;

            foreach (var bed in bedObjs)
            {
                if (beds.ContainsValue(bed) || beds.ContainsKey(npcName)) continue;

                beds.Add(npcName, bed);
                bed.m_nview.GetZDO().Set(ZDOVars.s_ownerName, npcName);
            }
        }

        // foreach (var request in town.GetRequests())
        // {
        //     if (request.requestType != RequestType.Bed) continue;
        //     if (HasBedFor(request.npcName))
        //     {
        //         town.CompleteRequest(request);
        //     }
        // }
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

    public void RemoveChest(Container craftingStation)
    {
        chests.Remove(craftingStation);
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
        var save = string.Empty;
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
        var save = GetSaveData();
        if (string.IsNullOrEmpty(save)) return;
        var savedProfiles = JSON.ToObject<string[]>(save);
        if (savedProfiles == null || savedProfiles.Length == 0) return;
        myWorkerName = m_view.GetZDO().GetString("myWorkerName", string.Empty);

        StartCoroutine(LoadNPCsIEnumerator(savedProfiles));
    }

    private IEnumerator LoadNPCsIEnumerator(string[] savedProfiles)
    {
        yield return new WaitForSeconds(3f);
        LoadNPCs(savedProfiles);
    }

    private void LoadNPCs(string[] savedProfiles)
    {
        var brains = NPC_Brain.allNPCs.ToList().Where(x => savedProfiles.Contains(x.profile.name));
        foreach (var brain in brains)
        {
            var findSavedHouse = allHouses.Find(x => x.myWorkerName == brain.profile.name);
            brain.SetHouse(this, findSavedHouse ? findSavedHouse : town.FindWorkHouse(brain.profile));
        }

        m_view.GetZDO().Set("FirstLoad", false);
    }

    private static void ShowHousesVisual(bool flag)
    {
        foreach (var house in allHouses) Utils.FindChild(house.transform, "houseWalls").gameObject.SetActive(flag);
    }

    internal bool IsProfessionHouse()
    {
        return houseType == ProfessionHouse || houseType == HousingAndProfessionHouse ||
               houseType == WarehouseAndProfessionHouse || houseType == All;
        //&& craftingStations.Count > 0
    }

    internal bool IsEntertainmentHouse()
    {
        return houseType == Entertainment ||
               houseType == EntertainmentAndProfessionHouse || houseType == All;
    }

    internal bool IsWarehouse()
    {
        return (houseType == Warehouse || houseType == WarehouseAndProfessionHouse ||
                houseType == HousingAndWarehouse || houseType == All)
               && chests.Count > 0;
    }

    public bool IsHousingHouse()
    {
        return houseType == Housing || houseType == HousingAndWarehouse ||
               houseType == HousingAndProfessionHouse || houseType == All;
    }

    public List<ItemData> GetHouseInventory()
    {
        List<ItemData> houseInventory = new();
        foreach (var chest in chests)
        foreach (var item in chest.GetInventory().GetAllItems())
            houseInventory.Add(item);

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

    public List<ItemData> GetItemsByType(ItemType type, out Container container, bool checkIsContainerInUse = false)
    {
        container = null;
        var items = new List<ItemData>();
        foreach (var chest in chests)
        {
            foreach (var item in chest.GetInventory().GetAllItems())
                if (item.m_shared.m_itemType == type)
                {
                    items.Add(item);
                    if (checkIsContainerInUse && chest.m_inUse) continue;
                    container = chest;
                }

            if (container) return items;
        }

        return items;
    }

    public bool HasBeds()
    {
        return beds.Count > 0;
    }

    public bool HasBedFor(string npcName)
    {
        return GetBedFor(npcName);
    }

    public Bed GetBedFor(string npcName)
    {
        if (beds.TryGetValue(npcName, out var bed)) return bed;
        return null;
    }

    public bool AddItem(ItemData itemData)
    {
        foreach (var chest in chests)
            if (chest.GetInventory().AddItem(itemData))
                return true;

        return false;
    }

    public bool HaveEmptySlot()
    {
        foreach (var chest in chests)
            if (chest.GetInventory().HaveEmptySlot())
                return true;

        return false;
    }

    public bool AddItem(string prefabName, int count = 1)
    {
        List<Container> checkedChests = new();

        while (checkedChests.Count != chests.Count)
        {
            var chest = chests.Random();
            checkedChests.Add(chest);
            if (!chest.m_nview.IsOwner()) continue;

            if (chest.GetInventory().AddItem(ZNetScene.instance.GetPrefab(prefabName), count))
                return true;
        }
        // foreach (var chest in chests)
        // {
        //     if (!chest.m_nview.IsOwner()) continue;
        //     if (chest.GetInventory().AddItem(ZNetScene.instance.GetPrefab(prefabName), count))
        //         return true;
        // }

        return false;
    }

    public bool HaveFood()
    {
        return GetItemsByType(ItemType.Consumable, out var _).Count > 0;
    }

    public void CloseAllDoors()
    {
        foreach (var door in doors)
        {
            door.m_nview.GetZDO().Set(ZDOVars.s_state, 0);
            door.UpdateState();
        }
    }

    public void OpenAllDoors()
    {
        foreach (var door in doors)
        {
            door.m_nview.GetZDO().Set(ZDOVars.s_state, 1);
            door.UpdateState();
        }
    }

    public bool IsAvailable()
    {
        if (IsProfessionHouse())
        {
            var i = 0;
            foreach (var npc in NPC_Brain.allNPCs)
                if (npc.workHouse == this)
                    i++;

            return i < maxNPCs;
        }

        return currentnpcs.Count < maxNPCs;
    }

    public int GetItemsCountInHouse(string item)
    {
        var result = 0;
        var list = GetHouseInventory();
        list.ForEach(x =>
        {
            if (x.m_shared.m_name == item) result++;
        });

        return result;
    }

    public Container FindContainer()
    {
        foreach (var chest in chests)
            if (chest.GetInventory().HaveEmptySlot())
                return chest;

        return null;
    }

    private string GetSaveData()
    {
        return m_view.GetZDO().GetString("save");
    }

    public List<Pickable> FindPickables()
    {
        return ObjectsInstances.allPickables.Where(x => x && transform.DistanceXZ(x) <= GetRadius())
            .ToList();
    }

    public List<Plant> FindPlants()
    {
        return ObjectsInstances.allPlants.Where(x => x && transform.DistanceXZ(x) <= GetRadius())
            .ToList();
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Prefab name: {gameObject.GetPrefabName()}");
        sb.AppendLine($"Coords: {gameObject.transform.position.ToSimpleVector3()}");
        sb.AppendLine($"Profession: {professionForProfessionHouse}");
        sb.AppendLine($"Radius: {GetRadius()}");
        if (myWorkerName.IsGood()) sb.AppendLine($"Current worker: {myWorkerName}");
        var currentNPCsStr =
            string.Join(", ", currentnpcs.Select(x => x.profile.name)) + string.Join(", ",
                NPC_Brain.allNPCs.ToList().FindAll(x => x.workHouse == this).Select(x => x.profile.name));
        if (currentNPCsStr.IsGood()) sb.AppendLine($"Current NPCs: {currentNPCsStr}");
        if (bedObjs.Count > 0) sb.AppendLine($"Beds: {bedObjs.Select(x => x.GetPrefabName()).GetString()}");
        if (doors.Count > 0) sb.AppendLine($"Doors: {doors.Select(x => x.GetPrefabName()).GetString()}");
        if (signs.Count > 0) sb.AppendLine($"Signs: {signs.Select(x => x.GetPrefabName()).GetString()}");
        var plants = FindPlants();
        var pickables = FindPickables();
        if (plants.Count > 0) sb.AppendLine($"Plants: {plants.Select(x => x.GetPrefabName()).GetString()}");
        if (pickables.Count > 0) sb.AppendLine($"Pickables: {pickables.Select(x => x.GetPrefabName()).GetString()}");
        if (chests.Count > 0) sb.AppendLine($"Chests: {chests.Select(x => x.GetPrefabName()).GetString()}");
        if (craftingStations.Count > 0)
            sb.AppendLine($"CraftingStations: {craftingStations.Select(x => x.GetPrefabName()).GetString()}");

        return sb.ToString();
    }
}