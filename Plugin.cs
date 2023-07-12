using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ItemManager;
using LocationManager;
using CreatureManager;
using JustAFrogger;
using ServerSync;
using UnityEngine;
using PieceManager;

namespace NPCsSystem;

[BepInPlugin(ModGUID, ModName, ModVersion)]
internal class Plugin : BaseUnityPlugin
{
    #region values

    internal const string ModName = "NPCsSystem", ModVersion = "1.0.0", ModGUID = "com.Frogger." + ModName;
    internal static Harmony harmony = new(ModGUID);
    internal static Plugin _self;
    internal static AssetBundle bundle;
    public static float time;
    public static Action onTimeUpdated;

    #endregion

    #region tools

    public static void Debug(string msg)
    {
        _self.Logger.LogInfo(msg);
    }

    public static void DebugError(object msg, bool showWriteToDev = true)
    {
        if (showWriteToDev)
        {
            msg += "Write to the developer and moderator if this happens often.";
        }

        _self.Logger.LogError(msg);
    }

    public static void DebugWarning(string msg, bool showWriteToDev = false)
    {
        if (showWriteToDev)
        {
            msg += "Write to the developer and moderator if this happens often.";
        }

        _self.Logger.LogWarning(msg);
    }

    #endregion

    #region ConfigSettings

    #region tools

    static string ConfigFileName = $"com.Frogger.{ModName}.cfg";
    DateTime LastConfigChange;

