﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using Timer = System.Timers.Timer;
using static NPCsSystem.Plugin;
using static ItemDrop;
using static ItemDrop.ItemData;
using static ItemDrop.ItemData.AiTarget;
using static ItemDrop.ItemData.ItemType;
using static ItemDrop.ItemData.SharedData;

namespace NPCsSystem;

public class NPC_Brain : BaseAI, Hoverable, Interactable
{
    internal static HashSet<NPC_Brain> allNPCs = new HashSet<NPC_Brain>();
    internal NPC_Profile profile;
    internal NPC_House house;
    internal Humanoid human;

    public NPC_Profile defaultProfile;
    public bool m_despawnInDay;
    public bool m_eventCreature;
    public float m_interceptTime;
    private float m_pauseTimer;
    private System.Timers.Timer m_professionBehaviorTimer;
    public float m_professionBehaviorInterval = 150;
    public float m_updateTargetTimer;
    public float m_interceptTimeMax;
    public float m_interceptTimeMin;
    public EffectList m_wakeupEffects = new EffectList();
    public float m_sleepDelay = 0.5f;
    public Character m_targetCreature;
    public Vector3 m_lastKnownTargetPos = Vector3.zero;
    public bool m_beenAtLastPos;
    public string m_aiStatus = string.Empty;
    public float m_fleeIfLowHealth;
    public bool m_fleeIfNotAlerted;
    public float m_alertRange = 9999f;
    public float seePrivatePropertyRange = 15f;
    [Header("Circle target")] public float m_circleTargetInterval;
    public float m_circleTargetDuration = 5f;
    public float m_circleTargetDistance = 10f;
    public float m_updateWeaponTimer;
    public float m_minAttackInterval;
    public bool m_circulateWhileCharging;
    public GameObject m_follow;
    public float m_timeSinceAttacking;
    public float m_unableToAttackTargetTimer;
    public float m_timeSinceSensedTargetCreature;
    public float m_maxChaseDistance;
    [Header("Food")] public float m_consumeSearchInterval = 10f;
    private NPC_House foodHouse;
    public float m_wakeupRange = 15;
    private ItemData m_currentConsumeItem;
    public float m_consumeSearchTimer;
    private Action<ItemData> m_onConsumedItem;
    public float m_eatDuration = 30f;
    private Container m_containerToGrab;
    public EffectList m_sootheEffect = new EffectList();

    [Header("Attach")] public bool m_attached;
    private string m_attachAnimation = "";
    private bool m_sleeping;
    private Transform m_attachPoint;
    private Vector3 m_detachOffset = Vector3.zero;
    private Vector3 m_attachOffset = Vector3.zero;
    private Transform m_attachPointCamera;
    private Collider[] m_attachColliders;
    public Text ai_statusText;
    private bool inCrafting = false;
    private bool inProfessionBehavior = false;
    private GameObject hammerMark;
    private WearNTear targetBuilding;
    private Vector3 lootPos;

    public override void Awake()
    {
        base.Awake();
        m_despawnInDay = false;
        m_eventCreature = false;
        m_animator.SetBool(MonsterAI.s_sleeping, IsSleeping());
        m_onConsumedItem += OnConsumedItem;
        m_interceptTime = Random.Range(m_interceptTimeMin, m_interceptTimeMax);
        m_pauseTimer = Random.Range(0, m_circleTargetInterval);
        m_updateTargetTimer = Random.Range(0, 2f);
        SetHuntPlayer(false);
        var savedProfile = GetSavedProfile();
        profile = savedProfile == null ? defaultProfile : savedProfile;
        if (GetComponent<Tameable>())
        {
            DebugError("Found tamable component. NPCs can't be tamed. Remove it.");
            m_tamable = null;
        }

        m_afraidOfFire = false;
        ai_statusText = Utils.FindChild(transform, "Ai status").GetComponent<Text>();
        hammerMark = Utils.FindChild(transform, "HammerMark").gameObject;
        //m_pathAgentType = 0;
    }

    public void UpdateAiStatus()
    {
        ai_statusText.text = m_aiStatus;
    }

