using System;
using System.Linq;
using System.Text;
using UnityEngine;

namespace NPCsSystem;

public class RequestBoard : MonoBehaviour, Interactable, Hoverable
{
    private Piece m_piece;
    private ZNetView m_view;
    private NPC_Town town;
    private StringBuilder sb = new StringBuilder();

    private void Awake()
    {
        m_piece = GetComponent<Piece>();
        m_view = GetComponent<ZNetView>();
    }

    internal void Init(NPC_Town _town)
    {
        town = _town;

        UpdateHover();
        InvokeRepeating(nameof(UpdateHover), 3, 3);
    }

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold) return false;
        return false;
    }

    public bool UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        return false;
    }

    public string GetHoverText()
    {
        return sb.ToString();
    }

    private void UpdateHover()
    {
        if (!town) return;

        sb.Clear();
        sb.AppendLine(GetHoverName());
        var list = town.GetRequests();
        if (list.Count == 0)
        {
            sb.AppendLine("$noRequests".Localize());
        }
        else
        {
            sb.AppendLine("$requests :".Localize());
            sb.AppendLine();
            for (var index = 0; index < list.Count; index++)
            {
                var request = list[index];
                var requestsFor = string.Empty;
                if (request.requestType == RequestType.Bed)
                {
                    requestsFor = "$piece_bed".Localize();
                }
                else if (request.requestType == RequestType.CraftingStation)
                {
                    requestsFor = request.thingName.Localize();
                }
                else if (request.requestType == RequestType.Item)
                {
                    foreach (var item in request.items)
                    {
                        requestsFor += $"{item.Item2} {item.Item1.m_name.Localize()}, ";
                    }
                }

                if (requestsFor.EndsWith(", "))
                {
                    requestsFor.Remove(requestsFor.Length - 2);
                }

                sb.AppendLine($"{index + 1}. {request.npcName} {"$request_asksFor".Localize()} {requestsFor}");
            }
        }
    }

    public string GetHoverName() => m_piece.m_name.Localize();
}