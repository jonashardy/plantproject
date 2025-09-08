#nullable enable
using System.Collections.Generic;
using Godot;
using PlantProject.Core;
using PlantProject.Levels;
using PlantProject.Items;

namespace PlantProject.Enemies
{
    [GlobalClass]
    public partial class EnemyUnit2D : GodotUnit2D
    {
        [Export] public bool AddDefaultMelee { get; set; } = true;
        [Export] public bool SleepOffscreen { get; set; } = true;

        private VisibleOnScreenNotifier2D? _vis;
        private Sprite2D? _sprite;
        private Node2D? _player;
        private bool _active;
        private EnemySword2D? _sword;
        private Vector2 _lastDirToPlayer = Vector2.Right;

        public override void _Ready()
        {
            var actions = new List<IAction>();
            if (AddDefaultMelee)
            {
                actions.Add(new MeleeAttackAction("Chomp", 0.8f));
            }

            var model = new EnemyUnit(UnitName, MaxHpInit, CurrentHpInit, DefenseInit, PowerLevelInit, SpeedInit, actions);
            SetUnitModel(model);

            // Visual: add a Sprite2D using the simple purple SVG
            var tex = GD.Load<Texture2D>("res://assets/enemies/enemy.svg");
            if (tex != null)
            {
                _sprite = new Sprite2D
                {
                    Name = "Sprite",
                    Texture = tex
                };
                AddChild(_sprite);
            }

            // Visibility notifier to sleep off-screen
            _vis = new VisibleOnScreenNotifier2D { Name = "Vis" };
            AddChild(_vis);
            _vis.Connect(VisibleOnScreenNotifier2D.SignalName.ScreenEntered, new Callable(this, nameof(OnScreenEntered)));
            _vis.Connect(VisibleOnScreenNotifier2D.SignalName.ScreenExited, new Callable(this, nameof(OnScreenExited)));

            // Find player reference
            _player = FindPlayer();

            // Start inactive until we know visibility; disable processing to save CPU
            _active = !SleepOffscreen;
            if (SleepOffscreen)
            {
                SetProcess(false);
                if (_sprite != null) _sprite.Visible = false;
            }

            // Attach enemy sword
            _sword = new EnemySword2D { Name = "Sword" };
            AddChild(_sword);

            // Despawn 1s after death
            Died += OnUnitDied;

            // Apply difficulty to enemy weapon cooldown once the sword is ready
            CallDeferred(nameof(ApplyWeaponDifficulty));

            base._Ready();
        }

        private void ApplyWeaponDifficulty()
        {
            if (_sword == null) return;
            var level = GetAncestorLevel();
            int levelNum = level?.LevelNumber ?? 1;
            float mul = Core.Difficulty.GetEnemyCooldownMultiplier(levelNum);
            _sword.AttackCooldown *= mul;
        }

        public override void _Process(double delta)
        {
            if (!_active || _player == null || !IsAlive) return;

            var toPlayer = (_player.GlobalPosition - GlobalPosition);
            float dist = toPlayer.Length();

            // Determine facing direction even if overlapping
            Vector2 dirNorm;
            if (dist > 0.0001f)
            {
                dirNorm = toPlayer / dist;
                _lastDirToPlayer = dirNorm;
            }
            else
            {
                dirNorm = _lastDirToPlayer == Vector2.Zero ? Vector2.Right : _lastDirToPlayer;
            }

            float myRadius = GetApproxRadius(this);
            float playerRadius = GetApproxRadius(_player);
            float stopDist = myRadius + playerRadius; // touch edges

            float needed = dist - stopDist;
            if (needed > 0f)
            {
                float maxStep = Speed * (float)delta;
                float move = Mathf.Min(maxStep, needed);
                if (move > 0.0001f)
                {
                    GlobalPosition += dirNorm * move;
                }
                else
                {
                    // Not moving effectively: try to swing
                    _sword?.RequestAttack(this, dirNorm);
                }
            }
            else
            {
                // Within or overlapping touch distance: swing immediately
                _sword?.RequestAttack(this, dirNorm);
            }

            // Keep inside world bounds if available
            if (GetAncestorLevel() is SimpleLevel level)
            {
                var size = level.WorldSize;
                var half = GetSpriteHalfSize();
                var minX = half.X;
                var minY = half.Y;
                var maxX = Mathf.Max(half.X, size.X - half.X);
                var maxY = Mathf.Max(half.Y, size.Y - half.Y);
                GlobalPosition = new Vector2(
                    Mathf.Clamp(GlobalPosition.X, minX, maxX),
                    Mathf.Clamp(GlobalPosition.Y, minY, maxY)
                );
            }
        }

        private void OnScreenEntered()
        {
            _active = true;
            if (_sprite != null) _sprite.Visible = true;
            SetProcess(true);
        }

        private void OnScreenExited()
        {
            if (!SleepOffscreen) return;
            _active = false;
            if (_sprite != null) _sprite.Visible = false; // drop draw cost
            SetProcess(false); // drop CPU cost
        }

        private async void OnUnitDied(IUnit unit, IUnit? source)
        {
            // Ensure we stop processing and hide visuals; then free after 1s
            _active = false;
            SetProcess(false);
            if (_sprite != null) _sprite.Visible = false;
            // If killed by player, grant temporary power bonus
            if (source is GodotUnit2D killer && killer.IsInGroup("player"))
            {
                killer.AddDamagePowerMod(0.1f);
                // Add special charge on kill
                if (killer is PlantProject.Player.PlayerUnit2D p)
                {
                    p.AddSpecialCharge(p.SpecialKillBonus);
                }
            }
            var timer = GetTree().CreateTimer(1.0);
            await ToSignal(timer, SceneTreeTimer.SignalName.Timeout);
            QueueFree();
        }

        private Node2D? FindPlayer()
        {
            // Prefer group lookup
            var players = GetTree().GetNodesInGroup("player");
            foreach (var n in players)
            {
                if (n is Node2D nn) return nn;
            }
            // Fallback to level's child named Player
            if (GetAncestorLevel() is SimpleLevel level)
            {
                return level.GetNodeOrNull<Node2D>("Player");
            }
            return null;
        }

        private SimpleLevel? GetAncestorLevel()
        {
            Node? n = GetParent();
            while (n != null)
            {
                if (n is SimpleLevel sl) return sl;
                n = n.GetParent();
            }
            return null;
        }

        private Vector2 GetSpriteHalfSize()
        {
            var s = _sprite ?? GetNodeOrNull<Sprite2D>("Sprite");
            if (s?.Texture != null)
            {
                var texSize = s.Texture.GetSize();
                var scale = s.Scale;
                return new Vector2(texSize.X * scale.X, texSize.Y * scale.Y) * 0.5f;
            }
            return Vector2.Zero;
        }

        private static float GetApproxRadius(Node2D node)
        {
            var s = node.GetNodeOrNull<Sprite2D>("Sprite");
            if (s?.Texture != null)
            {
                var texSize = s.Texture.GetSize();
                var scale = s.Scale;
                return Mathf.Max(texSize.X * scale.X, texSize.Y * scale.Y) * 0.5f;
            }
            return 8f;
        }
    }
}
