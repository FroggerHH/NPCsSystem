using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace NPCsSystem;

public static class TerminalCommands
{
    private static bool isServer => SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
    private static string modName => Plugin.ModName;

    private static List<string> commands = new()
    {
        "CompleteItemRequest",
    };


    [HarmonyPatch(typeof(Terminal), nameof(Terminal.InitTerminal))]
    internal class AddChatCommands
    {
        private static void Postfix()
        {
            _ = new Terminal.ConsoleCommand(modName, $"Manages the {modName.Replace(".", "")} commands.",
                args =>
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
                        {
                            if (request.requestType != RequestType.Item) continue;
                            var warehouse = town.FindWarehouse(null);
                            foreach (var item in request.items)
                            {
                                if (!warehouse.AddItem(item.Key, item.Value))
                                    args.Context.AddString($"Can't give {item.Value} {item.Key}");
                            }

                            resultRequests.Add(request);
                        }

                        foreach (var request in resultRequests)
                        {
                            town.CompleteRequest(request, true);
                        }

                        args.Context.AddString("Done");
                        return;
                    }
                    else
                    {
                        args.Context.AddString($"Unknown command");
                        args.Context.AddString($"List of all commands:\n");
                        foreach (var command in commands) args.Context.AddString(command);
                        return;
                    }
                },
                optionsFetcher: () =>
                {
                    var list = new List<string>();
                    foreach (var command in commands)
                    {
                        list.Add(command.Split('-')[0]);
                    }

                    return list;
                });
        }
    }
}