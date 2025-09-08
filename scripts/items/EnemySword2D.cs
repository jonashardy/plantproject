#nullable enable
using Godot;

namespace PlantProject.Items
{
    [GlobalClass]
    public partial class EnemySword2D : Sword2D
    {
        [Export] public Color BladeColor { get; set; } = new Color(0.3f, 0.6f, 1f, 0.95f);

        public override void _Ready()
        {
            base._Ready();

            // Disable the full-cone overlay for enemies
            ShowAttackArc = false;

            // Make enemy sword half as long and tinted blue
            var sprite = GetNodeOrNull<Sprite2D>("Sprite");
            if (sprite != null)
            {
                sprite.Scale = new Vector2(0.5f, 1f);
                sprite.Modulate = BladeColor;
            }

            // Enemy sword swings less often: double player's cooldown
            AttackCooldown *= 2f;
        }
    }
}
