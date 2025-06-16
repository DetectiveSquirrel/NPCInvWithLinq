using System.Collections.Generic;
using System.Linq;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Helpers;
using Ground_Items_With_Linq;
using ItemFilterLibrary;
using SharpDX;
using static NPCInvWithLinq.ServerAndStashWindow;
using Vector2 = System.Numerics.Vector2;

namespace NPCInvWithLinq;

public class ServerAndStashWindow
{
    public IList<WindowSet> Tabs { get; set; }

    public class WindowSet
    {
        public int Index { get; set; }
        public string Title { get; set; }
        public bool IsVisible { get; set; }
        public Element TabNameElement { get; set; }
        public List<CustomItemData> ServerItems { get; set; }
        public List<CustomItemData> TradeWindowItems { get; set; }
        public ServerInventory Inventory { get; set; }

        public override string ToString()
        {
            return
                $"Tab({Title}) is Index({Index}) IsVisible({IsVisible}) [ServerItems({ServerItems.Count}), TradeWindowItems({TradeWindowItems.Count})]";
        }
    }
}

public class NPCInvWithLinq : BaseSettingsPlugin<NPCInvWithLinqSettings>
{
    public static NPCInvWithLinq Main;
    private readonly CachedValue<List<CustomItemData>> _rewardItems;
    private readonly CachedValue<List<CustomItemData>> _ritualItems;
    private readonly CachedValue<List<WindowSet>> _storedStashAndWindows;
    private PurchaseWindow _purchaseWindow;
    private PurchaseWindow _purchaseWindowHideout;
    public List<FilterEntry> ItemFilters;
    public sealed record FilterEntry(ItemFilter<CustomItemData> Filter, Color Color);

    public NPCInvWithLinq()
    {
        Name = "NPC Inv With Linq";
        _storedStashAndWindows =
            new TimeCache<List<WindowSet>>(CacheUtils.RememberLastValue<List<WindowSet>>(UpdateCurrentTradeWindow), 50);
        _rewardItems = new TimeCache<List<CustomItemData>>(GetRewardItems, 1000);
        _ritualItems = new TimeCache<List<CustomItemData>>(GetRitualItems, 1000);
    }

    public override bool Initialise()
    {
        Main = this;
        RulesDisplay.LoadAndApplyRules();
        return true;
    }

    public override void DrawSettings()
    {
        base.DrawSettings();
        RulesDisplay.DrawSettings();
    }

    public override Job Tick()
    {
        _purchaseWindowHideout = GameController.Game.IngameState.IngameUi.PurchaseWindowHideout;
        _purchaseWindow = GameController.Game.IngameState.IngameUi.PurchaseWindow;

        return null;
    }

    public override void Render()
    {
        var hoveredItem = GetHoveredItem();
        PerformItemFilterTest(hoveredItem);
        ProcessPurchaseWindow(hoveredItem);
        ProcessRewardsWindow(hoveredItem);
        ProcessRitualWindow(hoveredItem);
    }

    private void ProcessRewardsWindow(Element hoveredItem)
    {
        if (!GameController.IngameState.IngameUi.QuestRewardWindow.IsVisible) return;

        var items = _rewardItems?.Value ?? [];
        ProcessItems(hoveredItem, items);
    }

    private void ProcessRitualWindow(Element hoveredItem)
    {
        if (!GameController.IngameState.IngameUi.RitualWindow.IsVisible) return;

        ProcessItems(hoveredItem, _ritualItems?.Value ?? []);
    }

    private void ProcessItems(Element hoveredItem, List<CustomItemData> items)
    {
        foreach (var reward in items)
        {
            ProcessItem(hoveredItem, reward);
        }
    }

    private void ProcessItem(Element hoveredItem, CustomItemData reward)
    {
        if (GetItemFilter(reward) is { } filter)
        {
            var frameColor = filter.Color;
            if (hoveredItem != null &&
                hoveredItem.Tooltip.GetClientRectCache.Intersects(reward.ClientRectangle) && 
                hoveredItem.Entity.Address != reward.Entity.Address)
            {
                frameColor = frameColor.ToImguiVec4(45).ToSharpColor();
            }

            Graphics.DrawFrame(reward.ClientRectangle, frameColor, Settings.FrameThickness);
        }
    }