    public void Start()
    {
        if (!m_nview || !m_nview.IsValid() || !m_nview.IsOwner()) return;
        human = m_character as Humanoid;
        if (!human)
        {
            DebugError($"NPC {profile.name} is not a Humanoid");
            return;
        }

        human.m_name = profile.name + $"({profile.m_profession})";
        name = profile.name + " (Clone)";

        if (profile.IsWarrior()) human.EquipBestWeapon(null, null, null, null);
        else UnequiAllWeapons();
    }

    public new void UpdateAI(float dt)
    {
        hammerMark.SetActive(inProfessionBehavior);
        if (!m_nview.IsOwner()) return;
        if (IsSleeping()) UpdateSleep(dt);
        else
        {
            bool canHearTarget;
            bool canSeeTarget;
            UpdateTarget(human, dt, out canHearTarget, out canSeeTarget);

            if (m_fleeIfNotAlerted && m_targetCreature && !IsAlerted() &&
                Vector3.Distance(m_targetCreature.transform.position, transform.position) -
                m_targetCreature.GetRadius() > m_alertRange)
            {
                Flee(dt, m_targetCreature.transform.position);
                m_aiStatus = "Avoiding conflict";
            }
            else if (m_fleeIfLowHealth > 0 && m_character.GetHealthPercentage() < m_fleeIfLowHealth &&
                     m_timeSinceHurt < 20f && m_targetCreature)
            {
                Flee(dt, m_targetCreature.transform.position);
                m_aiStatus = "Low health, flee";
            }
            else if (AvoidBurning(dt))
            {
                m_aiStatus = "Avoiding fire";
            }
            else
            {
                if (m_aiStatus == "Target dead, go for loot")
                {
                    PickupLootFromDeadEnemy(dt);
                }


                //if (IsAlerted())
                //{
                if (m_circleTargetInterval > 0f && m_targetCreature && IsAlerted())
                {
                    m_pauseTimer += dt;
                    if (m_pauseTimer > m_circleTargetInterval)
                    {
                        if (m_pauseTimer > m_circleTargetInterval + m_circleTargetDuration)
                            m_pauseTimer = Random.Range(0f, m_circleTargetInterval / 10f);
                        RandomMovementArroundPoint(dt, m_targetCreature.transform.position, m_circleTargetDistance,
                            IsAlerted());
                        m_aiStatus = "Attack pause";
                        return;
                    }
                }

                ItemData itemData = SelectBestAttack(human, dt);
                bool flag = itemData != null &&
                            Time.time - itemData.m_lastAttackTime > itemData.m_shared.m_aiAttackInterval &&
                            m_character.GetTimeSinceLastAttack() >= m_minAttackInterval && !IsTakingOff();
                // if ((m_circulateWhileCharging ? 1 : 0) != 0 && m_targetCreature && itemData != null && !flag &&
                //     !m_character.InAttack())
                // {
                //     m_aiStatus = "Move around target weapon ready:" + flag.ToString();
                //     if (itemData != null)
                //         m_aiStatus += " Weapon:" + itemData.m_shared.m_name;
                //     Vector3 point = m_targetCreature.transform.position;
                //     RandomMovementArroundPoint(dt, point, m_randomMoveRange, IsAlerted());
                // }
                // else 
                if (!m_targetCreature)
                {
                    if (m_follow)
                    {
                        Follow(m_follow, dt);
                        m_aiStatus = $"Follow {m_follow.GetPrefabName()}";
                    }
                    else
                    {
                        if (UpdateFooding_falseNotNeed(human, dt) == false)
                            UpdateNormalBehavior(dt);
                    }
                }
                else if (itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Enemy && IsAlerted())
                {
                    if (!m_targetCreature)
                        return;
                    if (canHearTarget | canSeeTarget)
                    {
                        m_beenAtLastPos = false;
                        m_lastKnownTargetPos = m_targetCreature.transform.position;
                        float num1 = Vector3.Distance(m_lastKnownTargetPos, transform.position) -
                                     m_targetCreature.GetRadius();
                        float num2 = m_alertRange * m_targetCreature.GetStealthFactor();
                        if (canSeeTarget && num1 < num2)
                            SetAlerted(true);
                        int num3 = num1 < itemData.m_shared.m_aiAttackRange ? 1 : 0;
                        if (num3 == 0 || !canSeeTarget || itemData.m_shared.m_aiAttackRangeMin < 0.0 ||
                            !IsAlerted())
                        {
                            m_aiStatus = "Move closer";
                            Vector3 velocity = m_targetCreature.GetVelocity();
                            Vector3 vector3 = velocity * m_interceptTime;
                            Vector3 lastKnownTargetPos = m_lastKnownTargetPos;
                            if (num1 > vector3.magnitude / 4.0)
                                lastKnownTargetPos += velocity * m_interceptTime;
                            MoveTo(dt, lastKnownTargetPos, 0.0f, IsAlerted());
                            if (m_timeSinceAttacking > 15.0)
                                m_unableToAttackTargetTimer = 15f;
                        }
                        else
                            StopMoving();

                        if ((num3 & (canSeeTarget ? 1 : 0)) == 0 || !IsAlerted())
                            return;
                        m_aiStatus = "In attack range";
                        LookAt(m_targetCreature.GetTopPoint());
                        if (!flag || !IsLookingAt(m_lastKnownTargetPos,
                                itemData.m_shared.m_aiAttackMaxAngle))
                            return;
                        m_aiStatus = "Attacking creature";
                        DoAttack(m_targetCreature, false);
                    }
                    else
                    {
                        m_aiStatus = "Searching for target";
                        if (m_beenAtLastPos)
                        {
                            RandomMovement(dt, m_lastKnownTargetPos);
                            if (m_timeSinceAttacking <= 15.0)
                                return;
                            m_unableToAttackTargetTimer = 15f;
                        }
                        else
                        {
                            if (!MoveTo(dt, m_lastKnownTargetPos, 0.0f, IsAlerted()))
                                return;
                            m_beenAtLastPos = true;
                        }
                    }
                }
                // else
                // {
                //     if (itemData.m_shared.m_aiTargetType != ItemDrop.ItemData.AiTarget.FriendHurt &&
                //         itemData.m_shared.m_aiTargetType != ItemDrop.ItemData.AiTarget.Friend && IsAlerted())
                //         return;
                //     m_aiStatus = "Helping friend";
                //     Character target = itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.FriendHurt
                //         ? HaveHurtFriendInRange(m_viewRange)
                //         : HaveFriendInRange(m_viewRange);
                //     if (target)
                //     {
                //         if (Vector3.Distance(target.transform.position, transform.position) <
                //             itemData.m_shared.m_aiAttackRange)
                //         {
                //             if (flag)
                //             {
                //                 StopMoving();
                //                 LookAt(target.transform.position);
                //                 DoAttack(target, true);
                //             }
                //             else
                //                 RandomMovement(dt, target.transform.position);
                //         }
                //         else
                //             MoveTo(dt, target.transform.position, 0.0f, IsAlerted());
                //     }
                //     else
                //         RandomMovement(dt, transform.position, true);
                //}
                //}
            }
        }
    }

