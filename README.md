### Setup

1. Download 2 zips UnityDlls.zip and CodeDlls.zip
2. Extract files from UnityDlls to unity.
3. Extract files from CodeDlls to Valheim\BepInEx\plugins. NPCsExample.dll is optional.

### Unity

1. In unity create new mob, it mast have with NPC Brain component instead of MonsterAi. Add it to bundle.
2. In project files right click, select create, NPC_Profile. New file will appear, name it as you want your npc be
   named.
   In this file drag your npc prefab to prefab field, set its profession by dropdown, gender.
   If npc is ffarmer add to plants names names of plants prefabs he can plant.
   You can add m_defaultItems, m_randomWeapon, m_randomArmor, m_randomShield at in MonsterAI but by prefab name to
   decrease bundle size.

#### If your npc is trader you need to have individual prefad for it.

3. Create new location for town. Add it to bundle.
   Create new GameObject inside it named TownSettings. Add NPC_Town component to it, there you will see some settings.
   Make radius a bit bigger than location radius, houses needed is how much houses town has, must be exactly same as
   real number of houses in town. Drag npcs you want to live in this town to npcs field.

### Visual Studio

1. Register assetBundle in this way

```csharp
   bundle = PrefabManager.RegisterAssetBundle("bundlename");
```

2. Set up town location using LocationManager, but set GroupName = "NPCTowns".
3. Set up npc prefabs as creatures uning CreatureManager. Example:

```csharp
Creature mainNPS = new(bundle, "PlayerNPS")
{
    CanHaveStars = false,
    CanBeTamed = false,
    CanSpawn = false
};
```

4. Initialize NPCsManager

```csharp
NPCsManager.Initialize(bundle);
```

5. If you don't have any crafters npcs skip this step.
   This is how to add recipies to crafters:

```csharp
// Getting npc profile
var Bill = NPCsManager.GetNPCProfile("Bill");
Bill.AddCrafterItem("AxeBronze, maxCountToCraft:1");
Bill.AddCrafterItem("BronzeNails", maxCountToCraft:25);
```

6. This is how to add items to warehouses to have by default:

```csharp
NPCsManager.AddDefaultItemToHouse(houseName:"NPSHouseWarehouse", itemName:"CookedDeerMeat", count:80);
```

7. If you don't have any traders npcs skip this step.
   To add trades to traders you need to to use TraderUtils manager:

```csharp
new CustomTrade()
            .SetTrader("NPC_Ann") //Npc name
            .SetItem("Onion") // selling item 
            .SetPrice(25) // price
            .SetStack(5); // how many items are bought at once
```