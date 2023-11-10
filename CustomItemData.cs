using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.MemoryObjects;
using ItemFilterLibrary;

public class CustomItemData : ItemData
{
    public CustomItemData(Entity queriedItem, FilesContainer fs, AreaController area) : base(queriedItem, fs)
    {
        AreaInfo = new AreaData(area.CurrentArea.RealLevel, area.CurrentArea.Name, area.CurrentArea.Act, area.CurrentArea.Act > 10);
    }
    public CustomItemData(Entity queriedItem, FilesContainer fs, AreaController area, SharpDX.RectangleF GetClientRectCache) : base(queriedItem, fs)
    {
        ClientRectangleCache = GetClientRectCache;

        AreaInfo = new AreaData(area.CurrentArea.RealLevel, area.CurrentArea.Name, area.CurrentArea.Act, area.CurrentArea.Act > 10);
    }

    public SharpDX.RectangleF ClientRectangleCache { get; set; }
}