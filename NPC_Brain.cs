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

    public override void Awake()
    {
        base.Awake();
        allNPCs.Add(this);
        profile = GetSavedProfile();
        SetHuntPlayer(false);
        m_character.m_faction = Character.Faction.Players;
    }

    private void OnDestroy()
    {
        allNPCs.Remove(this);
    }

    public void Init(NPC_Profile profile)
    {
        this.profile = profile;
        m_nview.GetZDO().Set("NPC_ID",
            (profile.name + Random.Range(int.MinValue, Int32.MaxValue).ToString()).GetStableHashCode());
        SaveProfile(profile);
    }

    public void SetHouse(NPC_House npcHouse)
    {
        house = npcHouse;
        m_randomMoveRange = house.GetRadius();
        SetPatrolPoint(transform.position);
    }
    
    public NPC_Profile GetSavedProfile()
    {
        return TownDB.GetProfile(m_nview.GetZDO().GetString("ProfileName"));
    }

    public void SaveProfile(NPC_Profile profile)
    {
       m_nview.GetZDO().Set("ProfileName", profile.name);
    }

    public string GetHoverText() => profile.name;

    public string GetHoverName() => profile.name;
}