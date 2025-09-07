#nullable enable
using Godot;
using PlantProject.Core;
using PlantProject.UI;
using PlantProject.Audio;

namespace PlantProject.Levels
{
    public partial class SimpleLevel : Node2D
    {
        [Export]
        public int LevelNumber { get; set; } = 1;
        [Export]
        public Vector2 WorldSize { get; set; } = new Vector2(2048, 2048);

        private Sprite2D? _background;
        private Line2D? _border;
        private Camera2D? _camera;

        public override void _Ready()
        {
            // Adopt level number from global progress
            LevelNumber = System.Math.Max(1, GameProgress.CurrentLevelNumber);
            // Add a tiling background using the moss-green SVG.
            var tex = GD.Load<Texture2D>("res://assets/tiles/moss_green_tile.svg");
            if (tex != null)
            {
                _background = new Sprite2D
                {
                    Name = "Background",
                    Texture = tex,
                    Centered = false,
                    RegionEnabled = true,
                    TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled,
                    ZIndex = -1000
                };

                AddChild(_background);
                UpdateBackgroundRegion();
            }

            // Add visible world borders
            _border = new Line2D
            {
                Name = "WorldBorder",
                Closed = true,
                Width = 4f,
                DefaultColor = new Color(1f, 1f, 1f, 0.9f),
                ZIndex = 50
            };
            AddChild(_border);
            UpdateBorderGeometry();

            var player = GetNodeOrNull<Node2D>("Player");
            if (player != null)
            {
                // Center the player within the world bounds
                player.Position = WorldSize / 2f;

                // Create a camera owned by the level (explicit follow via _Process)
                _camera = new Camera2D
                {
                    Name = "Camera2D",
                };
                AddChild(_camera);
                _camera.MakeCurrent();
            }

            // Update background and camera limits when the viewport size changes
            GetViewport().Connect(Viewport.SignalName.SizeChanged, new Callable(this, nameof(OnViewportSizeChanged)));

            // Add enemy spawner
            var spawner = new Enemies.EnemySpawner2D { Name = "EnemySpawner" };
            AddChild(spawner);

            // Add HUD overlay (CanvasLayer) so it doesn't interfere with world space
            var hud = new Hud { Name = "HUD", Layer = 1 };
            AddChild(hud);

            // Ensure a single persistent ambient music player exists under the scene tree root
            var root = GetTree().Root;
            var amb = root.GetNodeOrNull<AmbientMusic>("AmbientMusic");
            if (amb == null)
            {
                amb = new AmbientMusic { Name = "AmbientMusic" };
                // Add to root so it persists across scene reloads (no abrupt restarts)
                root.AddChild(amb);
            }
            // On starting a fresh run from level entry, switch to the gameplay track (only if not already set)
            if (GameProgress.CurrentLevelNumber == 1 && GameProgress.LivesRemaining == GameProgress.LivesInitial)
            {
                var cur = amb.CurrentTrackName ?? string.Empty;
                if (!cur.ToLowerInvariant().Contains("neon shadows"))
                {
                    amb.CallDeferred("PlayTrackByPath", "res://assets/audio/music/Neon Shadows.mp3");
                }
            }
        }

        public override void _Process(double delta)
        {
            // Explicit camera follow: center on player, then clamp to world rectangle using viewport half extents
            if (_camera == null)
                return;

            var player = GetNodeOrNull<Node2D>("Player");
            if (player == null)
                return;

            var vp = GetViewportRect().Size;
            var half = vp / 2f;

            var minX = half.X;
            var minY = half.Y;
            var maxX = Mathf.Max(half.X, WorldSize.X - half.X);
            var maxY = Mathf.Max(half.Y, WorldSize.Y - half.Y);

            var target = player.Position;
            _camera.Position = new Vector2(
                Mathf.Clamp(target.X, minX, maxX),
                Mathf.Clamp(target.Y, minY, maxY)
            );
        }

        private void UpdateBackgroundRegion()
        {
            if (_background == null)
                return;

            _background.RegionRect = new Rect2(Vector2.Zero, WorldSize);
        }

        private void UpdateBorderGeometry()
        {
            if (_border == null)
                return;
            _border.Points = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(WorldSize.X, 0),
                new Vector2(WorldSize.X, WorldSize.Y),
                new Vector2(0, WorldSize.Y)
            };
        }

        // Camera limits not used with explicit follow; clamping handled in _Process
        private void UpdateCameraLimits() { }

        private void OnViewportSizeChanged()
        {
            UpdateBackgroundRegion();
            // No need to update camera limits; explicit follow recalculates each frame
        }
    }
}
