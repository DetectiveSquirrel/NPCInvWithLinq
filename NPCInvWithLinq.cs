using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.Elements.InventoryElements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using ItemFilterLibrary;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using static NPCInvWithLinq.ServerAndStashWindow;

namespace NPCInvWithLinq
{
    public class ServerAndStashWindow
    {
        public IList<WindowSet> Tabs { get; set; }
        public class WindowSet
        {
            public int Index { get; set; }
            public string Title { get; set; }
            public bool IsVisible { get; set; }
            public List<CustomItemData> ServerItems { get; set; }
            public List<CustomItemData> TradeWindowItems { get; set; }
            public override string ToString()
            {
                return $"Tab({Title}) is Index({Index}) IsVisible({IsVisible}) [ServerItems({ServerItems.Count}), TradeWindowItems({TradeWindowItems.Count})]";
            }
        }
    }

    public class NPCInvWithLinq : BaseSettingsPlugin<NPCInvWithLinqSettings>
    {
        private readonly TimeCache<List<WindowSet>> _storedStashAndWindows;
        private ItemFilter _itemFilter;
        private PurchaseWindow _purchaseWindowHideout;
        private PurchaseWindow _purchaseWindow;
        private IList<InventoryHolder> _npcInventories;

        public NPCInvWithLinq()
        {
            Name = "NPC Inv With Linq";
            _storedStashAndWindows = new TimeCache<List<WindowSet>>(UpdateCurrentTradeWindow, 50);
        }
        public override bool Initialise()
        {
            Settings.FilterFile.OnValueSelected = _ => LoadRuleFiles();
            Settings.ReloadFilters.OnPressed = LoadRuleFiles;
            LoadRuleFiles();
            return true;
        }

        public override Job Tick()
        {
            _purchaseWindowHideout = GameController.Game.IngameState.IngameUi.PurchaseWindowHideout;
            _purchaseWindow = GameController.Game.IngameState.IngameUi.PurchaseWindow;
            _npcInventories = GameController.Game.IngameState.ServerData.NPCInventories;

            return null;
        }

        public override void Render()
        {
            Element _hoveredItem = null;

            if (GameController.IngameState.UIHover is { Address: not 0 } h && h.Entity.IsValid)
                _hoveredItem = GameController.IngameState.UIHover;

            if (!_purchaseWindowHideout.IsVisible && !_purchaseWindow.IsVisible)
                return;

            // Draw open inventory and then for non visible inventories add items to a list that are in teh filter and draw on the side in an imgui window
            List<string> unSeenItems = new List<string>();

            foreach (var storedTab in _storedStashAndWindows.Value)
            {
                if (storedTab.IsVisible)
                {
                    //Hand is visible part here (add to a list of items to draw?)
                    foreach (var visibleItem in storedTab.TradeWindowItems)
                    {
                        if (visibleItem == null) continue;
                        if (!ItemInFilter(visibleItem)) continue;

                        if (_hoveredItem != null && _hoveredItem.Tooltip.GetClientRectCache.Intersects(visibleItem.ClientRectangleCache) && _hoveredItem.Entity.Address != visibleItem.Entity.Address)
                        {
                            var dimmedColor = Settings.FrameColor.Value; dimmedColor.A = 45;
                            Graphics.DrawFrame(visibleItem.ClientRectangleCache, dimmedColor, Settings.FrameThickness);
                        }
                        else
                        {
                            Graphics.DrawFrame(visibleItem.ClientRectangleCache, Settings.FrameColor, Settings.FrameThickness);
                        }
                    }
                }
                else
                {
                    // not visible part here (add to a list of items to draw?)
                    foreach (var hiddenItem in storedTab.ServerItems)
                    {
                        if (hiddenItem == null) continue;
                        if (!ItemInFilter(hiddenItem)) continue;
                        unSeenItems.Add($"{storedTab.Title} ({hiddenItem.Name})");
                    }
                }
            }

            if (Settings.FilterTest.Value is { Length: > 0 } && _hoveredItem != null)
            {
                var f = ItemFilter.LoadFromString(Settings.FilterTest);
                var matched = f.Matches(new ItemData(_hoveredItem.Entity, GameController.Files));
                DebugWindow.LogMsg($"Debug item match on hover: {matched}");
            }
        }

        private void LoadRuleFiles()
        {
            var pickitConfigFileDirectory = Path.Combine(ConfigDirectory);

            if (!Directory.Exists(pickitConfigFileDirectory))
            {
                Directory.CreateDirectory(pickitConfigFileDirectory);
                return;
            }

            var dirInfo = new DirectoryInfo(pickitConfigFileDirectory);
            Settings.FilterFile.Values = dirInfo.GetFiles("*.ifl").Select(x => Path.GetFileNameWithoutExtension(x.Name)).ToList();
            if (Settings.FilterFile.Values.Any() && !Settings.FilterFile.Values.Contains(Settings.FilterFile.Value))
            {
                Settings.FilterFile.Value = Settings.FilterFile.Values.First();
            }

            if (!string.IsNullOrWhiteSpace(Settings.FilterFile.Value))
            {
                var filterFilePath = Path.Combine(pickitConfigFileDirectory, $"{Settings.FilterFile.Value}.ifl");
                if (File.Exists(filterFilePath))
                {
                    _itemFilter = ItemFilter.LoadFromPath(filterFilePath);
                }
                else
                {
                    _itemFilter = null;
                    LogError("Item filter file not found, plugin will not work");
                }
            }
        }

        private List<WindowSet> UpdateCurrentTradeWindow()
        {
            var newTabSet = new List<WindowSet>();

            if (_purchaseWindowHideout == null || _purchaseWindow == null)
                return newTabSet;

            PurchaseWindow purchaseWindowItems = null;
            WorldArea currentWorldArea = GameController.Game.IngameState.Data.CurrentWorldArea;

            if (currentWorldArea.IsHideout && _purchaseWindowHideout.IsVisible)
                purchaseWindowItems = _purchaseWindowHideout;
            else if (currentWorldArea.IsTown && _purchaseWindow.IsVisible)
                purchaseWindowItems = _purchaseWindow;

            if (purchaseWindowItems == null)
                return newTabSet;

            for (int i = 0; i < _npcInventories.Count; i++)
            {
                var newTab = new WindowSet
                {
                    Index = i,

                    ServerItems = _npcInventories[i].Inventory.Items
                        .ToList()
                        .Where(x => x?.Path != null)
                        .Select(x => new CustomItemData(x, GameController.Files))
                        .ToList(),

                    TradeWindowItems = purchaseWindowItems.TabContainer.AllInventories[i].VisibleInventoryItems
                        .ToList()
                        .Where(x => x.Item?.Path != null)
                        .Select(x => new CustomItemData(x.Item, GameController.Files, x.GetClientRectCache))
                        .ToList(),

                    Title = $"-{i+1}-",

                    IsVisible = purchaseWindowItems.TabContainer.AllInventories[i].IsVisible
                };
                newTabSet.Add(newTab);
            }

            return newTabSet;
        }

        private bool ItemInFilter(ItemData item)
        {
            return _itemFilter?.Matches(item, false) ?? false;
        }
    }
}