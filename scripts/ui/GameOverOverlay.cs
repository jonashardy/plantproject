#nullable enable
using Godot;

namespace PlantProject.UI
{
    [GlobalClass]
    public partial class GameOverOverlay : Control
    {
        [Signal] public delegate void RestartRequestedEventHandler();
        [Signal] public delegate void QuitRequestedEventHandler();

        [Export] public Vector2 PanelSize { get; set; } = new Vector2(420, 420);
        [Export] public Color PanelColor { get; set; } = new Color(0f, 0f, 0f, 1f);

        public override void _Ready()
        {
            ProcessMode = ProcessModeEnum.WhenPaused;
            MouseFilter = MouseFilterEnum.Stop; // block world input
            Visible = false;

            // Ensure this control fills the viewport
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
            // Centered panel using explicit center anchors
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
                Text = "GAME OVER",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            title.AddThemeFontSizeOverride("font_size", 36);
            vbox.AddChild(title);

            vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 16) });

            var btnRestart = new Button { Text = "Restart Level" };
            btnRestart.Pressed += () => EmitSignal(SignalName.RestartRequested);
            vbox.AddChild(btnRestart);

            var btnQuit = new Button { Text = "End Game" };
            btnQuit.Pressed += () => EmitSignal(SignalName.QuitRequested);
            vbox.AddChild(btnQuit);
        }

        public void ShowOverlay() => Visible = true;
        public void HideOverlay() => Visible = false;
    }
}