    private void PickupLootFromDeadEnemy(float dt)
    {
        if (MoveTo(dt, lootPos, 3, false))
        {
            if (IsLookingAt(lootPos, 20f))
            {
                Collider[] colliderArray =
                    Physics.OverlapSphere(transform.position, 8, MonsterAI.m_itemMask);
                List<ItemDrop> drops = new();
                float num1 = 999999f;
                foreach (Collider collider in colliderArray)
                {
                    if (collider.attachedRigidbody)
                    {
                        ItemDrop component = collider.attachedRigidbody.GetComponent<ItemDrop>();
                        if (component && component.GetComponent<ZNetView>().IsValid())
                        {
                            drops.Add(component);
                        }
                    }
                }

                var drop = Helper.Nearest(drops, transform.position);
                if (drop)
                {
                    human.Pickup(drop.gameObject);
                    drops.Remove(drop);
                }
                else
                {
                    m_aiStatus = "";
                    //TODO: Put the loot from the enemy in the chest
                }
            }
        }
        else
        {
            m_aiStatus = $"Can't reach loot";
        }
    }

    private void UpdateNormalBehavior(float dt)
    {
        if (!house || !house.town) return;
        if (EnvMan.instance.IsNight())
        {
            m_aiStatus = $"Going to sleep";
            var bedPos = house.GetBedPos();
            var hasBed = house.HasBed();
            if (MoveTo(dt, bedPos, 3, false))
            {
                if (hasBed)
                {
                    var bed = house.GetBed();
                    LookAt(bedPos);
                    if (IsLookingAt(bedPos, 20f))
                    {
                        Sleep();
                    }
                }
                else
                {
                    m_aiStatus = $"Need a bed";
                }
            }
            else
            {
                if (MoveTo(dt, house.transform.position, 3, false))
                {
                    m_aiStatus = "Can't reach bad, going to house";
                }
                else
                {
                    m_aiStatus = "Can't reach bed";
                }
            }
        }
        else
        {
            if (inProfessionBehavior && HaveProfession())
            {
                UpdateProfessionBehavior(dt);
                return;
            }
            else
            {
                if (m_professionBehaviorTimer == null && HaveProfession())
                {
                    m_professionBehaviorTimer = new System.Timers.Timer(m_professionBehaviorInterval);
                    m_professionBehaviorTimer.Elapsed += (sendered, args) =>
                    {
                        inProfessionBehavior = true;
                        m_professionBehaviorTimer.Stop();
                        m_professionBehaviorTimer = null;
                    };
                    m_professionBehaviorTimer.Start();
                    m_professionBehaviorTimer.Enabled = true;
                }

                m_aiStatus = $"Random movement";
                IdleMovement(dt);
            }
        }
    }

