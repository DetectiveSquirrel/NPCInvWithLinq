using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using SharpDX;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using ExileCore.Shared.Helpers;

namespace NPCInvWithLinq;

public class NPCInvWithLinqSettings : ISettings
{
    public ToggleNode Enable { get; set; } = new ToggleNode(false);
    public ToggleNode DrawOnTabLabels { get; set; } = new ToggleNode(true);
    public ColorNode TabFrameColor { get; set; } = new ColorNode(Color.Red);
    public RangeNode<int> FrameThickness { get; set; } = new RangeNode<int>(1, 1, 20);

    [JsonIgnore]
    public TextNode FilterTest { get; set; } = new TextNode();

    [Menu("Use a Custom \"\\config\\custom_folder\" folder ")]
    public TextNode CustomConfigDir { get; set; } = new TextNode();

    public List<NPCInvRule> NPCInvRules { get; set; } = new List<NPCInvRule>();
}

public class NPCInvRule
{
    public string Name { get; set; } = "";
    public string Location { get; set; } = "";
    public bool Enabled { get; set; } = false;
    public ColorNode Color { get; set; } = new ColorNode(System.Drawing.Color.Red.ToSharpDx());

    public NPCInvRule(string name, string location, bool enabled)
    {
        Name = name;
        Location = location;
        Enabled = enabled;
    }
}