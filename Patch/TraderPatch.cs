using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using ItemManager;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using static NPCsSystem.Plugin;
using static Heightmap;
using static Heightmap.Biome;
using static ZoneSystem;
using static ZoneSystem.ZoneVegetation;

namespace NPCsSystem;

[HarmonyPatch]
public class TraderPatch
{
    private static TradeItem current;
    private static Image moneyIcon;
    internal static ItemDrop coinPrefab;

    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Awake)), HarmonyPostfix, HarmonyWrapSafe]
    public static void StoreGui_Awake(StoreGui __instance)
    {
        moneyIcon = __instance.transform.Find("Store/coins/coin icon").GetComponent<Image>();
    }

    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.FillList)), HarmonyPrefix, HarmonyWrapSafe]
    public static bool StoreGui_FillList(StoreGui __instance)
    {
        if (__instance.m_trader.TryGetComponent(out NPC_Brain npc))
        {
            var index1 = __instance.GetSelectedItemIndex();

            var availableItems = npc.profile.tradeItems;
            foreach (GameObject go in __instance.m_itemList) Object.Destroy(go);
            __instance.m_itemList.Clear();
            __instance.m_listRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Max(__instance.m_itemlistBaseSize, (float)availableItems.Count * __instance.m_itemSpacing));


            for (int index2 = 0; index2 < availableItems.Count; ++index2)
            {
                var tradeItem = availableItems[index2];
                __instance.m_coinPrefab = tradeItem.moneyItem;
                var playerCoins = Player.m_localPlayer.GetInventory()
                    .CountItems(__instance.m_coinPrefab.m_itemData.m_shared.m_name);

                GameObject tradeeElement = Object.Instantiate(__instance.m_listElement, __instance.m_listRoot);
                var rectTransform = (tradeeElement.transform as RectTransform);
                tradeeElement.SetActive(true);

                rectTransform.anchoredPosition = new Vector2(0.0f, (float)index2 * -__instance.m_itemSpacing);
                bool flag = tradeItem.price <= playerCoins;
                Image iconImage = tradeeElement.transform.Find("icon").GetComponent<Image>();
                iconImage.sprite = tradeItem.prefab.m_itemData.m_shared.m_icons[0];
                iconImage.color = flag ? Color.white : new Color(1f, 0.0f, 1f, 0.0f);
                string str = Localization.instance.Localize(tradeItem.prefab.m_itemData.m_shared.m_name);
                if (tradeItem.stack > 1) str = str + " x" + tradeItem.stack.ToString();
                Text nameText = tradeeElement.transform.Find("name").GetComponent<Text>();
                nameText.text = str;
                nameText.color = flag ? Color.white : Color.grey;
                UITooltip tooltip = tradeeElement.GetComponent<UITooltip>();
                tooltip.m_topic = tradeItem.prefab.m_itemData.m_shared.m_name;
                tooltip.m_text = tradeItem.prefab.m_itemData.GetTooltip();
                Text priceText = Utils.FindChild(tradeeElement.transform, "price").GetComponent<Text>();
                priceText.text = tradeItem.price.ToString();
                if (!flag) priceText.color = Color.grey;
                tradeeElement.GetComponent<Button>().onClick
                    .AddListener((() => __instance.OnSelectedItem(tradeeElement)));
                __instance.m_itemList.Add(tradeeElement);

                var coinImage = Utils.FindChild(tradeeElement.transform, "coin icon").GetComponent<Image>();
                coinImage.sprite = tradeItem.moneyItem.m_itemData.GetIcon();
            }

            if (index1 < 0) index1 = 0;
            __instance.SelectItem(index1, false);

            return false;
        }

        return true;
    }


    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.SelectItem)), HarmonyPostfix, HarmonyWrapSafe]
    public static void StoreGui_SelectItem(StoreGui __instance)
    {
        if (__instance.m_selectedItem == null) return;
        current = TradeItem.all.Find(x => x.prefab == __instance.m_selectedItem.m_prefab);
        if (current == null)
        {
            __instance.m_coinPrefab = coinPrefab;
            moneyIcon.sprite = coinPrefab.m_itemData.GetIcon();
            return;
        }

        __instance.m_coinPrefab = current.moneyItem;
        moneyIcon.sprite = current.moneyItem.m_itemData.GetIcon();
    }

    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.SellItem)), HarmonyPrefix, HarmonyWrapSafe]
    public static void StoreGui_SellItem_Prefi(StoreGui __instance)
    {
        __instance.m_coinPrefab = coinPrefab;
    }

    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.SellItem)), HarmonyPostfix, HarmonyWrapSafe]
    public static void StoreGui_SellItem_Postfix(StoreGui __instance)
    {
        StoreGui_SelectItem(__instance);
    }

    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.UpdateSellButton)), HarmonyPrefix, HarmonyWrapSafe]
    public static void StoreGui_UpdateSellButton(StoreGui __instance)
    {
        __instance.m_coinPrefab = coinPrefab;
    }

    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.OnSellItem)), HarmonyPrefix, HarmonyWrapSafe]
    public static void StoreGui_OnSellItem(StoreGui __instance)
    {
        StoreGui_SelectItem(__instance);
    }

    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.UpdateBuyButton)), HarmonyPrefix, HarmonyWrapSafe]
    public static void StoreGui_UpdateBuyButton(StoreGui __instance)
    {
        StoreGui_SelectItem(__instance);
    }

    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.GetPlayerCoins)), HarmonyPrefix, HarmonyWrapSafe]
    public static void StoreGui_GetPlayerCoins(StoreGui __instance)
    {
        StoreGui_SelectItem(__instance);
    }

    [HarmonyPatch(typeof(Trader), nameof(Trader.Say), typeof(List<string>), typeof(string)), HarmonyPrefix,
     HarmonyWrapSafe]
    public static bool Trader_Say(StoreGui __instance, List<string> texts, string trigger)
    {
        if (texts == null || texts.Count == 0) return false;
        return true;
    }

    [HarmonyPatch(typeof(Trader), nameof(Trader.Interact)), HarmonyPrefix, HarmonyWrapSafe]
    public static void Trader_Say(Trader __instance, Humanoid character, bool hold)
    {
        if (!__instance.GetComponent<NPC_Brain>()) return;
        __instance.transform.rotation = Quaternion.LookRotation(character.transform.position);
    }
}