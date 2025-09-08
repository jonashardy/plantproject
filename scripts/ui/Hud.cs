#nullable enable
using Godot;
using PlantProject.Core;
using PlantProject.Enemies;
using PlantProject.Levels;
using PlantProject.Audio;

namespace PlantProject.UI
{
    [GlobalClass]
    public partial class Hud : CanvasLayer
    {
        [Export] public int BarHeight { get; set; } = 96;
        [Export] public Color BackgroundColor { get; set; } = new Color(0f, 0f, 0f, 0.9f);

        private ColorRect? _topBar;
        private Label? _hpLabel;
        private Label? _livesLabel;
        private GodotUnit2D? _player;
        private EnemySpawner2D? _spawner;
        private Label? _spawnLabel;
        private Label? _powerLabel;
        private Label? _levelLabel;
        private SpecialMeter? _special;
        private GameOverOverlay? _gameOver;
        private DeathOverlay? _death;
        private LevelCompleteOverlay? _levelComplete;
        private bool _levelCompleteShown;
        private PlantProject.Core.RewardChoice[]? _pendingRewards;
        private PauseOverlay? _pause;
        private bool _pauseShown;
        private CountdownOverlay? _countdown;
        private bool _awaitingNextLevelAfterCountdown;

        public override void _Ready()
        {
            EnsureUiActions();
            // Handle input both paused and unpaused
            ProcessMode = Node.ProcessModeEnum.Always;
            BuildUi();
            _player = FindPlayer();
            if (_player != null)
            {
                _player.Damaged += OnPlayerChanged;
                _player.Healed += OnPlayerChanged;
                _player.Died += OnPlayerDied;
                UpdateHpText();
            }
            UpdateLivesText();

            _spawner = FindSpawner();
            if (_spawner != null)
            {
                _spawner.Connect(EnemySpawner2D.SignalName.EnemiesSpawnedChanged, new Callable(this, nameof(OnEnemiesSpawnedChanged)));
                _spawner.Connect(EnemySpawner2D.SignalName.EnemiesAliveChanged, new Callable(this, nameof(OnEnemiesAliveChanged)));
                UpdateSpawnText();
                UpdatePowerText();
            }

            UpdateLevelText();

            // Defer countdown start to ensure all units (player + enemies) have spawned
            CallDeferred(nameof(StartFreshRunCountdown));
            CallDeferred(nameof(StartRespawnCountdownIfNeeded));
        }

        public override void _Process(double delta)
        {
            // Keep special meter updated
            if (_special != null)
            {
                if (_player is PlantProject.Player.PlayerUnit2D p)
                {
                    _special.SetValue(p.SpecialCharge);
                }
                else
                {
                    _special.SetValue(0f);
                }
            }
        }

        private async void StartFreshRunCountdown()
        {
            // Only on fresh run
            if (!(GameProgress.CurrentLevelNumber == 1 && GameProgress.LivesRemaining == GameProgress.LivesInitial))
                return;

            // Wait a tiny moment to ensure spawner _Ready() completed and enemies are in the tree
            var timer = GetTree().CreateTimer(0.01);
            await ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
            _countdown?.StartCountdown(PlantProject.Core.UI.CountdownSeconds);
        }

        private async void StartRespawnCountdownIfNeeded()
        {
            if (!GameProgress.PendingRespawnCountdown) return;
            // Clear the flag immediately to avoid duplicate starts
            GameProgress.PendingRespawnCountdown = false;
            // Wait a tiny moment to ensure scene finished reloading and nodes are ready
            var timer = GetTree().CreateTimer(0.01);
            await ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
            _countdown?.StartCountdown(PlantProject.Core.UI.CountdownSeconds);
        }

        private static void EnsureUiActions()
        {
            // Ensure ESC is bound to ui_cancel for pausing
            const string action = "ui_cancel";
            if (!InputMap.HasAction(action))
            {
                InputMap.AddAction(action);
                var ev = new InputEventKey
                {
                    PhysicalKeycode = Key.Escape,
                    Keycode = Key.Escape
                };
                InputMap.ActionAddEvent(action, ev);
            }
        }

        public override void _ExitTree()
        {
            if (_player != null)
            {
                _player.Damaged -= OnPlayerChanged;
                _player.Healed -= OnPlayerChanged;
                _player.Died -= OnPlayerDied;
            }
        }

