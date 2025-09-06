#nullable enable
using Godot;

namespace PlantProject.UI
{
    [GlobalClass]
    public partial class LevelCompleteOverlay : Control
    {
        [Signal] public delegate void RestartRequestedEventHandler();
        [Signal] public delegate void QuitRequestedEventHandler();
        [Signal] public delegate void RewardSelectedEventHandler(int index);

        [Export] public Vector2 PanelSize { get; set; } = new Vector2(420, 420);
        [Export] public Color PanelColor { get; set; } = new Color(0f, 0f, 0f, 1f);

        public override void _Ready()
        {
            ProcessMode = ProcessModeEnum.WhenPaused;
            MouseFilter = MouseFilterEnum.Stop;
            Visible = false;

            // Fill viewport
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

        private VBoxContainer? _choiceBox;

        private void BuildUi()
        {
            var panel = new ColorRect { Color = PanelColor, CustomMinimumSize = PanelSize };
            var half = PanelSize * 0.5f;
            panel.AnchorLeft = 0.5f;
            panel.AnchorTop = 0.5f;
            panel.AnchorRight = 0.5f;
            panel.AnchorBottom = 0.5f;
            panel.OffsetLeft = -half.X;
            panel.OffsetRight = half.X;
            panel.OffsetTop = -half.Y;
            panel.OffsetBottom = half.Y;
            AddChild(panel);

            var vbox = new VBoxContainer
            {
                AnchorLeft = 0f,
                AnchorTop = 0f,
                AnchorRight = 1f,
                AnchorBottom = 1f,
                OffsetLeft = 16f,
                OffsetRight = -16f,
                OffsetTop = 16f,
                OffsetBottom = -16f
            };
            panel.AddChild(vbox);

            var title = new Label
            {
                Text = "LEVEL COMPLETE",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            title.AddThemeFontSizeOverride("font_size", 36);
            vbox.AddChild(title);

            vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 16) });

            // Choice box placeholder
            _choiceBox = new VBoxContainer();
            vbox.AddChild(_choiceBox);

            vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 8) });

            var btnRestart = new Button { Text = "Next Level" };
            btnRestart.Pressed += () => EmitSignal(SignalName.RestartRequested);
            vbox.AddChild(btnRestart);

            var btnQuit = new Button { Text = "End Game" };
            btnQuit.Pressed += () => EmitSignal(SignalName.QuitRequested);
            vbox.AddChild(btnQuit);
        }

        public void ShowOverlay() => Visible = true;
        public void HideOverlay() => Visible = false;

        public void ShowWithRewards(PlantProject.Core.RewardChoice[] choices)
        {
            ShowOverlay();
            if (_choiceBox == null) return;
            for (int i = _choiceBox.GetChildCount() - 1; i >= 0; i--)
            {
                _choiceBox.GetChild(i).QueueFree();
            }
            for (int i = 0; i < choices.Length; i++)
            {
                var c = choices[i];
                var btn = new Button { Text = $"{c.Title}", TooltipText = c.Description };
                int idx = i;
                btn.Pressed += () => EmitSignal(SignalName.RewardSelected, idx);
                _choiceBox.AddChild(btn);
            }
        }
    }
}