    private bool HaveProfession() => profile.m_profession != NPC_Profession.None;

    private void Sleep()
    {
        if (IsSleeping()) return;
        var bed = house.GetBed();
        AttachStart(bed.m_spawnPoint, bed.gameObject, true, true, "attach_bed",
            new Vector3(0.0f, 0.4f, 0.0f), attachOffset: new Vector3(0.0f, 1, 0));
        m_aiStatus = $"Speeping";
        m_animator.SetBool(MonsterAI.s_sleeping, true);
        m_nview.GetZDO().Set(ZDOVars.s_sleeping, true);
        m_wakeupEffects.Create(transform.position, transform.rotation);
        m_sleeping = true;
        house.CloseAllDoors();
    }

    private void UpdateProfessionBehavior(float dt)
    {
        if (profile.m_profession != NPC_Profession.None)
        {
            inProfessionBehavior = true;
            switch (profile.m_profession)
            {
                case NPC_Profession.Crafter:
                    UpdateCrafter(dt);
                    break;
                case NPC_Profession.Builder:
                    UpdateBuilder(dt);
                    break;
            }
        }
    }

    private void UpdateBuilder(float dt)
    {
        if (!house || !house.town) return;
        if (!targetBuilding) targetBuilding = house.town.FindWornBuilding();
        if (!targetBuilding)
        {
            inProfessionBehavior = false;
            m_aiStatus = "All buildings are okay";
            Debug($"{profile.name} {m_aiStatus}");
            return;
        }
        else
        {
            var buildingPos = targetBuilding.transform.position;
            if (MoveTo(dt, buildingPos, targetBuilding.m_piece.m_blockRadius + 2, false))
            {
                LookAt(buildingPos);
                if (IsLookingAt(buildingPos, 20f))
                {
                    var buildingName = targetBuilding.GetPrefabName();
                    m_aiStatus = $"Repairs {buildingName}";
                    human.m_zanim.SetTrigger("swing_hammer");
                    targetBuilding.m_piece.m_placeEffect.Create(buildingPos,
                        targetBuilding.transform.rotation);
                    human.UseStamina(5);
                    Debug($"{profile.name} repared {buildingName}");
                    targetBuilding = null;
                }
            }
        }
    }

    private void UpdateCrafter(float dt)
    {
        if (inCrafting) return;
        if (profile.itemsToCraft == null || profile.itemsToCraft.Count == 0) return;
        var randomItemToCraft =
            profile.itemsToCraft.Random();
        GoCraftItem(dt, randomItemToCraft);
    }

