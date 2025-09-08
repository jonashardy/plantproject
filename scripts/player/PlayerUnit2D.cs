#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using PlantProject.Core;
using PlantProject.Levels;
using PlantProject.Items;

namespace PlantProject.Player
{
    [GlobalClass]
    public partial class PlayerUnit2D : GodotUnit2D
    {
        [Export] public bool AddDefaultMelee { get; set; } = true;
        [Export] public float SprintMultiplier { get; set; } = 2f;
        [Export] public bool LockActionsWhenSprinting { get; set; } = true;
        [Export] public float RegenDelaySeconds { get; set; } = 2.0f;
        [Export] public float RegenPercentPerSecond { get; set; } = 0.05f; // Base fallback; actual regen scales with Power

        public bool IsSprinting { get; private set; }

        protected override bool ActionsLocked => LockActionsWhenSprinting && IsSprinting;

        private static bool _inputInitialized;
        private Vector2 _facing = Vector2.Right;
        private Sword2D? _sword;
        private Polygon2D? _directionIndicator;
        private float _sinceLastAttack;
        private float _sinceLastDamage;
        private float _regenCarry;
        private Texture2D? _texNormal;
        private Texture2D? _texSprint;
        private bool _usingSprintVisual;
        [Export] public float SpecialPassivePerSecond { get; set; } = 0.02f; // 2% per second
        [Export] public float SpecialKillBonus { get; set; } = 0.12f;       // +12% per kill
        private float _specialCharge; // 0..1
        public float SpecialCharge => _specialCharge;

        public override void _Ready()
        {
            EnsureInputActions();
            if (!IsInGroup("player")) AddToGroup("player");
            // Build a logical PlayerUnit model (apply run upgrades) and attach to the Node wrapper
            var actions = new List<IAction>();
            if (AddDefaultMelee)
            {
                actions.Add(new MeleeAttackAction("Slash", 1.0f));
            }

            int maxHp0 = MaxHpInit + PlantProject.Core.GameProgress.MaxHpFlatBonus;
            int curHp0 = Math.Min(CurrentHpInit + PlantProject.Core.GameProgress.MaxHpFlatBonus, maxHp0);
            int def0 = Math.Clamp(DefenseInit + PlantProject.Core.GameProgress.DefenseAdd, 0, 100);
            int pow0 = PowerLevelInit + PlantProject.Core.GameProgress.BonusPowerLevels;
            float spd0 = SpeedInit * MathF.Max(0.1f, PlantProject.Core.GameProgress.SpeedMultiplier);

            var model = new PlayerUnit(UnitName, maxHp0, curHp0, def0, pow0, spd0, actions);
            // Hook model into the Node wrapper
            SetUnitModel(model);

            // Visuals: load normal and sprint textures
            _texNormal = GD.Load<Texture2D>("res://assets/player/player.svg");
            _texSprint = GD.Load<Texture2D>("res://assets/player/player_sprint.svg");

            // Add the sprite with the normal texture
            var tex = _texNormal;
            if (tex != null)
            {
                var sprite = new Sprite2D
                {
                    Name = "Sprite",
                    Texture = tex
                };
                AddChild(sprite);
            }

            // Attach modular sword item
            _sword = new Sword2D { Name = "Sword" };
            AddChild(_sword);
            // Apply run upgrades to sword
            _sword.BaseDamage = Math.Max(1, (int)MathF.Round(_sword.BaseDamage * PlantProject.Core.GameProgress.SwordBaseDamageMultiplier));
            _sword.ConeHalfAngleDegrees += PlantProject.Core.GameProgress.SwordConeBonusDegrees;
            _sword.AttackCooldown = Math.Max(0.05f, _sword.AttackCooldown + PlantProject.Core.GameProgress.SwordCooldownDelta);

            // Direction indicator triangle (points to facing direction)
            _directionIndicator = new Polygon2D
            {
                Name = "DirectionIndicator",
                ZIndex = 300,
                Color = new Color(1f, 1f, 1f, 0.9f)
            };
            _directionIndicator.Polygon = new Vector2[]
            {
                new Vector2(12, 0),   // tip
                new Vector2(-6, 6),   // base lower
                new Vector2(-6, -6)   // base upper
            };
            AddChild(_directionIndicator);

            // Apply regen upgrade and listen for self events to drive regen timers
            // Keep upgrade for legacy fallback; actual regen computed per-frame from Power
            RegenPercentPerSecond = MathF.Max(0f, RegenPercentPerSecond + PlantProject.Core.GameProgress.RegenPercentBonus);
            ActionStarted += OnSelfActionStarted;
            Damaged += OnSelfDamaged;

            base._Ready();
        }

