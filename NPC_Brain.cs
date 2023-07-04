using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Random = UnityEngine.Random;
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
    public float m_pauseTimer;
    public float m_updateTargetTimer;
    public float m_interceptTimeMax;
    public float m_interceptTimeMin;
    public float m_wakeUpDelayMax;
    public EffectList m_wakeupEffects = new EffectList();
    public float m_wakeUpDelayMin;
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
    [Header("Consume items")] public float m_consumeRange = 2f;
    public float m_consumeSearchInterval = 10f;
    public NPC_House foodHouse;
    public float m_wakeupRange = 15;

    [FormerlySerializedAs("m_consumeTarget")]
    public ItemData m_currentConsumeItem;

    public float m_consumeSearchTimer;
    public Action<ItemData> m_onConsumedItem;
    public float m_eatDuration = 30f;
    private Container m_containerToGrab;

    [Header("Attach")] public bool m_attached;
    private string m_attachAnimation = "";
    private bool m_sleeping;
    private Transform m_attachPoint;
    private Vector3 m_detachOffset = Vector3.zero;
    private Transform m_attachPointCamera;
    private Collider[] m_attachColliders;
    public Text ai_statusText;
    private bool inCrafting = false;


    public override void Awake()
    {
        base.Awake();
        m_despawnInDay = false;
        m_eventCreature = false;
        m_animator.SetBool(MonsterAI.s_sleeping, IsSleeping());
        m_interceptTime = Random.Range(m_interceptTimeMin, m_interceptTimeMax);
        m_pauseTimer = Random.Range(0, m_circleTargetInterval);
        m_updateTargetTimer = Random.Range(0, 2f);
        if (m_wakeUpDelayMin > 0 || m_wakeUpDelayMax > 0)
            m_sleepDelay = Random.Range(m_wakeUpDelayMin, m_wakeUpDelayMax);
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

        human.m_name = profile.name;
        name = profile.name + " (Clone)";

        if (profile.IsWarrior()) human.EquipBestWeapon(null, null, null, null);
        else UnequiAllWeapons();
    }

    public new void UpdateAI(float dt)
    {
        if (!m_nview.IsOwner()) return;
        UpdateAiStatus();
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
                // if (m_targetCreature)
                // {
                //     if (EffectArea.IsPointInsideArea(
                //             m_targetCreature.transform.position, EffectArea.Type.PrivateProperty))
                //     {
                //         // Flee(dt, m_targetCreature.transform.position);
                //         // m_aiStatus = "Leaving someone else's territory";
                //         // return;
                //TODO: Leaving someone else's territory
                //     }
                // }
                // else
                // {
                //     if (PrivateArea.InsideFactionArea(transform.position, Character.Faction.Players))
                //     {
                //         Flee(dt, transform.position);
                //         m_aiStatus = "Avoid someone else's territory";
                //         return;
                //     }
                // }


                if (!IsAlerted() && UpdateConsumeItem(human, dt))
                {
                    m_aiStatus = "Consume item";
                }
                else
                {
                    if (m_circleTargetInterval > 0f && m_targetCreature)
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
                            UpdateNormalBehavior(dt);
                        }
                    }
                    else if (itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.Enemy)
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
                    else
                    {
                        if (itemData.m_shared.m_aiTargetType != ItemDrop.ItemData.AiTarget.FriendHurt &&
                            itemData.m_shared.m_aiTargetType != ItemDrop.ItemData.AiTarget.Friend)
                            return;
                        m_aiStatus = "Helping friend";
                        Character target = itemData.m_shared.m_aiTargetType == ItemDrop.ItemData.AiTarget.FriendHurt
                            ? HaveHurtFriendInRange(m_viewRange)
                            : HaveFriendInRange(m_viewRange);
                        if (target)
                        {
                            if (Vector3.Distance(target.transform.position, transform.position) <
                                itemData.m_shared.m_aiAttackRange)
                            {
                                if (flag)
                                {
                                    StopMoving();
                                    LookAt(target.transform.position);
                                    DoAttack(target, true);
                                }
                                else
                                    RandomMovement(dt, target.transform.position);
                            }
                            else
                                MoveTo(dt, target.transform.position, 0.0f, IsAlerted());
                        }
                        else
                            RandomMovement(dt, transform.position, true);
                    }
                }
            }
        }
    }

    private void UpdateNormalBehavior(float dt)
    {
        if (EnvMan.instance.IsNight())
        {
            m_aiStatus = $"Going to sleep";
            var bedPos = house.GetBedPos();
            var hasBed = house.HasBed();
            if (MoveTo(dt, bedPos, house.town.GetRadius() * 2, false))
            {
                if (hasBed)
                {
                    var bed = house.GetBed();
                    LookAt(bedPos);
                    if (IsLookingAt(bedPos, 20f))
                    {
                        AttachStart(bed.m_spawnPoint, bed.gameObject, true, true, "attach_bed",
                            new Vector3(0.0f, 0.9f, 0.0f));
                        m_aiStatus = $"Speeping";
                        m_animator.SetTrigger("consume");
                    }
                }
                else
                {
                    m_aiStatus = $"Need a bed";
                }
            }
        }
        else
        {
            if (Random.value < 0.15f || profile.m_profession == NPC_Profession.None)
            {
                m_aiStatus = $"Random movement";
                IdleMovement(dt);
            }
            else
            {
                if (profile.m_profession != NPC_Profession.None)
                {
                    switch (profile.m_profession)
                    {
                        case NPC_Profession.Crafter:
                            if (inCrafting) break;
                            if (profile.itemsToCraft == null || profile.itemsToCraft.Count == 0) break;
                            var randomItemToCraft =
                                profile.itemsToCraft.Random();
                            m_aiStatus = $"Crafting {randomItemToCraft.prefabName}";
                            GoCraftItem(dt, randomItemToCraft);
                            break;
                    }
                }
            }
        }
    }

    private void GoCraftItem(float dt, CrafterItem crafterItem)
    {
        if (!house.HaveEmptySlot())
        {
            m_aiStatus = $"Don't have free space for crafting.";
            return;
        }

        var recipe = ObjectDB.instance.GetRecipe(crafterItem.prefab.m_itemData);
        m_aiStatus = $"Going to craft {recipe.name}";
        CraftingStation craftingStation = null;
        if (HaveRequirementsForRecipe(recipe, out craftingStation))
        {
            var stationPos = craftingStation.transform.position;
            if (MoveTo(dt, stationPos, house.town.GetRadius() * 2, false))
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
        m_aiStatus = $"Crafting {recipe.name}...";
        foreach (var resource in recipe.m_resources)
        {
            var resourceName = resource.m_resItem.m_itemData.m_shared.m_name;
            yield return new WaitForSeconds(2);
            m_aiStatus = $"Crafting {recipe.name}... " + $" {resourceName}";
            house.RemoveItemFromInventory(resourceName);
        }

        m_aiStatus = $"Finishing crafting {recipe.name}...";
        yield return new WaitForSeconds(2);
        house.AddItem(recipe.m_item);
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
        string attachAnimation, Vector3 detachOffset, Transform cameraPos = null)
    {
        if (m_attached)
            return;
        m_attached = true;
        m_attachPoint = attachPoint;
        m_detachOffset = detachOffset;
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

    public bool DoAttack(Character target, bool isFriend)
    {
        ItemData currentWeapon = human.GetCurrentWeapon();
        if (currentWeapon == null || !CanUseAttack(currentWeapon)) return false;
        int num = human.StartAttack(target, false) ? 1 : 0;
        if (num == 0) return num != 0;
        m_timeSinceAttacking = 0;
        return num != 0;
    }

    public bool UpdateConsumeItem(Humanoid humanoid, float dt)
    {
        m_aiStatus = "Looking for food";
        m_consumeSearchTimer += dt;
        if (m_consumeSearchTimer > m_consumeSearchInterval)
        {
            m_consumeSearchTimer = 0;
            if (!IsHungry()) return false;
            FindFood();
        }

        if (!foodHouse) return false;
        m_aiStatus = "Going to food house";
        if (MoveTo(dt, m_containerToGrab.transform.position, m_consumeRange, false))
        {
            LookAt(m_containerToGrab.transform.position);
            if (IsLookingAt(m_containerToGrab.transform.position, 20f) &&
                m_containerToGrab.GetInventory().RemoveOneItem(m_currentConsumeItem))
            {
                m_onConsumedItem?.Invoke(m_currentConsumeItem);
                humanoid.m_consumeItemEffects.Create(transform.position, Quaternion.identity);
                m_animator.SetTrigger("consume");
                Debug($"Ate the {m_currentConsumeItem.m_shared.m_name.Localize()}");
                m_currentConsumeItem = null;
            }
        }

        return false;

        //false means don't need to eat.
    }

    public void UpdateAttach()
    {
        if (!m_attached)
            return;
        if (m_attachPoint != null)
        {
            transform.position = m_attachPoint.position;
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
        if (m_sleeping || !m_attached) return;
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
        foodHouse = house.town.FindFoodHouse();
        if (!foodHouse)
        {
            Debug($"Can't find food house");
            return;
        }

        Debug("Food house found");

        var inventory = foodHouse.GetHouseInventory();
        List<ItemData> food = foodHouse.GetItemsByType(ItemType.Consumable, out Container container);
        if (food == null || food.Count == 0) return;
        m_currentConsumeItem = food[0];
        m_containerToGrab = container;
    }

    public void UpdateSleep(float dt)
    {
        if (!IsSleeping()) return;
        if (EnvMan.instance.IsDay())
        {
            Wakeup();
            return;
        }

        if (m_wakeupRange > 0)
        {
            var closestEnemy = FindClosestEnemy(m_character, transform.position, m_wakeupRange);
            if (closestEnemy && !closestEnemy.InGhostMode() && !closestEnemy.IsDebugFlying())
            {
                Wakeup();
                return;
            }
        }
    }

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
    }

    public void SetHouse(NPC_House npcHouse)
    {
        house = npcHouse;
        m_randomMoveRange = house.town.GetRadius();
        m_spawnPoint = house.town.transform.position;
        m_patrol = false;
        m_patrolPoint = house.town.transform.position;
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

    public string GetHoverText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{profile.name}:");
        sb.AppendLine($"{(profile.m_profession == NPC_Profession.None ? "" : "Profession:" + profile.m_profession)}");
        sb.AppendLine($"$KEY_USE to talk");
        sb.AppendLine($"Ai: {m_aiStatus}");

        return sb.ToString().Localize();
    }

    public string GetHoverName() => profile.name;

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