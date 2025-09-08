#nullable enable
using Godot;

namespace PlantProject.UI
{
    [GlobalClass]
    public partial class CountdownOverlay : Control
    {
        [Signal] public delegate void CountdownCompletedEventHandler();

        [Export] public float StartSeconds { get; set; } = 3.0f;
        [Export] public int FontSize { get; set; } = 96;
        [Export] public Color TextColor { get; set; } = new Color(1f, 1f, 1f, 1f);

        private Label? _label;
        private float _remaining;
        private bool _fighting;
        private bool _running;

        public override void _Ready()
        {
            ProcessMode = ProcessModeEnum.Always; // continue while tree is paused
            MouseFilter = MouseFilterEnum.Ignore;
            Visible = false;

            AnchorLeft = 0f;
            AnchorTop = 0f;
            AnchorRight = 1f;
            AnchorBottom = 1f;
            OffsetLeft = 0f;
            OffsetTop = 0f;
            OffsetRight = 0f;
            OffsetBottom = 0f;

            BuildUi();
        }

        private void BuildUi()
        {
            var lbl = new Label
            {
                Name = "CountdownLabel",
                Text = "3.00",
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            lbl.AnchorLeft = 0.5f;
            lbl.AnchorRight = 0.5f;
            lbl.AnchorTop = 0.5f;
            lbl.AnchorBottom = 0.5f;
            lbl.OffsetLeft = -400f;  // width padding for centering large text
            lbl.OffsetRight = 400f;
            lbl.OffsetTop = -80f;
            lbl.OffsetBottom = 80f;
            lbl.AddThemeColorOverride("font_color", TextColor);
            lbl.AddThemeFontSizeOverride("font_size", FontSize);
            lbl.AddThemeColorOverride("font_outline_color", new Color(0f, 0f, 0f, 1f));
            lbl.AddThemeConstantOverride("outline_size", 2);
            AddChild(lbl);
            _label = lbl;
        }

        public void StartCountdown(float? seconds = null)
        {
            float s = seconds ?? StartSeconds;
            _remaining = Mathf.Max(0f, s);
            _fighting = false;
            _running = true;
            if (_label != null)
            {
                _label.Modulate = new Color(1f, 1f, 1f, 1f);
                _label.Text = _remaining.ToString("0.00");
            }
            Visible = true;
            GetTree().Paused = true;
        }

        public override void _Process(double delta)
        {
            if (!_running || _label == null) return;

            if (!_fighting)
            {
                _remaining -= (float)delta;
                if (_remaining > 0f)
                {
                    _label.Text = Mathf.Max(0f, _remaining).ToString("0.00");
                }
                else
                {
                    // Switch to FIGHT! and fade away
                    _fighting = true;
                    _label.Text = "FIGHT!";
                    var tw = CreateTween();
                    tw.SetPauseMode(Tween.TweenPauseMode.Process);
                    tw.TweenProperty(_label, "modulate:a", 0.0f, PlantProject.Core.UI.FightFadeSeconds);
                    tw.Finished += OnFadeFinished;
                }
            }
        }

        private void OnFadeFinished()
        {
            _running = false;
            Visible = false;
            // Unpause only if no other overlays are showing a blocking state
            var tree = GetTree();
            tree.Paused = false;
            EmitSignal(SignalName.CountdownCompleted);
        }
    }
}