        private void BuildUi()
        {
            // Top black bar spanning screen width, fixed height
            var bar = new ColorRect
            {
                Name = "TopBar",
                Color = BackgroundColor
            };
            bar.AnchorLeft = 0f;
            bar.AnchorRight = 1f;
            bar.AnchorTop = 0f;
            bar.AnchorBottom = 0f;
            bar.OffsetLeft = 0f;
            bar.OffsetTop = 0f;
            bar.OffsetRight = 0f;
            bar.OffsetBottom = BarHeight;
            AddChild(bar);
            _topBar = bar;

            // Layout metrics for two rows
            const float pad = 8f;
            const float gap = 8f;
            const float line = 36f;
            float row1Y = pad;
            float row2Y = pad + line + gap;

            // HP label (row 1, left)
            var hp = new Label
            {
                Name = "HpLabel",
                Text = "HP: 0/0"
            };
            hp.AnchorLeft = 0f;
            hp.AnchorTop = 0f;
            hp.OffsetLeft = 12f;
            hp.OffsetTop = row1Y;
            hp.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 1f));
            hp.AddThemeFontSizeOverride("font_size", 36);
            bar.AddChild(hp);
            _hpLabel = hp;

            // Lives label (row 1, center)
            var lives = new Label
            {
                Name = "LivesLabel",
                Text = "Lives: 3",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            lives.AnchorLeft = 0f;
            lives.AnchorRight = 1f;
            lives.AnchorTop = 0f;
            lives.OffsetLeft = 0f;
            lives.OffsetRight = 0f;
            lives.OffsetTop = row1Y;
            lives.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 1f));
            lives.AddThemeFontSizeOverride("font_size", 36);
            bar.AddChild(lives);
            _livesLabel = lives;

            // Level label (row 1, right)
            var lvl = new Label
            {
                Name = "LevelLabel",
                Text = "Level: 1"
            };
            lvl.AnchorRight = 1f;
            lvl.AnchorTop = 0f;
            lvl.OffsetRight = -12f;
            lvl.OffsetTop = row1Y;
            lvl.HorizontalAlignment = HorizontalAlignment.Right;
            lvl.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 1f));
            lvl.AddThemeFontSizeOverride("font_size", 36);
            bar.AddChild(lvl);
            _levelLabel = lvl;

            // Special meter (centered, under Lives)
            var meter = new SpecialMeter
            {
                Name = "SpecialMeter",
                // Slightly longer and taller; match row-2 font height (36)
                CustomMinimumSize = new Vector2(360, 36)
            };
            // Center horizontally, place on row2 (under the lives counter)
            meter.AnchorLeft = 0.5f;
            meter.AnchorRight = 0.5f;
            meter.AnchorTop = 0f;
            meter.AnchorBottom = 0f;
            meter.OffsetLeft = -180f; // half width
            meter.OffsetRight = 180f;
            meter.OffsetTop = row2Y; // align with other row-2 elements and add spacing from Lives
            meter.OffsetBottom = meter.OffsetTop + 36f;
            bar.AddChild(meter);
            _special = meter;

            // Enemies count label (row 2, right)
            var sc = new Label
            {
                Name = "SpawnLabel",
                Text = "Enemies: 0/0"
            };
            sc.AnchorRight = 1f;
            sc.AnchorTop = 0f;
            sc.OffsetRight = -12f;
            sc.OffsetTop = row2Y;
            sc.HorizontalAlignment = HorizontalAlignment.Right;
            sc.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 1f));
            sc.AddThemeFontSizeOverride("font_size", 36);
            bar.AddChild(sc);
            _spawnLabel = sc;

            // Power level (row 2, left)
            var pw = new Label
            {
                Name = "PowerLabel",
                Text = "Power: 0"
            };
            pw.AnchorLeft = 0f;
            pw.AnchorTop = 0f;
            pw.OffsetLeft = 12f;
            pw.OffsetTop = row2Y;
            pw.AddThemeColorOverride("font_color", new Color(1f, 1f, 1f, 1f));
            pw.AddThemeFontSizeOverride("font_size", 36);
            bar.AddChild(pw);
            _powerLabel = pw;

            // Create modular Game Over overlay object and hook signals
            _gameOver = new GameOverOverlay { Name = "GameOver" };
            AddChild(_gameOver);
            _gameOver.Connect(GameOverOverlay.SignalName.RestartRequested, new Callable(this, nameof(OnRestartPressed)));
            _gameOver.Connect(GameOverOverlay.SignalName.QuitRequested, new Callable(this, nameof(OnQuitPressed)));

            // Death overlay (you have fallen)
            _death = new DeathOverlay { Name = "Death" };
            AddChild(_death);
            _death.Connect(DeathOverlay.SignalName.TryAgainRequested, new Callable(this, nameof(OnTryAgainAfterDeath)));
            _death.Connect(DeathOverlay.SignalName.MainMenuRequested, new Callable(this, nameof(OnMainMenuPressed)));
            _death.Connect(DeathOverlay.SignalName.QuitRequested, new Callable(this, nameof(OnQuitPressed)));

            // Level complete overlay (same framework)
            _levelComplete = new LevelCompleteOverlay { Name = "LevelComplete" };
            AddChild(_levelComplete);
            _levelComplete.Connect(LevelCompleteOverlay.SignalName.QuitRequested, new Callable(this, nameof(OnQuitPressed)));
            _levelComplete.Connect(LevelCompleteOverlay.SignalName.RewardSelected, new Callable(this, nameof(OnRewardSelected)));

            // Pause overlay
            _pause = new PauseOverlay { Name = "Pause" };
            AddChild(_pause);
            _pause.Connect(PauseOverlay.SignalName.ResumeRequested, new Callable(this, nameof(OnResumePressed)));
            _pause.Connect(PauseOverlay.SignalName.RestartRequested, new Callable(this, nameof(OnRestartPressed)));
            _pause.Connect(PauseOverlay.SignalName.QuitRequested, new Callable(this, nameof(OnQuitPressed)));

            // Countdown overlay for new game start
            _countdown = new CountdownOverlay { Name = "Countdown" };
            AddChild(_countdown);
            _countdown.Connect(CountdownOverlay.SignalName.CountdownCompleted, new Callable(this, nameof(OnCountdownCompleted)));
        }

        private GodotUnit2D? FindPlayer()
        {
            foreach (var n in GetTree().GetNodesInGroup("player"))
            {
                if (n is GodotUnit2D u) return u;
            }
            return null;
        }

        private EnemySpawner2D? FindSpawner()
        {
            // Look for a sibling named EnemySpawner under the current scene root
            var root = GetTree().CurrentScene;
            return root?.GetNodeOrNull<EnemySpawner2D>("EnemySpawner");
        }

        private SimpleLevel? FindLevel()
        {
            var root = GetTree().CurrentScene;
            return root as SimpleLevel;
        }

        private void OnPlayerChanged(IUnit unit, int amount, IUnit? source)
        {
            UpdateHpText();
        }

        private void OnPlayerDied(IUnit unit, IUnit? source)
        {
            UpdateHpText();
            // Consume a life; if any remain, auto-restart level, otherwise game over
            bool hasMore = GameProgress.UseLife();
            UpdateLivesText();
            if (hasMore)
            {
                ShowDeath();
            }
            else
            {
                ShowGameOver();
            }
        }

        private void UpdateHpText()
        {
            if (_hpLabel == null) return;
            if (_player == null)
            {
                _hpLabel.Text = "HP: --/--";
                return;
            }
            _hpLabel.Text = $"HP: {_player.CurrentHp}/{_player.MaxHp}";
        }

        private void OnEnemiesSpawnedChanged(int total)
        {
            UpdateSpawnText();
            UpdatePowerText();
        }

        private void OnEnemiesAliveChanged(int alive)
        {
            UpdateSpawnText();
            UpdatePowerText();
            // Show level complete when all enemies are cleared (after at least one spawned)
            if (!_levelCompleteShown && (_spawner?.TotalSpawned ?? 0) > 0 && alive <= 0)
            {
                // Generate rewards and show overlay with them
                _pendingRewards = PlantProject.Core.RewardSystem.GetRandomChoices(3);
                ShowLevelComplete(_pendingRewards);
            }
        }

        private void UpdateSpawnText()
        {
            if (_spawnLabel == null) return;
            int alive = _spawner?.TotalAlive ?? 0;
            int total = _spawner?.TotalSpawned ?? 0;
            _spawnLabel.Text = $"Enemies: {alive}/{total}";
        }

        private void UpdateLivesText()
        {
            if (_livesLabel == null) return;
            _livesLabel.Text = $"Lives: {GameProgress.LivesRemaining}";
        }

        private void UpdatePowerText()
        {
            if (_powerLabel == null) return;
            if (_player == null)
            {
                _powerLabel.Text = "Power: --";
                return;
            }
            int basePower = _player.PowerLevel;
            float mod = _player.DamagePowerMod;
            if (mod > 0.0001f)
            {
                float effective = basePower * (1f + mod);
                _powerLabel.Text = $"Power: {basePower} (x{(1f + mod):0.0} = {effective:0.0})";
            }
            else
            {
                _powerLabel.Text = $"Power: {basePower}";
            }
        }

        private void UpdateLevelText()
        {
            if (_levelLabel == null) return;
            var level = FindLevel();
            if (level != null)
            {
                _levelLabel.Text = $"Level: {level.LevelNumber}";
            }
            else
            {
                _levelLabel.Text = "Level: --";
            }
        }

        private void ShowGameOver()
        {
            if (_gameOver == null) return;
            _gameOver.ShowOverlay();
            GetTree().Paused = true;
        }

        private void ShowDeath()
        {
            if (_death == null) return;
            _death.ShowOverlay();
            GetTree().Paused = true;
        }

        private void ShowLevelComplete(PlantProject.Core.RewardChoice[]? rewards = null)
        {
            if (_levelComplete == null) return;
            if (rewards != null) _levelComplete.ShowWithRewards(rewards);
            else _levelComplete.ShowOverlay();
            GetTree().Paused = true;
            _levelCompleteShown = true;
        }

        private void OnRewardSelected(int index)
        {
            if (_pendingRewards == null || index < 0 || index >= _pendingRewards.Length) return;
            PlantProject.Core.RewardSystem.ApplyChoice(_pendingRewards[index]);
            // Hide level complete overlay and start a short countdown before next level
            if (_levelComplete != null)
            {
                _levelComplete.HideOverlay();
            }
            _awaitingNextLevelAfterCountdown = true;
            _countdown?.StartCountdown(PlantProject.Core.UI.CountdownSeconds);
        }

        private void OnCountdownCompleted()
        {
            if (_awaitingNextLevelAfterCountdown)
            {
                _awaitingNextLevelAfterCountdown = false;
                OnNextLevelPressed();
            }
        }

        private void OnRestartPressed()
        {
            var tree = GetTree();
            tree.Paused = false;
            // If restarting from Game Over overlay, treat as returning to Start Menu
            if (_gameOver != null && _gameOver.Visible)
            {
                GameProgress.Reset();
                var amb = tree.Root.GetNodeOrNull<AmbientMusic>("AmbientMusic");
                amb?.PlayTrackByPath("res://assets/audio/music/Pixel Dreamscape.mp3");
                tree.ChangeSceneToFile("res://scenes/ui/start_menu.tscn");
                return;
            }
            // Otherwise, restart level only (Pause overlay path)
            if (tree.ReloadCurrentScene() != Error.Ok)
            {
                var path = tree.CurrentScene?.SceneFilePath;
                if (!string.IsNullOrEmpty(path)) tree.ChangeSceneToFile(path!);
            }
        }

        private void OnQuitPressed()
        {
            // Switch back to default menu track when leaving the game
            var amb = GetTree().Root.GetNodeOrNull<AmbientMusic>("AmbientMusic");
            amb?.PlayTrackByPath("res://assets/audio/music/Pixel Dreamscape.mp3");
            GetTree().Quit();
        }

        private void OnMainMenuPressed()
        {
            var tree = GetTree();
            tree.Paused = false;
            GameProgress.Reset();
            var amb = tree.Root.GetNodeOrNull<AmbientMusic>("AmbientMusic");
            amb?.PlayTrackByPath("res://assets/audio/music/Pixel Dreamscape.mp3");
            tree.ChangeSceneToFile("res://scenes/ui/start_menu.tscn");
        }

        private void OnTryAgainAfterDeath()
        {
            // Hide the death overlay, mark that a respawn countdown should run after reload,
            // then reload the current scene immediately.
            if (_death != null) _death.HideOverlay();
            GameProgress.PendingRespawnCountdown = true;
            OnRestartPressed();
        }

        private void OnNextLevelPressed()
        {
            var tree = GetTree();
            tree.Paused = false;
            PlantProject.Core.GameProgress.NextLevel();
            if (tree.ReloadCurrentScene() != Error.Ok)
            {
                var path = tree.CurrentScene?.SceneFilePath;
                if (!string.IsNullOrEmpty(path)) tree.ChangeSceneToFile(path!);
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (@event.IsActionPressed("ui_cancel"))
            {
                TogglePause();
            }
        }

        private void TogglePause()
        {
            // Don't allow pausing over blocking overlays
            if (_countdown != null && _countdown.Visible) return;
            if (_gameOver != null && _gameOver.Visible) return;
            if (_death != null && _death.Visible) return;
            if (_levelComplete != null && _levelComplete.Visible) return;

            if (!_pauseShown)
            {
                ShowPause();
            }
            else
            {
                OnResumePressed();
            }
        }

        private void ShowPause()
        {
            if (_pause == null) return;
            _pause.ShowOverlay();
            _pauseShown = true;
            GetTree().Paused = true;
        }

        private void OnResumePressed()
        {
            if (_pause == null) return;
            _pause.HideOverlay();
            _pauseShown = false;
            // Unpause only if no other overlays are holding pause
            if ((_gameOver == null || !_gameOver.Visible) && (_death == null || !_death.Visible) && (_levelComplete == null || !_levelComplete.Visible))
            {
                GetTree().Paused = false;
            }
        }
    }
}
