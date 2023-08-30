using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NPCsSystem;

public static class TerminalCommands
{
    private static readonly List<string> commands = new()
    {
        "CompleteItemRequest",
        "InWhatHouseAmI",
        "DestroyAllPlantsInHouse"
    };

    private static bool isServer => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
    private static string modName => Plugin.ModName;


    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
    [HarmonyWrapSafe]
    internal class AddChatCommands
    {
        private static void Postfix()
        {
            new Terminal.ConsoleCommand(modName, $"Manages the {modName.Replace(".", "")} commands.",
                args =>
                {
                    try
                    {
                        if (!Plugin.configSync.IsAdmin && !ZNet.instance.IsServer())
                        {
                            args.Context.AddString("You are not an admin on this server.");
                            return;
                        }

                        if (args.Length == 2 && args[1] == "CompleteItemRequest")
                        {
                            var town = NPC_Town.FindTown(Player.m_localPlayer.transform.position);
                            var resultRequests = new List<Request>();
                            foreach (var request in town.GetRequests())
                                if (request.requestType == RequestType.Item)
                                {
                                    var warehouse = town.FindWarehouse(null);
                                    foreach (var item in request.items)
                                        if (!warehouse.AddItem(item.Key, item.Value))
                                            args.Context.AddString($"Can't give {item.Value} {item.Key}");

                                    resultRequests.Add(request);
                                }
                                else if (request.requestType == RequestType.Food)
                                {
                                    var warehouse = town.FindWarehouse(null);
                                    if (!warehouse.AddItem("Bread", 3))
                                        args.Context.AddString("Can't give food");

                                    resultRequests.Add(request);
                                }

                            foreach (var request in resultRequests) town.CompleteRequest(request, true);

                            args.Context.AddString("Done");
                            return;
                        }

                        if (args.Length == 2 && args[1] == "InWhatHouseAmI")
                        {
                            var npcHouse = NPC_House.FindHouse(Player.m_localPlayer.transform.position);
                            if (!npcHouse)
                            {
                                args.Context.AddString("None");
                                return;
                            }

                            args.Context.AddString(npcHouse.ToString());
                            return;
                        }

                        if (args.Length == 2 && args[1] == "DestroyAllPlantsInHouse")
                        {
                            var npcHouse = NPC_House.FindHouse(Player.m_localPlayer.transform.position);
                            if (!npcHouse) return;

                            npcHouse.plantStateInfos.ToList().ForEach(x => x.plantObject = null);
                            npcHouse.FindPlants().ForEach(x =>
                            {
                                if (x.m_nview) x.m_nview.Destroy();
                                else Object.Destroy(x.gameObject);
                            });

                            args.Context.AddString("Done");
                            return;
                        }

                        args.Context.AddString("Unknown command");
                        args.Context.AddString("List of all commands:\n");
                        foreach (var command in commands) args.Context.AddString(command);
                    }
                    catch (Exception e)
                    {
                        args.Context.AddString(e.Message);
                    }
                },
                optionsFetcher: () =>
                {
                    var list = new List<string>();
                    foreach (var command in commands) list.Add(command.Split('-')[0]);

                    return list;
                });
        }
    }
}