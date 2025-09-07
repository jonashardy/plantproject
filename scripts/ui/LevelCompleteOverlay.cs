#nullable enable
using Godot;
using PlantProject.Core;

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
                var opt = CreateRewardOption(c, i);
                _choiceBox.AddChild(opt);
            }
        }

        private Control CreateRewardOption(RewardChoice c, int idx)
        {
            // Panel that looks button-like and fully controls its own layout/wrapping
            var panel = new PanelContainer
            {
                TooltipText = c.Description,
                SizeFlagsHorizontal = SizeFlags.ExpandFill,
                MouseFilter = MouseFilterEnum.Stop // capture clicks
            };

            // Default styling and hover highlight
            var sbNormal = new StyleBoxFlat { BgColor = new Color(0f, 0f, 0f, 0.6f) };
            sbNormal.CornerRadiusTopLeft = 6;
            sbNormal.CornerRadiusTopRight = 6;
            sbNormal.CornerRadiusBottomLeft = 6;
            sbNormal.CornerRadiusBottomRight = 6;
            var sbHover = new StyleBoxFlat { BgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f) };
            sbHover.CornerRadiusTopLeft = 6;
            sbHover.CornerRadiusTopRight = 6;
            sbHover.CornerRadiusBottomLeft = 6;
            sbHover.CornerRadiusBottomRight = 6;
            panel.AddThemeStyleboxOverride("panel", sbNormal);
            panel.MouseEntered += () => panel.AddThemeStyleboxOverride("panel", sbHover);
            panel.MouseExited += () => panel.AddThemeStyleboxOverride("panel", sbNormal);

            // Click handler
            panel.GuiInput += (InputEvent ev) =>
            {
                if (ev is InputEventMouseButton mb && mb.Pressed && mb.ButtonIndex == MouseButton.Left)
                {
                    EmitSignal(SignalName.RewardSelected, idx);
                }
            };

            var margin = new MarginContainer();
            margin.AddThemeConstantOverride("margin_left", 12);
            margin.AddThemeConstantOverride("margin_right", 12);
            margin.AddThemeConstantOverride("margin_top", 8);
            margin.AddThemeConstantOverride("margin_bottom", 8);
            panel.AddChild(margin);

            var rich = new RichTextLabel
            {
                BbcodeEnabled = true,
                FitContent = true,
                AutowrapMode = TextServer.AutowrapMode.Word,
                ScrollActive = false,
                MouseFilter = MouseFilterEnum.Ignore,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            // Title + bolded bonus text in parentheses
            rich.Text = $"{c.Title} ([b]{c.Description}[/b])";
            margin.AddChild(rich);

            return panel;
        }
    }
}
