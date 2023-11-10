using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Helpers;
using ImGuiNET;
using ItemFilterLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
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
            public Element TabNameElement { get; set; }
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
        private List<ItemFilter> _itemFilters;
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
            var _hoveredItem = GameController.IngameState.UIHover?.Address != 0 && GameController.IngameState.UIHover.Entity.IsValid
                ? GameController.IngameState.UIHover
                : null;

            if (!_purchaseWindowHideout.IsVisible && !_purchaseWindow.IsVisible)
                return;

            List<string> unSeenItems = new List<string>(); // Initialize the list here

            foreach (var storedTab in _storedStashAndWindows.Value)
            {
                if (storedTab.IsVisible)
                {
                    foreach (var visibleItem in storedTab.TradeWindowItems)
                    {
                        if (visibleItem == null) continue;
                        if (ItemInFilter(visibleItem))
                        {
                            if (_hoveredItem != null && _hoveredItem.Tooltip.GetClientRectCache.Intersects(visibleItem.ClientRectangleCache) && _hoveredItem.Entity.Address != visibleItem.Entity.Address)
                                Graphics.DrawFrame(visibleItem.ClientRectangleCache, Settings.FrameColor.Value with { A = 45 }, Settings.FrameThickness);
                            else
                                Graphics.DrawFrame(visibleItem.ClientRectangleCache, Settings.FrameColor, Settings.FrameThickness);
                        }
                    }
                }
                else
                {
                    var tabHadWantedItem = false;
                    foreach (var hiddenItem in storedTab.ServerItems)
                    {
                        if (hiddenItem == null) continue;
                        if (ItemInFilter(hiddenItem))
                        {
                            if (!unSeenItems.Contains($"Tab [{storedTab.Title}]"))
                            {
                                unSeenItems.Add($"Tab [{storedTab.Title}]");
                                if (Settings.DrawOnTabLabels)
                                    if (_hoveredItem == null || !_hoveredItem.Tooltip.GetClientRectCache.Intersects(storedTab.TabNameElement.GetClientRectCache))
                                        Graphics.DrawFrame(storedTab.TabNameElement.GetClientRectCache, Settings.FrameColor, Settings.FrameThickness);
                                    else
                                        Graphics.DrawFrame(storedTab.TabNameElement.GetClientRectCache, Settings.FrameColor.Value with { A = 45 }, Settings.FrameThickness);
                            }
                            unSeenItems.Add($"\t{hiddenItem.Name}");
                            tabHadWantedItem = true;
                        }
                    }
                    if (tabHadWantedItem)
                        unSeenItems.Add("");
                }
            }

            PurchaseWindow purchaseWindowItems = _purchaseWindowHideout.IsVisible ? _purchaseWindowHideout : _purchaseWindow;

            var startingPoint = purchaseWindowItems.TabContainer.GetClientRectCache.TopRight.ToVector2Num();
            startingPoint.X += 15;

            var longestText = unSeenItems.OrderByDescending(s => s.Length).FirstOrDefault();
            var textHeight = Graphics.MeasureText(longestText);
            var textPadding = 10;

            var serverItemsBox = new SharpDX.RectangleF
            {
                Height = textHeight.Y * unSeenItems.Count,
                Width = textHeight.X + (textPadding * 2),
                X = startingPoint.X,
                Y = startingPoint.Y
            };

            var boxColor = new SharpDX.Color(0, 0, 0, 150);
            var textColor = new SharpDX.Color(255, 255, 255, 230);

            if (_hoveredItem == null || !_hoveredItem.Tooltip.GetClientRectCache.Intersects(serverItemsBox))
            {
                Graphics.DrawBox(serverItemsBox, boxColor);

                for (int i = 0; i < unSeenItems.Count; i++)
                {
                    string stringItem = unSeenItems[i];
                    Graphics.DrawText(stringItem, new Vector2(startingPoint.X + textPadding, startingPoint.Y + (textHeight.Y * i)), textColor);
                }
            }

            if (Settings.FilterTest.Value is { Length: > 0 } && _hoveredItem != null)
            {
                var f = ItemFilter.LoadFromString(Settings.FilterTest);
                var matched = f.Matches(new ItemData(_hoveredItem.Entity, GameController));
                DebugWindow.LogMsg($"Debug item match on hover: {matched}");
            }
        }

        public record FilterDirItem(string Name, string Path);

        public override void DrawSettings()
        {
            base.DrawSettings();

            if (ImGui.Button("Open Build Folder"))
                Process.Start("explorer.exe", ConfigDirectory);

            ImGui.Separator();
            ImGui.BulletText("Select Rules To Load");
            ImGui.BulletText("Ordering rule sets so general items will match first rather than last will improve performance");

            var tempNPCInvRules = new List<NPCInvRule>(Settings.NPCInvRules); // Create a copy

            for (int i = 0; i < tempNPCInvRules.Count; i++)
            {
                if (ImGui.ArrowButton($"##upButton{i}", ImGuiDir.Up) && i > 0)
                    (tempNPCInvRules[i - 1], tempNPCInvRules[i]) = (tempNPCInvRules[i], tempNPCInvRules[i - 1]);

                ImGui.SameLine(); ImGui.Text(" "); ImGui.SameLine();

                if (ImGui.ArrowButton($"##downButton{i}", ImGuiDir.Down) && i < tempNPCInvRules.Count - 1)
                    (tempNPCInvRules[i + 1], tempNPCInvRules[i]) = (tempNPCInvRules[i], tempNPCInvRules[i + 1]);

                ImGui.SameLine(); ImGui.Text(" - "); ImGui.SameLine();

                var refToggle = tempNPCInvRules[i].Enabled;
                if (ImGui.Checkbox($"{tempNPCInvRules[i].Name}##checkbox{i}", ref refToggle))
                    tempNPCInvRules[i].Enabled = refToggle;
            }

            Settings.NPCInvRules = tempNPCInvRules;
        }

        private void LoadRuleFiles()
        {
            var pickitConfigFileDirectory = ConfigDirectory;

            if (!Directory.Exists(pickitConfigFileDirectory))
            {
                Directory.CreateDirectory(pickitConfigFileDirectory);
                return;
            }

            var tempPickitRules = new List<NPCInvRule>(Settings.NPCInvRules);
            var toRemove = new List<NPCInvRule>();

            var itemList = new DirectoryInfo(pickitConfigFileDirectory)
                .GetFiles("*.ifl")
                .Select(drItem =>
                {
                    var existingRule = tempPickitRules.FirstOrDefault(rule => rule.Location == drItem.FullName);
                    if (existingRule == null)
                        Settings.NPCInvRules.Add(new NPCInvRule(drItem.Name, drItem.FullName, false));
                    return new FilterDirItem(drItem.Name, drItem.FullName);
                })
                .ToList();

            try
            {
                tempPickitRules
                    .Where(rule => !File.Exists(rule.Location))
                    .ToList()
                    .ForEach(rule => { toRemove.Add(rule); LogError($"File '{rule.Name}' not found."); });

                _itemFilters = tempPickitRules
                    .Where(rule => rule.Enabled && File.Exists(rule.Location))
                    .Select(rule => ItemFilter.LoadFromPath(rule.Location))
                    .ToList();

                toRemove.ForEach(rule => Settings.NPCInvRules.Remove(rule));
            }
            catch (Exception e)
            {
                LogError($"An error occurred while loading rule files: {e.Message}");
            }
        }

        private List<WindowSet> UpdateCurrentTradeWindow()
        {
            if (_purchaseWindowHideout == null || _purchaseWindow == null)
                return new List<WindowSet>();

            PurchaseWindow purchaseWindowItems = (GameController.Game.IngameState.Data.CurrentWorldArea.IsHideout && _purchaseWindowHideout.IsVisible)
                ? _purchaseWindowHideout
                : ((GameController.Game.IngameState.Data.CurrentWorldArea.IsTown && _purchaseWindow.IsVisible) ? _purchaseWindow : null);

            if (purchaseWindowItems == null)
                return new List<WindowSet>();

            return _npcInventories.Select((inventory, i) =>
            {
                var newTab = new WindowSet
                {
                    Index = i,
                    ServerItems = inventory.Inventory.Items.Where(x => x?.Path != null).Select(x => new CustomItemData(x, GameController)).ToList(),
                    TradeWindowItems = purchaseWindowItems.TabContainer.AllInventories[i].VisibleInventoryItems
                        .Where(x => x.Item?.Path != null)
                        .Select(x => new CustomItemData(x.Item, GameController, x.GetClientRectCache))
                        .ToList(),
                    Title = $"-{i + 1}-",
                    IsVisible = purchaseWindowItems.TabContainer.AllInventories[i].IsVisible
                };

                newTab.TabNameElement = purchaseWindowItems.TabContainer.TabSwitchBar.Children
                    .FirstOrDefault(x => x?.GetChildAtIndex(0)?.GetChildAtIndex(1)?.Text == newTab.Title);

                return newTab;
            }).ToList();
        }

        private bool ItemInFilter(ItemData item)
        {
            return _itemFilters?.Any(filter => filter.Matches(item)) ?? false;
        }
    }
}