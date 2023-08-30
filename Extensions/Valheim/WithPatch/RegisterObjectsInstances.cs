using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Extensions.Valheim.WithPatch;

[HarmonyPatch]
internal static class RegisterObjectsInstances
{
    internal static List<Pickable> AllPickables { get; private set; } = new();
    internal static List<Plant> AllPlants { get; private set; } = new();
    internal static List<Door> AllDoors { get; private set; } = new();
    internal static List<Sign> AllSigns { get; private set; } = new();
    internal static List<Container> AllContainers { get; private set; } = new();
    internal static List<Bed> AllBeds { get; private set; } = new();

    [HarmonyPatch(typeof(Pickable), nameof(Pickable.Awake))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void AddToInstancesCollection(Pickable __instance)
    {
        AllPickables.TryAdd(__instance);
        AllPickables = AllPickables.Where(x => x != null).ToList();
    }

    [HarmonyPatch(typeof(Plant), nameof(Plant.Awake))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void AddToInstancesCollection(Plant __instance)
    {
        AllPlants.TryAdd(__instance);
        AllPlants = AllPlants.Where(x => x != null).ToList();
        ;
        Debug.Log($"Add plant {__instance.GetPrefabName()} to instances collection");
    }

    [HarmonyPatch(typeof(Door), nameof(Door.Awake))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void AddToInstancesCollection(Door __instance)
    {
        AllDoors.TryAdd(__instance);
        AllDoors = AllDoors.Where(x => x != null).ToList();
        ;
    }

    [HarmonyPatch(typeof(Sign), nameof(Sign.Awake))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void AddToInstancesCollection(Sign __instance)
    {
        AllSigns.TryAdd(__instance);
        AllSigns = AllSigns.Where(x => x != null).ToList();
        ;
    }

    [HarmonyPatch(typeof(Container), nameof(Container.Awake))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void AddToInstancesCollection(Container __instance)
    {
        AllContainers.TryAdd(__instance);
        AllContainers = AllContainers.Where(x => x != null).ToList();
        ;
    }

    [HarmonyPatch(typeof(Bed), nameof(Bed.Awake))]
    [HarmonyPostfix]
    [HarmonyWrapSafe]
    private static void AddToInstancesCollection(Bed __instance)
    {
        AllBeds.TryAdd(__instance);
        AllBeds = AllBeds.Where(x => x != null).ToList();
        ;
    }
}

public static class ObjectsInstances
{
    static ObjectsInstances()
    {
        new Harmony("Extensions.Valheim.WithPatch.RegisterObjectsInstances").PatchAll(typeof(RegisterObjectsInstances));
        Debug.Log("RegisterObjectsInstances patched");
    }

    public static List<Pickable> allPickables => RegisterObjectsInstances.AllPickables;
    public static List<Plant> allPlants => RegisterObjectsInstances.AllPlants;
    public static List<Door> allDoors => RegisterObjectsInstances.AllDoors;
    public static List<Sign> allSigns => RegisterObjectsInstances.AllSigns;
    public static List<Container> allContainers => RegisterObjectsInstances.AllContainers;
    public static List<CraftingStation> allCraftingStations => CraftingStation.m_allStations;
    public static List<Bed> allBeds => RegisterObjectsInstances.AllBeds;
}