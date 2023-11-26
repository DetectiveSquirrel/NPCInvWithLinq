using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ItemFilterLibrary;

public class CustomItemData : ItemData
{
    public CustomItemData(Entity queriedItem, GameController gc) : base(queriedItem, gc)
    {
    }

    public CustomItemData(Entity queriedItem, GameController gc, SharpDX.RectangleF GetClientRectCache) : base(queriedItem, gc)
    {
        ClientRectangleCache = GetClientRectCache;
    }

    public SharpDX.RectangleF ClientRectangleCache { get; set; }
}