    public static readonly ConfigSync configSync = new(ModName)
        { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

    public static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
        bool synchronizedSetting = true)
    {
        ConfigEntry<T> configEntry = _self.Config.Bind(group, name, value, description);

        SyncedConfigEntry<T> syncedConfigEntry = configSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    private ConfigEntry<T> config<T>(string group, string name, T value, string description,
        bool synchronizedSetting = true)
    {
        return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }

    void SetCfgValue<T>(Action<T> setter, ConfigEntry<T> config)
    {
        setter(config.Value);
        config.SettingChanged += (_, _) => setter(config.Value);
    }

    public enum Toggle
    {
        On = 1,
        Off = 0
    }

    #endregion

    #region configs

    public static ConfigEntry<bool> npcsNoCost;

    #endregion

    #endregion

    #region Config

    private void SetupWatcher()
    {
        FileSystemWatcher fileSystemWatcher = new(Paths.ConfigPath, ConfigFileName);
        fileSystemWatcher.Changed += ConfigChanged;
        fileSystemWatcher.IncludeSubdirectories = true;
        fileSystemWatcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
        fileSystemWatcher.EnableRaisingEvents = true;
    }

    void ConfigChanged(object sender, FileSystemEventArgs e)
    {
        if ((DateTime.Now - LastConfigChange).TotalSeconds <= 5.0)
        {
            return;
        }

        LastConfigChange = DateTime.Now;
        try
        {
            Config.Reload();
        }
        catch
        {
            DebugError("Can't reload Config", true);
        }
    }

    private void UpdateConfiguration()
    {
        Debug("Configuration Received");
    }

    #endregion

    private void Awake()
    {
        _self = this;
        harmony.PatchAll();

        #region Config

        configSync.AddLockingConfigEntry(config("Main", "Lock Configuration", Toggle.On,
            "If on, the configuration is locked and can be changed by server admins only."));

        npcsNoCost = config("Main", "NPCs dont need resources", false, "");
        Config.Save();

        #endregion

        bundle = ItemManager.PrefabManager.RegisterAssetBundle("npsssystem");
        LocationManager.Location TestTown = new(bundle, "TestTown")
        {
            Biome = Const.Desert,
            Count = 15,
            Prioritize = true,
            Rotation = Rotation.Random,
            CanSpawn = true,
            MapIcon = "WoodNPSHouse",
            ShowMapIcon = ShowIcon.Always,
            GroupName = "Town",
            MinimumDistanceFromGroup = 500,
            PreferCenter = true,
            SpawnAltitude = new(10, 200)
        };
        Creature PlayerNPS = new(bundle, "PlayerNPS")
        {
            CanHaveStars = false,
            CanBeTamed = false,
            CanSpawn = false
        };

        BuildPiece piece_requestBoard = new(bundle, "piece_requestBoard");
        piece_requestBoard.Crafting.Set(CraftingTable.Workbench);
        piece_requestBoard.Category.Add(BuildPieceCategory.Furniture);
        piece_requestBoard.RequiredItems.Add("FineWood", 25, true);
        piece_requestBoard.Name
            .Russian("Доска запросов")
            .Portuguese_Brazilian("Quadro De Pedidos")
            .English("Request board");
        piece_requestBoard.Description
            .English("");
        piece_requestBoard.SpecialProperties.AdminOnly = true;


        NPCsManager.Initialize(bundle);
        var Bill = NPCsManager.GetNPCProfile("Bill");
        Bill.AddCrafterItem("ArmorBronzeChest");
        Bill.AddCrafterItem("ArmorBronzeLegs");
        Bill.AddCrafterItem("ArrowBronze");
        Bill.AddCrafterItem("AtgeirBronze");
        Bill.AddCrafterItem("AxeBronze");
        Bill.AddCrafterItem("BronzeNails", 25);
        Bill.AddCrafterItem("HelmetBronze");
        Bill.AddCrafterItem("MaceBronze");
        Bill.AddCrafterItem("PickaxeBronze");
        Bill.AddCrafterItem("ShieldBronzeBuckler");
        Bill.AddCrafterItem("SpearBronze");
        Bill.AddCrafterItem("SwordBronze");


        var Brian = NPCsManager.GetNPCProfile("Brian");
        Brian.AddCrafterItem("ArmorIronChest");
        Brian.AddCrafterItem("ArmorIronLegs");
        Brian.AddCrafterItem("ArrowIron");
        Brian.AddCrafterItem("AtgeirIron");
        Brian.AddCrafterItem("AxeIron");
        Brian.AddCrafterItem("BoltIron");
        Brian.AddCrafterItem("HelmetIron");
        Brian.AddCrafterItem("IronNails", 25);
        Brian.AddCrafterItem("PickaxeIron");
        Brian.AddCrafterItem("ShieldIronBuckler");
        Brian.AddCrafterItem("ShieldIronSquare");
        Brian.AddCrafterItem("ShieldIronTower");
        Brian.AddCrafterItem("SwordIron");

        var Carl = NPCsManager.GetNPCProfile("Carl");
        Carl.AddCrafterItem("ArrowSilver");
        Carl.AddCrafterItem("MaceSilver");
        Carl.AddCrafterItem("ShieldSilver");
        Carl.AddCrafterItem("SwordSilver");

        var Charles = NPCsManager.GetNPCProfile("Charles");
        Charles.AddCrafterItem("ArmorLeatherChest");
        Charles.AddCrafterItem("ArmorLeatherLegs");
        Charles.AddCrafterItem("ArmorTrollLeatherChest");
        Charles.AddCrafterItem("ArmorTrollLeatherLegs");
        Charles.AddCrafterItem("HelmetLeather");
        Charles.AddCrafterItem("HelmetTrollLeather");

        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "CookedDeerMeat", 80);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "CookedDeerMeat", 80);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "CookedMeat", 80);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "CookedMeat", 80);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "RawMeat", 25);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "Bronze", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "Bronze", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "Bronze", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "Iron", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "Iron", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "Iron", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "Silver", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "Silver", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "Silver", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "BoneFragments", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "BoneFragments", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "DeerHide", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "DeerHide", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "DeerHide", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "TrollHide", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "TrollHide", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "TrollHide", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "Feathers", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "Feathers", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "Feathers", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "Wood", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "Wood", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "Wood", 155);

        new TradeItem("Ann,Jane,Joseph,Richard,Ryan,Steve", "Onion", 25, stack: 5);
        new TradeItem("Ann,Jane,Joseph,Richard,Ryan,Steve", "Raspberry", 10, stack: 5);
        new TradeItem("Ann,Jane,Joseph,Richard,Ryan,Steve", "QueensJam", 100, moneyItemName: "Raspberry");
        new TradeItem("Ann,Jane,Joseph,Richard,Ryan,Steve", "FishCooked", 2, moneyItemName: "FishRaw");
        new TradeItem("Ann,Jane,Joseph,Richard,Ryan,Steve", "OnionSoup", 10, moneyItemName: "Onion");
        new TradeItem("Ann,Jane,Joseph,Richard,Ryan,Steve", "Bread", 45, moneyItemName: "Carrot");

        LocalizationManager.Localizer.Load();
        JFHelperLite.Initialize(Info);
        JFHelperLite.FixMusicLocation(bundle, "TestTown");
        InvokeRepeating(nameof(UpdateTime), 2, 2);
    }


    private void UpdateTime()
    {
        onTimeUpdated?.Invoke();
        time = TimeUtils.GetCurrentTimeValue();
    }
}