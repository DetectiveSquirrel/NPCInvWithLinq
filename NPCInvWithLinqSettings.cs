using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NPCInvWithLinq
{
    public class NPCInvWithLinqSettings : ISettings
    {
        public ToggleNode Enable { get; set; } = new ToggleNode(false);
        public ColorNode FrameColor { get; set; } = new ColorNode(Color.Red);
        public RangeNode<int> FrameThickness { get; set; } = new RangeNode<int>(1, 1, 20);

        [JsonIgnore]
        public TextNode FilterTest { get; set; } = new TextNode();

        [JsonIgnore]
        public ButtonNode ReloadFilters { get; set; } = new ButtonNode();

        public List<NPCInvRule> NPCInvRules { get; set; } = new List<NPCInvRule>();
    }
}

public class NPCInvRule
{
    public string Name { get; set; } = "";
    public string Location { get; set; } = "";
    public bool Enabled { get; set; } = false;
    public NPCInvRule(string name, string location, bool enabled)
    {
        Name = name;
        Location = location;
        Enabled = enabled;
    }
}