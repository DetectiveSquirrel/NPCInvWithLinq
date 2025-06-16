using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ItemFilterLibrary;
using SharpDX;

public class CustomItemData : ItemData
{
    public CustomItemData(Entity queriedItem, GameController gc, EKind kind, RectangleF clientRect = default) : base(
        queriedItem, gc)
    {
        Kind = kind;
        ClientRectangle = clientRect;
    }

    public RectangleF ClientRectangle { get; set; }
    public EKind Kind { get; }
}

public enum EKind
{
    QuestReward,
    Shop,
    RitualReward,
    Mercenary
}