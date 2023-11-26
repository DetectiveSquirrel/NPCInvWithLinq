using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ItemFilterLibrary;

public class CustomItemData : ItemData
{
    public CustomItemData(Entity queriedItem, GameController gc, EKind kind, SharpDX.RectangleF clientRect = default) : base(queriedItem, gc)
    {
        Kind = kind;
        ClientRectangle = clientRect;
    }

    public SharpDX.RectangleF ClientRectangle { get; set; }
    public EKind Kind { get; }
}

public enum EKind
{
    QuestReward,
    Shop,
}