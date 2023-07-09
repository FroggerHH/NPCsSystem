﻿using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ItemManager;
using LocationManager;
using CreatureManager;
using ServerSync;
using UnityEngine;
using PieceManager;
using static ItemManager.PrefabManager;
using PrefabManager = ItemManager.PrefabManager;

namespace NPCsSystem;

[BepInPlugin(ModGUID, ModName, ModVersion)]
internal class Plugin : BaseUnityPlugin
{
    #region values

    internal const string ModName = "NPCsSystem", ModVersion = "1.0.0", ModGUID = "com.Frogger." + ModName;
    internal static Harmony harmony = new(ModGUID);
    internal static Plugin _self;
    internal static AssetBundle bundle;

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

        bundle = RegisterAssetBundle("npsssystem");
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

        PrefabManager.RegisterPrefab(bundle, "TestNullTown");

        NPCsManager.Initialize(bundle);
        var Bill = NPCsManager.GetNPCProfile("Bill");
        Bill.AddCrafterItem("ArmorBronzeChest");
        Bill.AddCrafterItem("ArmorBronzeLegs");
        Bill.AddCrafterItem("ArrowBronze");
        Bill.AddCrafterItem("AtgeirBronze");
        Bill.AddCrafterItem("AxeBronze");
        Bill.AddCrafterItem("BronzeNails");
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
        Brian.AddCrafterItem("IronNails");
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
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "CookedMeat", 80);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "RawMeat", 25);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "Bronze", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "Iron", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "Silver", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "BoneFragments", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "DeerHide", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "TrollHide", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "Feathers", 155);
        NPCsManager.AddDefaultItemToHouse("NPSHouseWarehouse", "Wood", 155);

        LocalizationManager.Localizer.Load();
    }
}