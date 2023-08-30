using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ServerSync;

namespace NPCsSystem;

[BepInPlugin(ModGUID, ModName, ModVersion)]
internal class Plugin : BaseUnityPlugin
{
    private void Awake()
    {
        _self = this;
        Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), ModGUID);

        #region Config

        configSync.AddLockingConfigEntry(config("Main", "Lock Configuration", Toggle.On,
            "If on, the configuration is locked and can be changed by server admins only."));

        npcsNoCost = config("Main", "NPCs dont need resources", false, "");
        Config.Save();

        #endregion

        InvokeRepeating(nameof(UpdateTime), 2, 2);
    }


    private void UpdateTime()
    {
        onTimeUpdated?.Invoke();
        time = TimeUtils.GetCurrentTimeValue();
    }

    #region values

    internal const string ModName = "NPCsSystem", ModVersion = "1.0.0", ModGUID = "com.Frogger." + ModName;
    internal static Plugin _self;
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
        if (showWriteToDev) msg += "Write to the developer and moderator if this happens often.";

        _self.Logger.LogError(msg);
    }

    public static void DebugWarning(string msg, bool showWriteToDev = false)
    {
        if (showWriteToDev) msg += "Write to the developer and moderator if this happens often.";

        _self.Logger.LogWarning(msg);
    }

    #endregion

    #region ConfigSettings

    #region tools

    private static readonly string ConfigFileName = $"com.Frogger.{ModName}.cfg";
    private DateTime LastConfigChange;

    public static readonly ConfigSync configSync = new(ModName)
        { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

    public static ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
        bool synchronizedSetting = true)
    {
        var configEntry = _self.Config.Bind(group, name, value, description);

        var syncedConfigEntry = configSync.AddConfigEntry(configEntry);
        syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

        return configEntry;
    }

    private ConfigEntry<T> config<T>(string group, string name, T value, string description,
        bool synchronizedSetting = true)
    {
        return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
    }

    private void SetCfgValue<T>(Action<T> setter, ConfigEntry<T> config)
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

    private void ConfigChanged(object sender, FileSystemEventArgs e)
    {
        if ((DateTime.Now - LastConfigChange).TotalSeconds <= 5.0) return;

        LastConfigChange = DateTime.Now;
        try
        {
            Config.Reload();
        }
        catch
        {
            DebugError("Can't reload Config");
        }
    }

    private void UpdateConfiguration()
    {
        Debug("Configuration Received");
    }

    #endregion
}