    private void GoCraftItem(float dt, CrafterItem crafterItem)
    {
        if (!house || crafterItem == null) return;
        if (!house.HaveEmptySlot())
        {
            m_aiStatus = $"Don't have free space for crafting.";
            return;
        }

        var recipe = ObjectDB.instance.GetRecipe(crafterItem.prefab.m_itemData);
        if (!recipe)
        {
            DebugError($"Can't find recipe {crafterItem.prefabName}");
            return;
        }

        m_aiStatus += $"\nGoing to craft {recipe.name}";
        CraftingStation craftingStation = null;
        if (HaveRequirementsForRecipe(recipe, out craftingStation))
        {
            m_aiStatus += $"\nHave requirements for {recipe.name}";

            var stationPos = craftingStation.transform.position;
            if (MoveTo(dt, stationPos, craftingStation.m_useDistance * 0.8f, false))
            {
                LookAt(stationPos);
                if (IsLookingAt(stationPos, 20f))
                {
                    inCrafting = true;
                    craftingStation.PokeInUse();
                    human.HideHandItems();
                    human.m_zanim.SetInt("crafting", craftingStation.m_useAnimation);
                    StartCoroutine(CraftItem(dt, craftingStation, crafterItem, recipe));
                }
            }
        }
        else
        {
            inCrafting = false;
            human.m_zanim.SetInt("crafting", 0);
            m_aiStatus = $"Don't have enough resources to craft {recipe.name}";
        }
    }

    private IEnumerator CraftItem(float dt, CraftingStation craftingStation, CrafterItem crafterItem, Recipe recipe)
    {
        m_aiStatus = $"\nCrafting {recipe.name}...";
        foreach (var resource in recipe.m_resources)
        {
            var resourceName = resource.m_resItem.m_itemData.m_shared.m_name;
            yield return new WaitForSeconds(2);
            m_aiStatus += $"\nSpends {resourceName}...";
            house.RemoveItemFromInventory(resourceName);
        }

        m_aiStatus += $"\nFinishing crafting {recipe.name}...";
        yield return new WaitForSeconds(2);
        var itemDrop = Instantiate(recipe.m_item, craftingStation.transform.position, Quaternion.identity);
        itemDrop.m_itemData.m_durability = itemDrop.m_itemData.GetMaxDurability();
        if (house.AddItem(itemDrop))
        {
            itemDrop.m_nview.Destroy();
            //TODO: emote
        }

        inCrafting = false;
        human.m_zanim.SetInt("crafting", 0);
        inProfessionBehavior = false;
    }

    private bool HaveRequirementsForRecipe(Recipe recipe, out CraftingStation station)
    {
        var stationForRecipe = HaveCraftingStationForRecipe(recipe, out station);
        return HaveItemsForRecipe(recipe) && stationForRecipe;
    }

    private bool HaveCraftingStationForRecipe(Recipe recipe, out CraftingStation station)
    {
        station = house.GetCraftingStations().Find(x => x.m_name == recipe.m_craftingStation.m_name);
        return station;
    }

    private bool HaveItemsForRecipe(Recipe recipe)
    {
        var inventory = house.GetHouseInventory();
        foreach (var resource in recipe.m_resources)
        {
            var findItem = inventory.Find(x => x.m_shared.m_name == resource.m_resItem.m_itemData.m_shared.m_name);
            if (findItem == null) return false;
            if (findItem.m_stack < resource.m_amount) return false;
        }

        return true;
    }

    private void AttachStart(Transform attachPoint, GameObject colliderRoot, bool hideWeapons, bool isBed,
        string attachAnimation, Vector3 detachOffset, Vector3 attachOffset, Transform cameraPos = null)
    {
        if (m_attached)
            return;
        m_attached = true;
        m_attachPoint = attachPoint;
        m_detachOffset = detachOffset;
        m_attachOffset = attachOffset;
        m_attachAnimation = attachAnimation;
        m_attachPointCamera = cameraPos;
        human.m_zanim.SetBool(attachAnimation, true);
        human.m_nview.GetZDO().Set(ZDOVars.s_inBed, isBed);
        if ((UnityEngine.Object)colliderRoot != (UnityEngine.Object)null)
        {
            m_attachColliders = colliderRoot.GetComponentsInChildren<Collider>();
            ZLog.Log(("Ignoring " + m_attachColliders.Length.ToString() + " colliders"));
            foreach (Collider attachCollider in m_attachColliders)
                Physics.IgnoreCollision(human.m_collider, attachCollider, true);
        }

        if (hideWeapons)
            human.HideHandItems();
        UpdateAttach();
        human.ResetCloth();
    }