        public override void _Process(double delta)
        {
            // Read movement input from actions; supports WASD and Arrow keys when EnsureInputActions() runs
            var input = Input.GetVector("move_left", "move_right", "move_up", "move_down");
            var sprintHeld = Input.IsActionPressed("sprint");

            IsSprinting = sprintHeld && input != Vector2.Zero;
            UpdateSprintVisual();

            if (input != Vector2.Zero)
            {
                float sprintMul = IsSprinting ? Mathf.Max(1f, SprintMultiplier) : 1f;
                // Scale movement speed with Power: +10% per Power level
                float powerSpeedMul = 1f + 0.1f * Math.Max(0, PowerLevel);
                var velocity = input.Normalized() * Speed * sprintMul * powerSpeedMul * (float)delta;
                Position += velocity;
                _facing = input.Normalized();
            }
            UpdateDirectionIndicator();

            // Delegate attack to sword item
            if (!ActionsLocked && Input.IsActionJustPressed("attack"))
            {
                _sword?.RequestAttack(this, _facing);
            }

            // Clamp within the world's bounds (keep entire sprite inside)
            var level = GetAncestorLevel();
            if (level != null)
            {
                var size = level.WorldSize;
                var half = GetSpriteHalfSize();
                var minX = half.X;
                var minY = half.Y;
                var maxX = Mathf.Max(half.X, size.X - half.X);
                var maxY = Mathf.Max(half.Y, size.Y - half.Y);
                Position = new Vector2(
                    Mathf.Clamp(Position.X, minX, maxX),
                    Mathf.Clamp(Position.Y, minY, maxY)
                );
            }

            // Update regen timers
            _sinceLastAttack += (float)delta;
            _sinceLastDamage += (float)delta;

            // Passive special charge generation while alive
            if (IsAlive && SpecialPassivePerSecond > 0f)
            {
                _specialCharge = Mathf.Clamp(_specialCharge + SpecialPassivePerSecond * (float)delta, 0f, 1f);
            }

            // Start healing if no attacks and no damage for RegenDelaySeconds
            if (_sinceLastAttack >= RegenDelaySeconds && _sinceLastDamage >= RegenDelaySeconds && CurrentHp < MaxHp)
            {
                float missingFrac = 1f - (float)CurrentHp / Math.Max(1, MaxHp);
                missingFrac = Mathf.Clamp(missingFrac, 0f, 1f);
                // Base regen scales linearly with Power: 1 Power = 1% MaxHP/sec
                float regenPercentBase = 0.01f * Math.Max(0, PowerLevel) + MathF.Max(0f, PlantProject.Core.GameProgress.RegenPercentBonus);
                // Keep missing-health scaling for smoother feel
                float perSecond = MathF.Max(0f, MaxHp * regenPercentBase * (1f + missingFrac));
                float toHealF = perSecond * (float)delta + _regenCarry;
                int toHeal = (int)MathF.Floor(toHealF);
                _regenCarry = toHealF - toHeal;
                if (toHeal > 0)
                {
                    Heal(toHeal);
                }
            }
        }

        private void UpdateSprintVisual()
        {
            var sprite = GetNodeOrNull<Sprite2D>("Sprite");
            if (sprite == null) return;
            bool shouldSprintVisual = IsSprinting && _texSprint != null;
            if (shouldSprintVisual != _usingSprintVisual)
            {
                sprite.Texture = shouldSprintVisual ? _texSprint : _texNormal;
                _usingSprintVisual = shouldSprintVisual;
            }
        }

        private static void EnsureInputActions()
        {
            if (_inputInitialized)
                return;

            AddActionIfMissing("move_left", new[] { Key.A, Key.Left });
            AddActionIfMissing("move_right", new[] { Key.D, Key.Right });
            AddActionIfMissing("move_up", new[] { Key.W, Key.Up });
            AddActionIfMissing("move_down", new[] { Key.S, Key.Down });
            AddActionIfMissing("sprint", new[] { Key.Shift });
            AddActionIfMissing("attack", new[] { Key.Space });

            _inputInitialized = true;
        }

        private static void AddActionIfMissing(string actionName, Key[] keys)
        {
            if (!InputMap.HasAction(actionName))
            {
                InputMap.AddAction(actionName);
                foreach (var key in keys)
                {
                    var ev = new InputEventKey
                    {
                        // Set both for broader compatibility across layouts
                        PhysicalKeycode = key,
                        Keycode = key
                    };
                    InputMap.ActionAddEvent(actionName, ev);
                }
            }
        }

        private SimpleLevel? GetAncestorLevel()
        {
            Node? n = GetParent();
            while (n != null)
            {
                if (n is SimpleLevel sl)
                    return sl;
                n = n.GetParent();
            }
            return null;
        }

        private Vector2 GetSpriteHalfSize()
        {
            var sprite = GetNodeOrNull<Sprite2D>("Sprite");
            if (sprite?.Texture != null)
            {
                var texSize = sprite.Texture.GetSize();
                var scale = sprite.Scale;
                return new Vector2(texSize.X * scale.X, texSize.Y * scale.Y) * 0.5f;
            }
            return Vector2.Zero;
        }

        // Sword functionality moved to scripts/items/Sword2D.cs

        private void UpdateDirectionIndicator()
        {
            if (_directionIndicator == null)
                return;

            var dir = _facing == Vector2.Zero ? Vector2.Right : _facing.Normalized();
            float angle = dir.Angle();
            _directionIndicator.Rotation = angle;

            // Rotate the sprint oblong to match facing when sprinting
            var sprite = GetNodeOrNull<Sprite2D>("Sprite");
            if (sprite != null)
            {
                sprite.Rotation = _usingSprintVisual ? angle : 0f;
            }

            var half = GetSpriteHalfSize();
            float playerExtent = Mathf.Max(half.X, half.Y);
            // Offset a bit beyond the player edge so the triangle sits outside
            float triLen = 12f;
            float offset = playerExtent + triLen * 0.6f;
            _directionIndicator.Position = dir * offset;
        }

        private void OnSelfActionStarted(IUnit unit, IAction action)
        {
            _sinceLastAttack = 0f;
            _regenCarry = 0f; // optional: drop carry on action to avoid instant big tick
        }

        private void OnSelfDamaged(IUnit unit, int amount, IUnit? source)
        {
            _sinceLastDamage = 0f;
            _regenCarry = 0f;
        }

        public void AddSpecialCharge(float amount)
        {
            if (amount <= 0f) return;
            _specialCharge = Mathf.Clamp(_specialCharge + amount, 0f, 1f);
        }
    }
}
