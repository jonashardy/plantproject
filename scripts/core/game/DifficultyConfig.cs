#nullable enable
using Godot;

namespace PlantProject.Core
{
    [GlobalClass]
    public partial class DifficultyConfig : Resource
    {
        [Export] public float HpGrowth { get; set; } = 1.12f;              // multiplier per level (compounded)
        [Export] public float DefensePerLevel { get; set; } = 2.5f;         // percent added per level
        [Export] public int DefenseCap { get; set; } = 60;                  // max percent reduction
        [Export] public float PowerGrowth { get; set; } = 0.10f;            // +10% per level
        [Export] public float SpeedGrowth { get; set; } = 0.03f;            // +3% per level
        [Export] public float SpeedCap { get; set; } = 1.5f;                // 1.5x max
        [Export] public float CooldownDecrease { get; set; } = 0.05f;       // -5% per level
        [Export] public float CooldownMin { get; set; } = 0.6f;             // 0.6x min
        [Export] public float CooldownMax { get; set; } = 1.5f;             // 1.5x max (safety)
        [Export] public int SpawnIncrement { get; set; } = 2;               // +2 per level
    }
}

