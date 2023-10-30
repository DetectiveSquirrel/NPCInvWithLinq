using ExileCore.PoEMemory;
using ExileCore.PoEMemory.MemoryObjects;
using ItemFilterLibrary;

public class CustomItemData : ItemData
{
    public CustomItemData(Entity queriedItem, FilesContainer fs, SharpDX.RectangleF GetClientRectCache) : base(queriedItem, fs)
    {
        ClientRectangleCache = GetClientRectCache;
    }

    public SharpDX.RectangleF ClientRectangleCache { get; set; }
}