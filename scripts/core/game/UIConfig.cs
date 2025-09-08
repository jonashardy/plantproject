#nullable enable
using Godot;

namespace PlantProject.Core
{
    [GlobalClass]
    public partial class UIConfig : Resource
    {
        [Export] public float CountdownSeconds { get; set; } = 3.0f;
        [Export] public float FightFadeSeconds { get; set; } = 0.6f;
    }
}