    public void UpdateTarget(Humanoid humanoid, float dt, out bool canHearTarget, out bool canSeeTarget)
    {
        m_unableToAttackTargetTimer -= dt;
        m_updateTargetTimer -= dt;
        if (m_updateTargetTimer <= 0f && !m_character.InAttack())
        {
            m_updateTargetTimer = Player.IsPlayerInRange(transform.position, 50f) ? 2f : 6f;
            Character enemy = FindEnemy();
            if (enemy) m_targetCreature = enemy;
        }

        if (m_targetCreature && m_targetCreature.IsDead()) m_targetCreature = null;
        if (m_targetCreature && !IsEnemy(m_targetCreature)) m_targetCreature = null;
        canHearTarget = false;
        canSeeTarget = false;
        if (m_targetCreature)
        {
            canHearTarget = CanHearTarget(m_targetCreature);
            canSeeTarget = CanSeeTarget(m_targetCreature);
            if (canSeeTarget | canHearTarget)
                m_timeSinceSensedTargetCreature = 0;
            SetTargetInfo(m_targetCreature.GetZDOID());
            if (!m_targetCreature.m_onDeath.GetInvocationList().Contains(OnTargetDeath))
                m_targetCreature.m_onDeath += OnTargetDeath;
        }
        else SetTargetInfo(ZDOID.None);

        m_timeSinceSensedTargetCreature += dt;
        if (!IsAlerted() && !(m_targetCreature != null)) return;
        m_timeSinceAttacking += dt;
        float num1 = 60f;
        float num2 = Vector3.Distance(m_spawnPoint, transform.position);
        bool flag = HuntPlayer() && m_targetCreature &&
                    m_targetCreature.IsPlayer();
        if (m_timeSinceSensedTargetCreature <= 30f && (flag || m_timeSinceAttacking <= num1 &&
                (m_maxChaseDistance <= 0f || m_timeSinceSensedTargetCreature <= 1f ||
                 num2 <= m_maxChaseDistance))) return;
        SetAlerted(false);
        m_targetCreature = null;
        m_timeSinceAttacking = 0;
        m_updateTargetTimer = 5f;
    }

    private void OnTargetDeath()
    {
        Debug($"{profile.name} {m_aiStatus}");
        m_aiStatus = "Target dead, go for loot";
    }

    public bool DoAttack(Character target, bool isFriend)
    {
        ItemData currentWeapon = human.GetCurrentWeapon();
        if (currentWeapon == null || !CanUseAttack(currentWeapon)) return false;
        int num = human.StartAttack(target, false) ? 1 : 0;
        if (num == 0) return num != 0;
        m_timeSinceAttacking = 0;
        return num != 0;
    }

    public bool UpdateFooding_falseNotNeed(Humanoid humanoid, float dt)
    {
        if (!IsHungry()) return false;
        m_consumeSearchTimer += dt;
        if (m_consumeSearchTimer > m_consumeSearchInterval)
        {
            m_consumeSearchTimer = 0;
            if (!IsHungry()) return false;
            FindFood();
        }

        if (!foodHouse || m_currentConsumeItem == null || !m_containerToGrab) return false;

        m_aiStatus = "Looking for food";
        if (MoveTo(dt, m_containerToGrab.transform.position, 2, m_aiStatus == "Can't reach food house"))
        {
            LookAt(m_containerToGrab.transform.position);
            //if ( IsLookingAt(m_containerToGrab.transform.position, 20f) && )
            //{
            m_onConsumedItem?.Invoke(m_currentConsumeItem);
            humanoid.m_consumeItemEffects?.Create(transform.position, Quaternion.identity);
            m_animator.SetTrigger("eat");
            Debug($"{profile.name} ate the {m_currentConsumeItem.m_shared.m_name.Localize()}");
            m_currentConsumeItem = null;
            foodHouse = null;
            var inventory = m_containerToGrab.GetInventory();
            inventory.RemoveItem(m_currentConsumeItem.m_shared.m_name, 1);
            //human.ConsumeItem(inventory, inventory.GetItem(m_currentConsumeItem.m_shared.m_name));
            return true;
            //}
        }
        else
        {
            foodHouse.OpenAllDoors();
            m_aiStatus = "Can't reach food house";
            return false;
        }

        return false;

        //false means don't need to eat.
    }