    private List<CustomItemData> GetRewardItems()
    {
        return GameController.IngameState.IngameUi.QuestRewardWindow.GetPossibleRewards()
            .Where(item => item.Item2 is { Address: not 0, IsValid: true })
            .Select(item =>
                new CustomItemData(item.Item1, GameController, EKind.QuestReward, item.Item2.GetClientRectCache))
            .ToList();
    }

    private List<CustomItemData> GetRitualItems()
    {
        return GameController.IngameState.IngameUi.RitualWindow.InventoryElement.VisibleInventoryItems
            .Where(item => item.Item is { Address: not 0, IsValid: true })
            .Select(item => new CustomItemData(item.Item, GameController, EKind.RitualReward, item.GetClientRectCache))
            .ToList();
    }

    private void ProcessPurchaseWindow(Element hoveredItem)
    {
        if (!IsPurchaseWindowVisible())
            return;

        List<string> unSeenItems = [];
        ProcessStoredTabs(unSeenItems, hoveredItem);

        var purchaseWindowItems = GetVisiblePurchaseWindow();
        var serverItemsBox = CalculateServerItemsBox(unSeenItems, purchaseWindowItems);

        DrawServerItems(serverItemsBox, unSeenItems, hoveredItem);
    }

    private void DrawServerItems(RectangleF serverItemsBox, List<string> unSeenItems, Element hoveredItem)
    {
        if (hoveredItem == null || !hoveredItem.Tooltip.GetClientRectCache.Intersects(serverItemsBox))
        {
            var boxColor = new Color(0, 0, 0, 150);
            var textColor = new Color(255, 255, 255, 230);

            Graphics.DrawBox(serverItemsBox, boxColor);

            for (var i = 0; i < unSeenItems.Count; i++)
            {
                var stringItem = unSeenItems[i];
                var textHeight = Graphics.MeasureText(stringItem);
                var textPadding = 10;

                Graphics.DrawText(stringItem,
                    new Vector2(serverItemsBox.X + textPadding, serverItemsBox.Y + textHeight.Y * i), textColor);
            }
        }
    }

    private Element GetHoveredItem()
    {
        return GameController.IngameState.UIHover is { Address: not 0, Entity.IsValid: true } hover ? hover : null;
    }

    private bool IsPurchaseWindowVisible()
    {
        return _purchaseWindowHideout.IsVisible || _purchaseWindow.IsVisible;
    }

    private void ProcessStoredTabs(List<string> unSeenItems, Element hoveredItem)
    {
        foreach (var storedTab in _storedStashAndWindows.Value)
            if (storedTab.IsVisible)
                ProcessItems(hoveredItem, storedTab.TradeWindowItems);
            else
                ProcessHiddenTabItems(storedTab, unSeenItems, hoveredItem);
    }

    private void ProcessHiddenTabItems(WindowSet storedTab, List<string> unSeenItems, Element hoveredItem)
    {
        var tabHadWantedItem = false;
        foreach (var hiddenItem in storedTab.ServerItems)
        {
            if (hiddenItem == null) continue;
            if (ItemInFilter(hiddenItem))
            {
                ProcessUnseenItems(unSeenItems, storedTab, hoveredItem);
                unSeenItems.Add($"\t{hiddenItem.Name}");
                tabHadWantedItem = true;
            }
        }

        if (tabHadWantedItem)
            unSeenItems.Add("");
    }

    private void ProcessUnseenItems(List<string> unSeenItems, WindowSet storedTab, Element hoveredItem)
    {
        if (!unSeenItems.Contains($"Tab [{storedTab.Title}]"))
        {
            unSeenItems.Add($"Tab [{storedTab.Title}]");
            if (Settings.DrawOnTabLabels) DrawTabNameElementFrame(storedTab.TabNameElement, hoveredItem);
        }
    }

