#nullable enable
using Godot;
using PlantProject.Core;

namespace PlantProject.UI
{
    [GlobalClass]
    public partial class HealthBar2D : Node2D
    {
        [Export] public Vector2 Size { get; set; } = new Vector2(48, 6);
        [Export] public Vector2 Offset { get; set; } = new Vector2(0, -24);
        [Export] public Color BackColor { get; set; } = new Color(0f, 0f, 0f, 0.6f);
        [Export] public Color FillColor { get; set; } = new Color(0.2f, 0.9f, 0.2f, 0.95f);
        [Export] public Color BorderColor { get; set; } = new Color(1f, 1f, 1f, 0.9f);
        [Export] public float BorderWidth { get; set; } = 1f;
        [Export] public bool ShowWhenFull { get; set; } = true;
        [Export] public bool ForceTopLevel { get; set; } = true;

        private GodotUnit2D? _unit;

        public override void _Ready()
        {
            _unit = GetParent() as GodotUnit2D;
            if (_unit != null)
            {
                _unit.Damaged += OnUnitChanged;
                _unit.Healed += OnUnitChanged;
                _unit.Died += OnUnitDied;
            }

            if (ForceTopLevel)
            {
                TopLevel = true; // ignore parent's transform/z ordering
                ZAsRelative = false;
                ZIndex = 10000; // keep above world-space elements
            }

            QueueRedraw();
        }

        public override void _ExitTree()
        {
            if (_unit != null)
            {
                _unit.Damaged -= OnUnitChanged;
                _unit.Healed -= OnUnitChanged;
                _unit.Died -= OnUnitDied;
            }
        }

        public override void _Process(double delta)
        {
            if (_unit != null)
            {
                bool full = _unit.CurrentHp >= _unit.MaxHp;
                Visible = ShowWhenFull || !full;
                if (ForceTopLevel)
                {
                    GlobalPosition = _unit.GlobalPosition; // follow unit in world space
                }
            }
        }

        public override void _Draw()
        {
            if (_unit == null) return;

            int max = _unit.MaxHp;
            int cur = Mathf.Clamp(_unit.CurrentHp, 0, max);
            if (max <= 0) return;

            float t = max > 0 ? (float)cur / (float)max : 0f;

            var size = Size;
            var origin = Offset - new Vector2(size.X * 0.5f, size.Y); // center above

            DrawRect(new Rect2(origin, size), BackColor);

            var fillSize = new Vector2(Mathf.Max(0f, size.X * t), size.Y);
            DrawRect(new Rect2(origin, fillSize), FillColor);

            if (BorderWidth > 0f)
            {
                var a = origin;
                var b = origin + new Vector2(size.X, 0);
                var c = origin + size;
                var d = origin + new Vector2(0, size.Y);
                DrawLine(a, b, BorderColor, BorderWidth);
                DrawLine(b, c, BorderColor, BorderWidth);
                DrawLine(c, d, BorderColor, BorderWidth);
                DrawLine(d, a, BorderColor, BorderWidth);
            }
        }

        private void OnUnitChanged(IUnit unit, int amount, IUnit? source)
        {
            QueueRedraw();
        }

        private void OnUnitDied(IUnit unit, IUnit? source)
        {
            QueueRedraw();
        }
    }
}