    public void OnConsumedItem(ItemData item)
    {
        m_sootheEffect.Create(human.GetCenterPoint(), Quaternion.identity);
        ResetHungryTimer();
    }

    public void UpdateAttach()
    {
        if (!m_attached)
            return;
        if (m_attachPoint != null)
        {
            transform.position = m_attachPoint.position + m_attachOffset;
            transform.rotation = m_attachPoint.rotation;
            Rigidbody componentInParent = m_attachPoint.GetComponentInParent<Rigidbody>();
            m_body.useGravity = false;
            m_body.velocity = componentInParent ? componentInParent.GetPointVelocity(transform.position) : Vector3.zero;
            m_body.angularVelocity = Vector3.zero;
            human.m_maxAirAltitude = transform.position.y;
        }
        else
            AttachStop();
    }

    public void AttachStop()
    {
        if (!m_attached) return;
        if (m_attachPoint != null) transform.position = m_attachPoint.TransformPoint(m_detachOffset);
        if (m_attachColliders != null)
        {
            foreach (Collider attachCollider in m_attachColliders)
            {
                if (attachCollider)
                    Physics.IgnoreCollision(human.m_collider, attachCollider, false);
            }

            m_attachColliders = null;
        }

        m_body.useGravity = true;
        m_attached = false;
        m_attachPoint = null;
        m_attachPointCamera = null;
        human.m_zanim.SetBool(m_attachAnimation, false);
        m_nview.GetZDO().Set(ZDOVars.s_inBed, false);
        human.ResetCloth();
    }

    private bool IsHungry()
    {
        if (!m_nview) return false;
        ZDO zdo = m_nview.GetZDO();
        if (zdo == null) return false;
        DateTime dateTime = new DateTime(zdo.GetLong(ZDOVars.s_tameLastFeeding));
        return (ZNet.instance.GetTime() - dateTime).TotalSeconds > m_eatDuration;
    }

    public void ResetHungryTimer() => m_nview.GetZDO().Set(ZDOVars.s_tameLastFeeding, ZNet.instance.GetTime().Ticks);

    private void FindFood()
    {
        var result = new List<NPC_House>();
        result = house.town.FindFoodHouses();
        if (result == null || result.Count == 0) return;

        foreach (var npcHouse in result)
        {
            foodHouse = npcHouse;
            var inventory = foodHouse.GetHouseInventory();
            List<ItemData> food = foodHouse.GetItemsByType(ItemType.Consumable, out Container container);
            if (food == null || food.Count == 0)
            {
                m_currentConsumeItem = null;
                m_containerToGrab = null;
                continue;
            }

            m_currentConsumeItem = food[0];
            m_containerToGrab = container;
            return;
        }
    }

    public void UpdateSleep(float dt)
    {
        if (!IsSleeping()) return;
        if (EnvMan.instance.IsDay())
        {
            Wakeup();
            return;
        }

        // if (m_wakeupRange > 0)
        // {
        //     var closestEnemy = FindClosestEnemy(m_character, transform.position, m_wakeupRange);
        //     if (closestEnemy && !closestEnemy.InGhostMode() && !closestEnemy.IsDebugFlying())
        //     {
        //         Wakeup();
        //         return;
        //     }
        // }
    }

    public new bool IsSleeping() => m_nview.IsValid() && m_nview.GetZDO().GetBool(ZDOVars.s_sleeping, m_sleeping);

    public ItemData SelectBestAttack(Humanoid humanoid, float dt)
    {
        if (m_targetCreature)
        {
            m_updateWeaponTimer -= dt;
            if (m_updateWeaponTimer <= 0f && !m_character.InAttack())
            {
                m_updateWeaponTimer = 1f;
                Character hurtFriend;
                Character friend;
                HaveFriendsInRange(m_viewRange, out hurtFriend, out friend);
                humanoid.EquipBestWeapon(m_targetCreature, null, hurtFriend, friend);
            }
        }

        return humanoid.GetCurrentWeapon();
    }

