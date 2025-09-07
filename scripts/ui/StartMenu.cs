#nullable enable
using Godot;
using PlantProject.Audio;
using PlantProject.Core;

namespace PlantProject.UI
{
    [GlobalClass]
    public partial class StartMenu : Control
    {
        private AboutOverlay? _about;

        [Export] public Vector2 PanelSize { get; set; } = new Vector2(540, 420);
        [Export] public Color PanelColor { get; set; } = new Color(0f, 0f, 0f, 1f);

        public override void _Ready()
        {
            ProcessMode = ProcessModeEnum.Always;
            MouseFilter = MouseFilterEnum.Stop;

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
            // Double-check music is correct on menu load too
            PlantProject.Audio.MusicHelper.EnsureMenuTrack(GetTree());
        }


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
                Text = "Planet of the Grapes",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            title.AddThemeFontSizeOverride("font_size", 36);
            vbox.AddChild(title);

            vbox.AddChild(new Control { CustomMinimumSize = new Vector2(0, 24) });

            var btnStart = new Button { Text = "Start Game" };
            btnStart.Pressed += OnStartPressed;
            vbox.AddChild(btnStart);

            var btnAbout = new Button { Text = "About" };
            btnAbout.Pressed += OnAboutPressed;
            vbox.AddChild(btnAbout);

            var btnQuit = new Button { Text = "End Game" };
            btnQuit.Pressed += OnQuitPressed;
            vbox.AddChild(btnQuit);

            // About overlay modal
            _about = new AboutOverlay { Name = "About" };
            AddChild(_about);
            _about.Connect(AboutOverlay.SignalName.CloseRequested, new Callable(this, nameof(OnAboutClose)));
        }

        private void OnStartPressed()
        {
            var tree = GetTree();
            tree.Paused = false;
            // Switch to the gameplay track before entering the level
            var amb = tree.Root.GetNodeOrNull<AmbientMusic>("AmbientMusic");
            amb?.PlayTrackByPath("res://assets/audio/music/Neon Shadows.mp3");
            GameProgress.Reset();
            tree.ChangeSceneToFile("res://scenes/levels/simple_level.tscn");
        }

        private void OnAboutPressed()
        {
            _about?.ShowOverlay();
        }

        private void OnAboutClose()
        {
            _about?.HideOverlay();
        }

        private void OnQuitPressed()
        {
            GetTree().Quit();
        }
    }
}
