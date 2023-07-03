using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NPCsSystem;

public class NPC_Brain : MonsterAI, Hoverable
{
    internal static HashSet<NPC_Brain> allNPCs = new HashSet<NPC_Brain>();

    internal NPC_Profile profile;
    internal NPC_House house;
    internal BaseAI baseAI;

    public override void Awake()
    {
        baseAI = GetComponent<BaseAI>();
        allNPCs.Add(this);
        profile = GetSavedProfile();
        baseAI.SetHuntPlayer(false);
        baseAI.m_character.m_faction = Character.Faction.Players;
    }

    private void OnDestroy()
    {
        allNPCs.Remove(this);
    }

    public void Init(NPC_Profile profile)
    {
        this.profile = profile;
        baseAI.m_nview.GetZDO().Set("NPC_ID",
            (profile.m_name + Random.Range(int.MinValue, Int32.MaxValue).ToString()).GetStableHashCode());
        SaveProfile(profile);
    }

    public void SetHouse(NPC_House npcHouse)
    {
        house = npcHouse;
        baseAI.m_randomMoveRange = house.GetRadius();
        baseAI.SetPatrolPoint(transform.position);
    }

    public int GetNPC_ID()
    {
        return baseAI.m_nview.GetZDO().GetInt("NPC_ID");
    }

    public void SetNPC_ID(int id)
    {
        baseAI.m_nview.GetZDO().Set("NPC_ID", id);
    }

    public NPC_Profile GetSavedProfile()
    {
        return TownDB.GetProfile(baseAI.m_nview.GetZDO().GetString("ProfileName"));
    }

    public void SaveProfile(NPC_Profile profile)
    {
        baseAI.m_nview.GetZDO().Set("ProfileName", profile.m_name);
    }

    public string GetHoverText() => profile.m_name;

    public string GetHoverName() => profile.m_name;
}