    private void UnequiAllWeapons()
    {
        human.UnequipItem(human.m_rightItem, false);
        human.UnequipItem(human.m_leftItem, false);
        human.UnequipItem(human.m_ammoItem, false);
        human.UnequipItem(human.m_shoulderItem, false);
        human.UnequipItem(human.m_utilityItem, false);
        //human.UnequipItem(human.m_chestItem, false);
        //human.UnequipItem(human.m_legItem, false);
        //human.UnequipItem(human.m_helmetItem, false);
    }

    public override void OnEnable()
    {
        base.OnEnable();
        allNPCs.Add(this);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        allNPCs.Remove(this);
    }

    public override void OnDamaged(float damage, Character attacker)
    {
        base.OnDamaged(damage, attacker);
        Wakeup();
        SetAlerted(true);
        SetTarget(attacker);
    }

    public void Wakeup()
    {
        if (!IsSleeping()) return;
        m_animator.SetBool(MonsterAI.s_sleeping, false);
        m_nview.GetZDO().Set(ZDOVars.s_sleeping, false);
        m_wakeupEffects.Create(transform.position, transform.rotation);
        m_sleeping = false;
        if (house) house.OpenAllDoors();
        AttachStop();
    }

    public void SetTarget(Character attacker)
    {
        if (!attacker || m_targetCreature || attacker.IsPlayer())
            return;
        m_targetCreature = attacker;
        m_lastKnownTargetPos = attacker.transform.position;
        m_beenAtLastPos = false;
    }

    public void Init(NPC_Profile profile_)
    {
        profile = profile_;
        // m_nview.GetZDO().Set("NPC_ID",
        //     (profile.name + Random.Range(int.MinValue, Int32.MaxValue).ToString()).GetStableHashCode());
        SaveProfile(profile);
        switch (profile.m_profession)
        {
            case NPC_Profession.Builder:
                if (!human.GetInventory().ContainsItemByName("$item_hammer"))
                    human.Pickup(ObjectDB.instance.GetItemPrefab("Hammer"));
                break;
        }

        house.OpenAllDoors();
    }

    public void SetHouse(NPC_House npcHouse)
    {
        house = npcHouse;
        m_randomMoveRange = house.town.GetRadius();
        m_spawnPoint = house.town.transform.position;
        m_patrol = false;
        m_patrolPoint = house.town.transform.position;
        SetPatrolPoint(transform.position);
        npcHouse.RegisterNPC(this);
        npcHouse.OpenAllDoors();
    }

    public NPC_Profile GetSavedProfile()
    {
        return TownDB.GetProfile(m_nview.GetZDO().GetString("ProfileName"));
    }

    public void SaveProfile(NPC_Profile profile)
    {
        m_nview.GetZDO().Set("ProfileName", profile.name);
    }

    public string GetHoverText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{profile.name}:");
        sb.AppendLine($"{(profile.m_profession == NPC_Profession.None ? "" : "Profession: " + profile.m_profession)}");
        sb.AppendLine($"Ai: {m_aiStatus}");
        sb.AppendLine($"Hungry: {IsHungry()}");
        sb.AppendLine($"Sleeping: {IsSleeping()}");
        sb.AppendLine($"{Const.UseKey} to talk");

        return sb.ToString().Localize();
    }

    public string GetHoverName() =>
        $"{profile.name}{(profile.m_profession == NPC_Profession.None ? "" : $" ({profile.m_profession})")}";

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold) return false;
        return true;
    }

    public bool UseItem(Humanoid user, ItemData item)
    {
        return false;
    }

    private bool AvoidBurning(float dt)
    {
        EffectArea effectArea = EffectArea.IsPointInsideArea(transform.position, EffectArea.Type.Burning, 2f);
        if (effectArea)
        {
            RandomMovementArroundPoint(dt, effectArea.transform.position, (effectArea.GetRadius() + 3) * 1.5f,
                IsAlerted());
        }

        return false;
    }
}