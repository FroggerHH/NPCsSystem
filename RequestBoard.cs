using System;
using System.Collections.Generic;
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
    private List<GameObject> notes = new();

    private void Awake()
    {
        m_piece = GetComponent<Piece>();
        m_view = GetComponent<ZNetView>();

        var notesTransform = Utils.FindChild(transform, "Notes");
        for (int i = 0; i < notesTransform.childCount; i++)
        {
            notes.Add(notesTransform.GetChild(i).gameObject);
        }
    }

    internal void Init(NPC_Town _town)
    {
        town = _town;
        town.onRequestsChanged += UpdateHover;
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
            foreach (var note in notes)
            {
                note.SetActive(false);
            }
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
                else if (request.requestType == RequestType.Food)
                {
                    requestsFor = "$npc_food".Localize();
                }
                else if (request.requestType == RequestType.Thing)
                {
                    requestsFor = request.thingName.Localize();
                }
                else if (request.requestType == RequestType.Item)
                {
                    int i = 0;
                    foreach (var item in request.items)
                    {
                        i++;
                        requestsFor +=
                            $"{item.Value} {ObjectDB.instance.GetItemPrefab(item.Key).GetComponent<ItemDrop>().m_itemData.m_shared.m_name.Localize()}{(i != request.items.Count ? ", " : "")}";
                    }
                }

                sb.AppendLine($"{index + 1}. {request.npcName} {"$request_asksFor".Localize()} {requestsFor}");
            }


            for (int i = 0; i < notes.Count; i++)
            {
                notes[i].SetActive(i < list.Count);
            }
        }
    }

    public string GetHoverName() => m_piece.m_name.Localize();
}