    private void DrawTabNameElementFrame(Element tabNameElement, Element hoveredItem)
    {
        if (hoveredItem == null ||
            !hoveredItem.Tooltip.GetClientRectCache.Intersects(tabNameElement.GetClientRectCache))
            Graphics.DrawFrame(tabNameElement.GetClientRectCache, Settings.TabFrameColor, Settings.FrameThickness);
        else
            Graphics.DrawFrame(tabNameElement.GetClientRectCache, Settings.TabFrameColor.Value with { A = 45 },
                Settings.FrameThickness);
    }

    private RectangleF CalculateServerItemsBox(List<string> unSeenItems, PurchaseWindow purchaseWindowItems)
    {
        var startingPoint = purchaseWindowItems.TabContainer.GetClientRectCache.TopRight.ToVector2Num();
        startingPoint.X += 15;

        var longestText = unSeenItems.MaxBy(s => s.Length);
        var textHeight = Graphics.MeasureText(longestText);
        var textPadding = 10;

        return new RectangleF
        {
            Height = textHeight.Y * unSeenItems.Count,
            Width = textHeight.X + textPadding * 2,
            X = startingPoint.X,
            Y = startingPoint.Y
        };
    }

    private PurchaseWindow GetVisiblePurchaseWindow()
    {
        return _purchaseWindowHideout.IsVisible ? _purchaseWindowHideout :
            _purchaseWindow.IsVisible ? _purchaseWindow : null;
    }

    private void PerformItemFilterTest(Element hoveredItem)
    {
        if (Settings.FilterTest.Value is { Length: > 0 } && hoveredItem != null)
        {
            var filter = ItemFilter.LoadFromString(Settings.FilterTest);
            var matched = filter.Matches(new ItemData(hoveredItem.Entity, GameController));
            DebugWindow.LogMsg($"Debug item match on hover: {matched}");
        }
    }

    private List<WindowSet> UpdateCurrentTradeWindow(List<WindowSet> previousValue)
    {
        var previousDict = previousValue?.ToDictionary(x =>
            (x.Inventory.Address, x.Inventory.ServerRequestCounter, x.IsVisible, x.TradeWindowItems.Count));
        var purchaseWindowItems = (_purchaseWindowHideout, _purchaseWindow) switch
        {
            ({ IsVisible: true } w, _) => w,
            (_, { IsVisible: true } w) => w,
            _ => null
        };

        if (purchaseWindowItems == null)
            return [];

        return purchaseWindowItems.TabContainer.Inventories.Select((inventory, i) =>
        {
            var uiInventory = inventory?.Inventory;
            if (uiInventory == null) return null;
            var serverInventory = uiInventory.ServerInventory;
            if (serverInventory == null)
                DebugWindow.LogError(
                    $"Server inventory for ui inventory {uiInventory} ({uiInventory.InvType}) is missing");

            var isVisible = uiInventory.IsVisible;
            var visibleValidUiItems = uiInventory.VisibleInventoryItems
                .Where(x => x.Item?.Path != null).ToList();
            if (previousDict?.TryGetValue(
                    (serverInventory?.Address ?? 0, serverInventory?.ServerRequestCounter ?? -1, isVisible,
                        visibleValidUiItems.Count), out var previousSet) == true) return previousSet;

            var title = $"-{i + 1}-";
            var newTab = new WindowSet
            {
                Inventory = serverInventory,
                Index = i,
                ServerItems = serverInventory?.Items.Where(x => x?.Path != null)
                    .Select(x => new CustomItemData(x, GameController, EKind.Shop)).ToList() ?? [],
                TradeWindowItems = visibleValidUiItems
                    .Select(x => new CustomItemData(x.Item, GameController, EKind.Shop, x.GetClientRectCache))
                    .ToList(),
                Title = title,
                IsVisible = isVisible,
                TabNameElement = inventory.TabButton
            };

            return newTab;
        }).Where(x => x != null).ToList();
    }

    private bool ItemInFilter(CustomItemData item) => GetItemFilter(item) != null;

    private FilterEntry GetItemFilter(CustomItemData item) => ItemFilters?.FirstOrDefault(filter => filter.Filter.Matches(item));
}