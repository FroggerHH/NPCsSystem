using System.Collections.Generic;
using System.Linq;
using System.Text;
using Extensions.Valheim;
using UnityEngine;

namespace NPCsSystem;

public class RequestBoard : MonoBehaviour, Interactable, Hoverable
{
    private readonly List<GameObject> notes = new();
    private readonly StringBuilder sb = new();
    private Piece m_piece;
    private ZNetView m_view;
    private NPC_Town town;

    private void Awake()
    {
        m_piece = GetComponent<Piece>();
        m_view = GetComponent<ZNetView>();

        var notesTransform = Utils.FindChild(transform, "Notes");
        for (var i = 0; i < notesTransform.childCount; i++) notes.Add(notesTransform.GetChild(i).gameObject);

        var signPos = transform.position + new Vector3(0, 4, 0.25f);
        //var sign = Piece.s_allPieces.Find(x => x.transform.position == signPos)?.GetComponent<Sign>();
        //var prefab = ZNetScene.instance.GetPrefab("sign");
        //if (!sign && prefab) sign = Instantiate(prefab, signPos, Quaternion.identity).GetComponent<Sign>();
    }

    public string GetHoverText()
    {
        return sb.ToString();
    }

    public string GetHoverName()
    {
        return m_piece.m_name.Localize();
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

    internal void Init(NPC_Town _town)
    {
        town = _town;
        town.onRequestsChanged += UpdateHover;
        UpdateHover();
        InvokeRepeating(nameof(UpdateHover), 3, 3);
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
            foreach (var note in notes) note.SetActive(false);
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
                    requestsFor = "$piece_bed".Localize();
                else if (request.requestType == RequestType.Food)
                    requestsFor = "$npc_food".Localize();
                else if (request.requestType == RequestType.Thing)
                    requestsFor = request.thingName.Localize();
                else if (request.requestType == RequestType.Item)
                    requestsFor += string.Join(", ",
                        request.items.Select(x =>
                            $"{x.Value} {ObjectDB.instance.GetItemPrefab(x.Key).GetComponent<ItemDrop>().m_itemData.m_shared.m_name.Localize()}"));

                sb.AppendLine($"{index + 1}. {request.npcName} {"$request_asksFor".Localize()} {requestsFor}");
            }


            for (var i = 0; i < notes.Count; i++) notes[i].SetActive(i < list.Count);
        }
    }
}