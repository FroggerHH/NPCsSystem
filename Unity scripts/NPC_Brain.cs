using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Extensions;
using Extensions.Valheim;
using UnityEngine;
using UnityEngine.UI;
using static NPCsSystem.Plugin;
using static ItemDrop;
using static ItemDrop.ItemData.AiTarget;
using static ItemDrop.ItemData.ItemType;
using Random = UnityEngine.Random;

namespace NPCsSystem;

public class NPC_Brain : BaseAI, Hoverable, Interactable
{
    internal static HashSet<NPC_Brain> allNPCs = new();
    private static float sayChance = 0.0002f;
    private static float chanceToChangeTargetCraftItem = 0.01f;

    public NPC_Profile defaultProfile;
    public Rigidbody m_body;
    public bool m_despawnInDay;
    public bool m_eventCreature;
    public float m_interceptTime;
    public float m_professionBehaviorInterval = 100000;
    public float m_updateTargetTimer;
    public float m_interceptTimeMax;
    public float m_interceptTimeMin;
    public EffectList m_wakeupEffects = new();
    public float m_sleepDelay = 0.5f;
    public Character m_targetCreature;
    public Vector3 m_lastKnownTargetPos = Vector3.zero;
    public bool m_beenAtLastPos;
    public string m_aiStatus = string.Empty;
    public string m_aiStatus_Sub = string.Empty;
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
    public float m_wakeupRange = 15;
    public float m_consumeSearchTimer;
    public float m_eatDuration = 1000f;
    public EffectList m_sootheEffect = new();

    [Header("Attach")] public bool m_attached;
    public Text ai_statusText;

    private readonly bool requestedInit = false;
    private CraftingStation currentCraftingStation;

    private Pickable currentPickable;
    internal NPC_House entertainmentHouse;

    private readonly float farmWorkTimer = 150;
    private NPC_House foodHouse;
    private GameObject hammerMark;
    internal Humanoid human;
    private bool inCrafting;
    private Vector3 lootPos;
    private string m_attachAnimation = "";
    private Collider[] m_attachColliders;
    private Vector3 m_attachOffset = Vector3.zero;
    private Transform m_attachPoint;
    private Transform m_attachPointCamera;
    private Container m_containerToGrab;
    private ItemData m_currentConsumeItem;
    private Vector3 m_detachOffset = Vector3.zero;
    private Action<ItemData> m_onConsumedItem;
    private float m_pauseTimer;
    private System.Timers.Timer m_professionBehaviorTimer;
    private bool m_sleeping;
    private (string, GameObject) plantObject = new(string.Empty, null);

    private PlantStateInfo plantPoint;

    internal NPC_Profile profile;
    private CrafterItem randomItemToCraft;
    internal NPC_House sleepHouse;
    private WearNTear targetBuilding;
    internal NPC_Town town;
    internal Trader trader;
    internal NPC_House warehouse;
    internal NPC_House workHouse;

    public override void Awake()
    {
        base.Awake();
        human = m_character as Humanoid;
        trader = GetComponent<Trader>();
        m_body = GetComponent<Rigidbody>();
        m_despawnInDay = false;
        m_eventCreature = false;
        m_animator.SetBool(MonsterAI.s_sleeping, IsSleeping());
        m_onConsumedItem += OnConsumedItem;
        m_interceptTime = Random.Range(m_interceptTimeMin, m_interceptTimeMax);
        if (m_circleTargetInterval <= 0) m_circleTargetInterval = 1;
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
        var findChild_ai_statusText = Utils.FindChild(transform, "Ai status");
        if (findChild_ai_statusText) ai_statusText = findChild_ai_statusText.GetComponent<Text>();
        hammerMark = Utils.FindChild(transform, "HammerMark").gameObject;
        hammerMark.SetActive(false);
    }

    public void Start()
    {
        if (!m_nview || !m_nview.IsValid() || !m_nview.IsOwner()) return;
        if (!human)
        {
            DebugError($"NPC {profile.name} is not a Humanoid");
            return;
        }

        human.m_name = profile.name + $"({profile.m_profession})";
        name = profile.name + " (Clone)";

        if (profile.IsWarrior()) human.EquipBestWeapon(null, null, null, null);
        else UnequipAllWeapons();


        Invoke(nameof(RequestTownInit), 4);
    }

