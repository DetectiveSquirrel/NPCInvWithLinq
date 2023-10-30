using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;
using System.Text.Json.Serialization;

namespace NPCInvWithLinq
{
    public class NPCInvWithLinqSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(false);

        [JsonIgnore]
        public ButtonNode ReloadFilters { get; set; } = new ButtonNode();
        public ListNode FilterFile { get; set; } = new ListNode();
        public ColorNode FrameColor { get; set; } = new ColorNode(Color.Red);
        public RangeNode<int> FrameThickness { get; set; } = new RangeNode<int>(1, 1, 20);

        [JsonIgnore]
        public TextNode FilterTest { get; set; } = new TextNode();
    }
}