    public void FixedUpdate()
    {
        var fixedDeltaTime = Time.fixedDeltaTime;
        if (m_nview.GetZDO() == null)
            return;
        if (!m_nview.IsOwner())
            return;
        if (human.IsDead())
            return;
        UpdateAttach();
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

    public string GetHoverText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{profile.name}:");
        if (m_aiStatus_Sub != string.Empty) sb.AppendLine($"Ai sub: {m_aiStatus_Sub}");
        sb.AppendLine($"Ai: {m_aiStatus}");
        sb.AppendLine($"Hungry: {IsHungry()}");
        sb.AppendLine($"Sleeping: {IsSleeping()}");
        if (profile.IsTrader() && IsTimeToWork())
            sb.AppendLine($"{Const.UseKey.Localize()} {"$npc_toTrade".Localize()}");

        return sb.ToString();
    }

    public string GetHoverName()
    {
        return $"{profile.name}{(profile.m_profession == NPC_Profession.None ? "" : $" ({profile.m_profession})")}";
    }

    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold) return false;
        if (profile.IsTrader() && IsTimeToWork()) return trader.Interact(user, hold, alt);
        return false;
    }

    public bool UseItem(Humanoid user, ItemData item)
    {
        return false;
    }

    private void RequestTownInit()
    {
        if (workHouse == null && profile.HasProfession() && requestedInit == false) town.InitAllNPCs();
    }

    public void UpdateAiStatus()
    {
        if (!ai_statusText) return;
        ai_statusText.text = m_aiStatus;
    }

    public new void UpdateAI(float dt)
    {
        UpdateAiStatus();
        UpdateAttach();
        if (!m_nview.IsOwner()) return;
        if (!UpdateSleep(dt) && !IsSleeping())
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

                var itemData = SelectBestAttack(human, dt);
                var flag = itemData != null &&
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
                else if (itemData.m_shared.m_aiTargetType == Enemy && IsAlerted())
                {
                    if (!m_targetCreature)
                        return;
                    if (canHearTarget | canSeeTarget)
                    {
                        m_beenAtLastPos = false;
                        m_lastKnownTargetPos = m_targetCreature.transform.position;
                        var num1 = Vector3.Distance(m_lastKnownTargetPos, transform.position) -
                                   m_targetCreature.GetRadius();
                        var num2 = m_alertRange * m_targetCreature.GetStealthFactor();
                        if (canSeeTarget && num1 < num2)
                            SetAlerted(true);
                        var num3 = num1 < itemData.m_shared.m_aiAttackRange ? 1 : 0;
                        if (num3 == 0 || !canSeeTarget || itemData.m_shared.m_aiAttackRangeMin < 0.0 ||
                            !IsAlerted())
                        {
                            m_aiStatus = "Move closer";
                            var velocity = m_targetCreature.GetVelocity();
                            var vector3 = velocity * m_interceptTime;
                            var lastKnownTargetPos = m_lastKnownTargetPos;
                            if (num1 > vector3.magnitude / 4.0)
                                lastKnownTargetPos += velocity * m_interceptTime;
                            MoveTo(dt, lastKnownTargetPos, 0.0f, IsAlerted());
                            if (m_timeSinceAttacking > 15.0)
                                m_unableToAttackTargetTimer = 15f;
                        }
                        else
                        {
                            StopMoving();
                        }

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
            }
        }
    }

    private void PickupLootFromDeadEnemy(float dt)
    {
        if (FindPath(lootPos))
        {
            if (MoveTo(dt, lootPos, 3, false))
            {
                if (IsLookingAt(lootPos, 20f))
                {
                    var colliderArray =
                        Physics.OverlapSphere(transform.position, 8, MonsterAI.m_itemMask);
                    List<ItemDrop> drops = new();
                    var num1 = 999999f;
                    foreach (var collider in colliderArray)
                        if (collider.attachedRigidbody)
                        {
                            var component = collider.attachedRigidbody.GetComponent<ItemDrop>();
                            if (component && component.GetComponent<ZNetView>().IsValid()) drops.Add(component);
                        }

                    foreach (var itemDrop in drops)
                    {
                        itemDrop.m_itemData.m_customData.Add("NPC_loot", "true");
                        human.Pickup(itemDrop.gameObject);
                    }

                    m_aiStatus = "Putting loot from the enemy in the chest";
                    //TODO: Put the loot from the enemy in the chest
                }
            }
            else
            {
                m_aiStatus = "Moving to enemies loot";
            }
        }
        else
        {
            m_aiStatus = "Can't reach loot";
        }
    }

    private bool PutLootFromDeadEnemyToChest(float dt)
    {
        var result = false;
        if (!warehouse) warehouse = town.FindWarehouse(null);
        if (!warehouse) return false;
        var targetPos = warehouse.FindContainer().transform.position;
        if (FindPath(targetPos))
            if (MoveTo(dt, targetPos, 3, false))
            {
                var allItems = (human.GetInventory().GetAllItems().ToArray().Clone() as ItemData[]).ToList();
                foreach (var itemDrop in allItems)
                {
                    if (!itemDrop.m_customData.ContainsKey("NPC_loot")) continue;
                    itemDrop.m_customData.Remove("NPC_loot");
                    warehouse.AddItem(itemDrop);
                    human.GetInventory().RemoveItem(itemDrop);
                    result = true;
                }
            }

        return result;
    }

    private void UpdateNormalBehavior(float dt)
    {
        if (!sleepHouse || !town || IsSleeping()) return;

        if (m_aiStatus == "Target dead, go for loot")
        {
            PickupLootFromDeadEnemy(dt);
            return;
        }

        if (m_aiStatus == "Putting loot from the enemy in the chest")
            if (PutLootFromDeadEnemyToChest(dt))
                return;

        if (HaveProfession() && IsTimeToWork())
        {
            var updateProfessionBehavior = UpdateProfessionBehavior(dt);
            if (updateProfessionBehavior)
            {
                hammerMark.SetActive(true);
            }
            else
            {
                UpdateFreeTime(dt);
                hammerMark.SetActive(false);
            }
        }
        else
        {
            if (IsTimeToDinner())
                if (UpdateFooding_falseNotNeed(dt))
                    return;

            UpdateFreeTime(dt);
        }
    }

    private void UpdateFreeTime(float dt)
    {
        m_aiStatus = "Chilling (having free time)";
        UnequipAllWeapons();
        if (!entertainmentHouse) entertainmentHouse = town.FindEntertainmentHouse();
        //TrySaySmt(dt); //TODO: say something
        if (entertainmentHouse)
        {
            if (MoveTo(dt, entertainmentHouse.transform.position, entertainmentHouse.GetRadius(), false))
                RandomMovementArroundPoint(dt, entertainmentHouse.transform.position, entertainmentHouse.GetRadius(),
                    false);
        }
        else
        {
            IdleMovement(dt);
        }
    }

    internal void TrySaySmt()
    {
        var envMan = EnvMan.instance;
        if (!envMan) return;
        var text = string.Empty;
        if (envMan.IsDay())
            text = envMan.IsWet()
                ? profile.talksBadWeather_Day.Random()
                : profile.talksClearWeather_Day.Random();
        else
            text = envMan.IsWet()
                ? profile.talksBadWeather_Night.Random()
                : profile.talksClearWeather_Night.Random();

        Chat.instance.SetNpcText(gameObject, Vector3.up * 1.5f, 20, 4, profile.name,
            text.Localize(), false);
    }

    public static bool IsTimeToDinner()
    {
        if (time == -1) return false;
        return time > 12.5f && time < 14f;
    }

    public static bool IsTimeToWork()
    {
        if (time == -1) return false;
        return (time > 7.2f && time < 12.5f) || (time > 14f && time < 19f);
    }

    public static bool IsTimeToChill()
    {
        return !IsTimeToWork();
    }

    private bool HaveProfession()
    {
        return profile.m_profession != NPC_Profession.None;
    }

    private void Sleep()
    {
        if (IsSleeping()) return;
        var bed = sleepHouse.GetBedFor(profile.name);
        AttachStart(bed.m_spawnPoint, bed.gameObject, true, true, "attach_bed",
            new Vector3(0.0f, 0.4f, 0.0f), Vector3.zero);
        m_aiStatus = "Sleeping";
        m_animator.SetBool(MonsterAI.s_sleeping, true);
        m_nview.GetZDO().Set(ZDOVars.s_sleeping, true);
        m_wakeupEffects.Create(transform.position, transform.rotation);
        m_sleeping = true;
        sleepHouse.CloseAllDoors();
    }

    private bool UpdateProfessionBehavior(float dt)
    {
        switch (profile.m_profession)
        {
            case NPC_Profession.Crafter:
                return UpdateCrafter(dt);
            case NPC_Profession.Builder:
                return UpdateBuilder(dt);
            case NPC_Profession.Trader:
                return UpdateTrader(dt);
            case NPC_Profession.Farmer:
                return UpdateFarmer(dt);
            default:
                return false;
        }
    }

    private bool UpdateBuilder(float dt)
    {
        if (!sleepHouse || !town) return false;
        if (!targetBuilding) targetBuilding = town.FindWornBuilding();
        if (!targetBuilding)
        {
            m_aiStatus_Sub = "All buildings are okay";
            return false;
        }

        var buildingPos = targetBuilding.transform.position;
        if (FindPath(buildingPos))
        {
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
                    Debug($"{profile.name} repaired {buildingName}");
                    targetBuilding.Repair();
                    Chat.instance.SetNpcText(gameObject, Vector3.up * 1.5f, 20, 3, profile.name,
                        $"I repaired a {targetBuilding.m_piece.m_name.Localize()}", false);
                    targetBuilding = null;
                    m_aiStatus_Sub = string.Empty;
                    return true;
                }

                m_aiStatus = $"Can't look at {targetBuilding.m_piece.m_name.Localize()}";
                m_aiStatus_Sub = string.Empty;
                return false;
            }

            m_aiStatus = $"Moving to {targetBuilding.GetPrefabName()}";
            m_aiStatus_Sub = string.Empty;
            return true;
        }

        m_aiStatus_Sub = $"Can't reach {targetBuilding.GetPrefabName()}";
        m_aiStatus = $"Can't reach {targetBuilding.GetPrefabName()}";
        return false;
    }

    private bool UpdateTrader(float dt)
    {
        if (!sleepHouse || !town) return false;
        if (!workHouse)
        {
            m_aiStatus_Sub = "Don't have house to work";
            return false;
        }

        var workHousePos = workHouse.transform.position;
        if (FindPath(workHousePos))
        {
            if (MoveTo(dt, workHousePos, 0, false))
            {
                m_aiStatus_Sub = string.Empty;
                m_aiStatus = "Working";
                return true;
            }

            m_aiStatus = "Moving to workHouse";
            m_aiStatus_Sub = string.Empty;
            return true;
        }

        m_aiStatus_Sub = "Can't reach workHouse";
        m_aiStatus = m_aiStatus;
        return false;
    }

    private bool UpdateFarmer(float dt)
    {
        if (!sleepHouse || !town) return false;
        if (!workHouse)
        {
            m_aiStatus_Sub = "Don't have house to work";
            return false;
        }

        var haveAnyFarmWork = HaveAnyFarmWork();
        Debug($"Update farmer {profile.name}, haveAnyFarmWork = {haveAnyFarmWork}");
        if (!haveAnyFarmWork)
        {
            m_aiStatus_Sub = "All work is done";
            m_aiStatus = string.Empty;
            return false;
        }


        var workHousePos = workHouse.transform.position;
        if (FindPath(workHousePos))
        {
            if (MoveTo(dt, workHousePos, workHouse.GetRadius() * 1.1f, false))
            {
                //Working on farm
                m_aiStatus_Sub = string.Empty;
                m_aiStatus = m_aiStatus_Sub;

                return DoFarmWork(dt);
            }

            m_aiStatus = "Moving to farm";
            return true;
        }

        m_aiStatus_Sub = "Can't find valid path to work house";
        return false;
    }

    private bool DoFarmWork(float dt)
    {
        if (farmWorkTimer > 0f)
        {
            m_pauseTimer += dt;
            if (m_pauseTimer > farmWorkTimer)
            {
                if (m_pauseTimer > farmWorkTimer + m_circleTargetDuration)
                    m_pauseTimer = Random.Range(0f, farmWorkTimer / 10f);

                m_aiStatus = "Farm pause";
                return true;
            }
        }

        Debug($"{profile.name} do farm work 0");
        human.EquipItem(human.GetInventory().GetItem("Cultivator", isPrefabName: true));

        var pickablesInWorkHouse = workHouse.FindPickables();
        if (pickablesInWorkHouse.Count > 0)
        {
            if (currentPickable)
            {
                if (MoveTo(dt, currentPickable.transform.position, 2.5f, false))
                {
                    m_aiStatus = m_aiStatus_Sub = $"Collecting pickable {currentPickable.GetPrefabName()}";
                    currentPickable.m_nview.InvokeRPC("Pick");
                    currentPickable = null;

                    workHouse.PickupAllDrops(human, 1);
                    return true;
                }

                else m_aiStatus = $"Moving to {currentPickable.GetPrefabName()} to pick it up";

                return true;
            }
            else
            {
                currentPickable = pickablesInWorkHouse.Nearest(transform.position);
                if (currentPickable) return true;
                else return false;
            }

            Debug($"{profile.name} do farm work 1");
        }

        Debug($"{profile.name} do farm work 9");
        if (plantPoint)
        {
            PlaceNewPlant();
            plantPoint = null;
            return true;
        }
        else
        {
            plantPoint = workHouse.plantStateInfos.Where(x => x.HavePlant is false).ToList().Random();
            if (plantPoint) return true;

            m_aiStatus_Sub = "Cannot find valid plant on farm";
            return false;
        }
    }

    private bool HaveAnyFarmWork()
    {
        return workHouse.plantStateInfos.Any(x => x.HavePlant == false) || workHouse.FindPickables().Count > 0;
    }

    private void PlaceNewPlant()
    {
        if (plantObject.Item1 == string.Empty) plantObject = profile.plants.Random();

        if (plantObject.Item1 == string.Empty)
        {
            m_aiStatus_Sub = $"Error getting random plant from {profile.name} profile";
            return;
        }

        var pointPosition = plantPoint.position;
        if (MoveTo(Time.deltaTime, pointPosition, 2.5f, false))
        {
            var plantGameObject = Instantiate(plantObject.Item2, pointPosition, plantObject.Item2.transform.rotation);
            var snapToGround = plantGameObject.AddComponent<SnapToGround>();
            snapToGround.Snap();
            Destroy(snapToGround);
            plantPoint.plantObject = plantGameObject.GetComponent<Plant>();

            if (!NPC_House.FindHouse(plantGameObject.transform.position))
            {
                Debug($"{plantGameObject.GetPrefabName()} is not in house");
                ZNetScene.instance.Destroy(plantGameObject);
                workHouse.plantStateInfos.ToList().ForEach(x => x.plantObject = null);
            }
        }
        else
        {
            m_aiStatus = "Moving to plant a seed";
        }
    }

    [Description("False if can't")]
    private bool UpdateCrafter(float dt)
    {
        if (!workHouse)
        {
            m_aiStatus_Sub = "Don't have work house";
            return false;
        }

        if (inCrafting) return true;
        if (profile.itemsToCraft == null || profile.itemsToCraft.Count == 0) return false;
        if (randomItemToCraft == null)
        {
            randomItemToCraft = profile.itemsToCraft.Random();
            warehouse = null;
        }

        return GoCraftItem(dt, randomItemToCraft);
    }

    private bool GoCraftItem(float dt, CrafterItem crafterItem)
    {
        if (crafterItem == null || crafterItem.prefab == null) return false;
        if (workHouse == null) return false;
        if (!workHouse.HaveEmptySlot())
        {
            m_aiStatus = "Don't have free space for crafting.";
            m_aiStatus_Sub = m_aiStatus;
            inCrafting = false;
            return false;
        }


        if (workHouse.GetItemsCountInHouse(crafterItem.prefab.m_itemData.m_shared.m_name) >=
            crafterItem.maxCountToCraft)
        {
            m_aiStatus_Sub = string.Empty;
            inCrafting = false;
            return false;
        }


        var recipe = ObjectDB.instance.GetRecipe(crafterItem.prefab.m_itemData);
        if (!recipe)
        {
            DebugError($"Can't find recipe {crafterItem.prefabName}");
            inCrafting = false;
            return false;
        }

        m_aiStatus += $"\nGoing to craft {recipe.name}";
        CraftingStation craftingStation = null;
        if (HaveRequirementsForRecipe(recipe, out craftingStation))
        {
            m_aiStatus += $"\nHave requirements for {recipe.name}";

            if (craftingStation)
            {
                currentCraftingStation = craftingStation;
                var stationPos = currentCraftingStation.transform.position;
                if (FindPath(stationPos))
                {
                    if (MoveTo(dt, stationPos, currentCraftingStation.m_useDistance * 0.8f, false))
                    {
                        LookAt(stationPos);
                        if (IsLookingAt(stationPos, 20f))
                        {
                            currentCraftingStation.PokeInUse();
                            human.HideHandItems();
                            human.m_zanim.SetInt("crafting", currentCraftingStation.m_useAnimation);
                            StartCoroutine(CraftItem(dt, crafterItem, recipe));
                            m_aiStatus_Sub = string.Empty;
                            return true;
                        }

                        m_aiStatus = $"Can't look at {currentCraftingStation.m_name.Localize()}";
                        m_aiStatus_Sub = string.Empty;
                        return false;
                    }

                    m_aiStatus = $"Moving to crafting station {currentCraftingStation.GetPrefabName()}";
                    m_aiStatus_Sub = string.Empty;
                    return true;
                }

                m_aiStatus = $"Can't reach crafting station {currentCraftingStation.GetPrefabName()}";
                m_aiStatus_Sub = m_aiStatus;
                return false;
            }

            human.HideHandItems();
            human.m_zanim.SetInt("crafting", 1);
            StartCoroutine(CraftItem(dt, crafterItem, recipe));
            m_aiStatus_Sub = string.Empty;
            return true;
        }

        inCrafting = false;
        human.m_zanim.SetInt("crafting", 0);
        m_aiStatus = $"Don't have enough resources to craft {recipe.name}";
        CheckRequests();
        return false;
    }

    private void CheckRequests()
    {
        List<Request> requests = new();
        foreach (var request in town.GetRequests())
        {
            if (request.requestType == RequestType.Item && request.npcName == profile.name) continue;
            requests.Add(request);
        }

        town.requests = requests;
        Dictionary<string, int> items = new();

        foreach (var crafterItem in profile.itemsToCraft)
            if (!HaveItemsForRecipe(crafterItem.recipe))
                foreach (var tuple in crafterItem.recipe.ToListStr())
                    if (items.ContainsKey(tuple.Item1)) items[tuple.Item1] += tuple.Item2;
                    else
                        items.Add(tuple.Item1, tuple.Item2);

        if (items.Count > 0) town.RegisterNPCRequest(new Request(RequestType.Item, profile.name, items));
    }

    private IEnumerator CraftItem(float dt, CrafterItem crafterItem, Recipe recipe)
    {
        inCrafting = true;
        m_aiStatus = $"Crafting {recipe.name}...";
        foreach (var resource in recipe.m_resources)
        {
            var resourceName = resource.m_resItem.m_itemData.m_shared.m_name;
            yield return new WaitForSeconds(2);
            m_aiStatus += $"\nSpends {resourceName}...";
            warehouse.RemoveItemFromInventory(resourceName);
        }

        m_aiStatus += $"\nFinishing crafting {recipe.name}...";
        yield return new WaitForSeconds(2);
        var pos = currentCraftingStation
            ? currentCraftingStation.transform.position
            : transform.position + transform.up + new Vector3(0, 1, 0);
        var itemDrop = Instantiate(recipe.m_item, pos, Quaternion.identity);
        itemDrop.m_itemData.m_durability = itemDrop.m_itemData.GetMaxDurability();
        itemDrop.m_itemData.m_crafterName = $"{profile.name} (NPC)";
        itemDrop.m_itemData.m_crafterID = 88;
        Chat.instance.SetNpcText(gameObject, Vector3.up * 1.5f, 20, 3, profile.name,
            $"$npc_made_msg {itemDrop.m_itemData.m_shared.m_name}".Localize(), false);
        Emote("craft");
        if (workHouse.AddItem(itemDrop.m_itemData)) itemDrop.m_nview.Destroy();

        human.m_zanim.SetInt("crafting", 0);
        inCrafting = false;
        randomItemToCraft = null;
        warehouse = null;
    }

    private bool HaveRequirementsForRecipe(Recipe recipe, out CraftingStation station)
    {
        var stationForRecipe = HaveCraftingStationForRecipe(recipe, out station);
        return HaveItemsForRecipe(recipe) && stationForRecipe;
    }

    private bool HaveCraftingStationForRecipe(Recipe recipe, out CraftingStation station)
    {
        station = workHouse.GetCraftingStations().Find(x => x.m_name == recipe.m_craftingStation.m_name);
        return station;
    }

    private bool HaveItemsForRecipe(Recipe recipe)
    {
        if (npcsNoCost.Value) return true;
        var recipeList = recipe.ToList();
        if (!warehouse) warehouse = town.FindWarehouse(recipeList);

        if (!warehouse) return false;
        var inventory = warehouse.GetHouseInventory();
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
        if (colliderRoot != null)
        {
            m_attachColliders = colliderRoot.GetComponentsInChildren<Collider>();
            ZLog.Log("Ignoring " + m_attachColliders.Length + " colliders");
            foreach (var attachCollider in m_attachColliders)
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
            var enemy = FindEnemy();
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
        else
        {
            SetTargetInfo(ZDOID.None);
        }

        m_timeSinceSensedTargetCreature += dt;
        if (!IsAlerted() && !(m_targetCreature != null)) return;
        m_timeSinceAttacking += dt;
        var num1 = 60f;
        var num2 = Vector3.Distance(m_spawnPoint, transform.position);
        var flag = HuntPlayer() && m_targetCreature &&
                   m_targetCreature.IsPlayer();
        if (m_timeSinceSensedTargetCreature <= 30f && (flag || (m_timeSinceAttacking <= num1 &&
                                                                (m_maxChaseDistance <= 0f ||
                                                                 m_timeSinceSensedTargetCreature <= 1f ||
                                                                 num2 <= m_maxChaseDistance)))) return;
        SetAlerted(false);
        m_targetCreature = null;
        m_timeSinceAttacking = 0;
        m_updateTargetTimer = 5f;
    }

    private void OnTargetDeath()
    {
        m_aiStatus = "Target dead, go for loot";
        Debug($"{profile.name} {m_aiStatus}");
    }

    public bool DoAttack(Character target, bool isFriend)
    {
        var currentWeapon = human.GetCurrentWeapon();
        if (currentWeapon == null || !CanUseAttack(currentWeapon)) return false;
        var num = human.StartAttack(target, false) ? 1 : 0;
        if (num == 0) return num != 0;
        m_timeSinceAttacking = 0;
        return num != 0;
    }

    public bool UpdateFooding_falseNotNeed(float dt)
    {
        if (!IsHungry()) return false;
        m_consumeSearchTimer += dt;
        if (m_consumeSearchTimer > m_consumeSearchInterval &&
            (!foodHouse || m_currentConsumeItem == null || !m_containerToGrab))
        {
            m_consumeSearchTimer = 0;
            FindFood();
        }

        if (!foodHouse || m_currentConsumeItem == null || !m_containerToGrab) return false;

        m_aiStatus = "Looking for food";
        var chestPos = m_containerToGrab.transform.position;
        if (FindPath(chestPos))
        {
            if (MoveTo(dt, chestPos, 2, m_aiStatus == "Can't reach food house"))
            {
                LookAt(chestPos);
                m_onConsumedItem?.Invoke(m_currentConsumeItem);
                human.m_consumeItemEffects?.Create(transform.position, Quaternion.identity);
                m_animator.SetTrigger("eat");
                Debug($"{profile.name} ate the {m_currentConsumeItem.m_shared.m_name.Localize()}");
                var inventory = m_containerToGrab.GetInventory();
                inventory.RemoveItem(m_currentConsumeItem.m_shared.m_name, 1);
                m_containerToGrab.SetInUse(false);
                m_currentConsumeItem = null;
                foodHouse = null;
                m_containerToGrab = null;
                return true;
            }

            m_containerToGrab.SetInUse(true);
            m_aiStatus = "Moving to food house";
            return true;
        }

        foodHouse.OpenAllDoors();
        m_aiStatus = "Can't reach food house";
        return false;
    }

    public void OnConsumedItem(ItemData item)
    {
        m_sootheEffect.Create(human.GetCenterPoint(), Quaternion.identity);
        ResetHungryTimer();
    }

    public void UpdateAttach()
    {
        if (!m_attached) return;
        if (m_attachPoint)
        {
            transform.position = m_attachPoint.position + m_attachOffset;
            transform.rotation = m_attachPoint.rotation;
            var componentInParent = m_attachPoint.GetComponentInParent<Rigidbody>();
            m_body.useGravity = false;
            m_body.velocity = componentInParent
                ? componentInParent.GetPointVelocity(transform.position)
                : Vector3.zero;
            m_body.angularVelocity = Vector3.zero;
            human.m_maxAirAltitude = transform.position.y;
        }
        else
        {
            AttachStop();
        }
    }

    public void AttachStop()
    {
        if (!m_attached) return;
        if (m_attachPoint != null) transform.position = m_attachPoint.TransformPoint(m_detachOffset);
        if (m_attachColliders != null)
        {
            foreach (var attachCollider in m_attachColliders)
                if (attachCollider)
                    Physics.IgnoreCollision(human.m_collider, attachCollider, false);

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
        var zdo = m_nview.GetZDO();
        if (zdo == null) return false;
        var dateTime = new DateTime(zdo.GetLong(ZDOVars.s_tameLastFeeding));
        return (ZNet.instance.GetTime() - dateTime).TotalSeconds > m_eatDuration;
    }

    public void ResetHungryTimer()
    {
        m_nview.GetZDO().Set(ZDOVars.s_tameLastFeeding, ZNet.instance.GetTime().Ticks);
    }

    private void FindFood()
    {
        if (!town) return;
        var result = new List<NPC_House>();
        result = town.FindFoodHouses();
        if (result == null || result.Count == 0) return;

        foreach (var npcHouse in result)
        {
            foodHouse = npcHouse;
            var inventory = foodHouse.GetHouseInventory();
            var food = foodHouse.GetItemsByType(Consumable, out var container, true);
            if (food == null || food.Count == 0 || !container)
            {
                m_currentConsumeItem = null;
                m_containerToGrab = null;
                continue;
            }

            m_currentConsumeItem = food[0];
            m_containerToGrab = container;
            return;
        }

        if (!m_containerToGrab) town.RegisterNPCRequest(new Request(RequestType.Food, profile.name));
    }

    public bool UpdateSleep(float dt)
    {
        if (!sleepHouse || !town || !profile) return false;
        var isSleeping = IsSleeping();
        if (isSleeping && m_aiStatus_Sub == "Can't reach bed") m_aiStatus_Sub = string.Empty;
        if (isSleeping && !m_attached) Wakeup();
        if (IsTimeToWakeup() && isSleeping)
        {
            Wakeup();
            return true;
        }

        if (IsTimeToSleep() && !isSleeping)
        {
            m_aiStatus = "Going to sleep";
            var bedPos = sleepHouse.GetBedPos(profile.name);
            var hasBed = sleepHouse.HasBedFor(profile.name);
            if (FindPath(bedPos))
            {
                if (MoveTo(dt, bedPos, 3, false))
                {
                    if (hasBed)
                    {
                        var bed = sleepHouse.GetBedFor(profile.name);
                        LookAt(bedPos);
                        Sleep();
                        return true;
                    }

                    m_aiStatus = "Need a bed";
                    town.RegisterNPCRequest(new Request(RequestType.Bed, profile.name));
                    m_aiStatus_Sub = m_aiStatus;
                    return true;
                }

                m_aiStatus = "Moving to bed";
                m_aiStatus_Sub = string.Empty;
                return true;
            }

            m_aiStatus = "Can't reach bed";
            m_aiStatus_Sub = m_aiStatus;
            return true;
        }

        return false;
    }

    private static bool IsTimeToSleep()
    {
        return TimeUtils.GetCurrentTimeValue() > 23 || TimeUtils.GetCurrentTimeValue() < 7;
    }

    private static bool IsTimeToWakeup()
    {
        return TimeUtils.GetCurrentTimeValue() > 7;
    }

    public new bool IsSleeping()
    {
        return m_nview.IsValid() && m_nview.GetZDO().GetBool(ZDOVars.s_sleeping, m_sleeping);
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

    private void UnequipAllWeapons()
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

    public override void OnDamaged(float damage, Character attacker)
    {
        foreach (var brain in allNPCs) brain.SomeOneDamaged(damage, attacker);
    }

    public void SomeOneDamaged(float damage, Character attacker)
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
        if (sleepHouse) sleepHouse.OpenAllDoors();
        AttachStop();
        entertainmentHouse = null;
    }

    public void SetTarget(Character attacker)
    {
        if (!attacker || m_targetCreature || attacker.IsPlayer())
            return;
        m_targetCreature = attacker;
        m_lastKnownTargetPos = attacker.transform.position;
        m_beenAtLastPos = false;
        if (!m_targetCreature.m_onDeath.GetInvocationList().Contains(OnTargetDeath))
            m_targetCreature.m_onDeath += OnTargetDeath;
    }

    public void Init(NPC_Profile profile_)
    {
        profile = profile_;
        SaveProfile(profile);
        switch (profile.m_profession)
        {
            case NPC_Profession.Builder:
                human.GetInventory().GiveItemIfNotHave("Hammer");
                break;
            case NPC_Profession.Trader:
                trader.m_name = profile.name;
                break;
            case NPC_Profession.Farmer:
                human.GetInventory().GiveItemIfNotHave("Cultivator");
                human.GetInventory().GiveItemIfNotHave("Hoe");
                break;
        }

        human.m_visEquipment.m_models = ZNetScene.instance.GetPrefab("Player").GetComponent<VisEquipment>().m_models;

        if (profile.m_gender is NPC_Gender.Female) human.m_visEquipment.SetModel(1);

        sleepHouse.OpenAllDoors();
    }

    public void SetHouse(NPC_House sleepHouse_, NPC_House workHouse_)
    {
        sleepHouse = sleepHouse_;
        town = sleepHouse.town;
        workHouse = workHouse_;
        m_randomMoveRange = town.GetRadius();
        m_spawnPoint = town.transform.position;
        m_patrol = false;
        m_patrolPoint = town.transform.position;
        SetPatrolPoint(transform.position);
        sleepHouse.RegisterNPC(this);
        sleepHouse.OpenAllDoors();
    }

    public NPC_Profile GetSavedProfile()
    {
        return NPCsManager.GetNPCProfile(m_nview.GetZDO().GetString("ProfileName"));
    }

    public void SaveProfile(NPC_Profile profile)
    {
        m_nview.GetZDO().Set("ProfileName", profile.name);
    }

    private bool AvoidBurning(float dt)
    {
        var effectArea = EffectArea.IsPointInsideArea(transform.position, EffectArea.Type.Burning, 2f);
        if (effectArea)
            RandomMovementArroundPoint(dt, effectArea.transform.position, (effectArea.GetRadius() + 3) * 1.5f,
                IsAlerted());

        return false;
    }

    public void Emote(string emoteName)
    {
        Debug($"{profile.name} emote {emoteName}");
        //TODO: Implement emote
    }
}