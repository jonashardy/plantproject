#nullable enable
using System;
using System.Collections.Generic;
using Godot;
using PlantProject.UI;

namespace PlantProject.Core
{
    [GlobalClass]
    public partial class GodotUnit2D : Node2D, IUnit
    {
        private UnitBase? _model;
        private HealthBar2D? _healthBar;
        private float _damagePowerMod;

        [Export] public string UnitName { get; set; } = "Unit";
        [Export] public int MaxHpInit { get; set; } = 10;
        [Export] public int CurrentHpInit { get; set; } = 10;
        [Export] public int DefenseInit { get; set; } = 0;
        [Export] public int PowerLevelInit { get; set; } = 1;
        [Export] public float SpeedInit { get; set; } = 150f;

        public string Name => _model?.Name ?? UnitName;
        public int MaxHp => _model?.MaxHp ?? MaxHpInit;
        public int CurrentHp => _model?.CurrentHp ?? CurrentHpInit;
        public int Defense => _model?.Defense ?? DefenseInit;
        public int PowerLevel => _model?.PowerLevel ?? PowerLevelInit;
        public float Speed => _model?.Speed ?? SpeedInit;
        public IReadOnlyList<IAction> Actions => _model?.Actions ?? Array.Empty<IAction>();
        public bool IsAlive => _model?.IsAlive ?? CurrentHpInit > 0;
        // Temporary additive multiplier to power-based damage (e.g., +0.1 per kill)
        public float DamagePowerMod => _damagePowerMod;

        public event UnitDamagedEventHandler? Damaged;
        public event UnitHealedEventHandler? Healed;
        public event UnitDiedEventHandler? Died;
        public event UnitActionEventHandler? ActionStarted;
        public event UnitActionEventHandler? ActionCompleted;

        // When true, this unit is not allowed to perform actions (e.g., sprinting)
        protected virtual bool ActionsLocked => false;

        public override void _Ready()
        {
            if (_model == null)
            {
                var def = CreateDefaultModel();
                SetUnitModel(def);
            }

            EnsureHealthBar();

            // Group units for gameplay queries (e.g., weapon hit checks)
            if (!IsInGroup("units"))
                AddToGroup("units");
        }

        protected virtual UnitBase CreateDefaultModel()
        {
            return new DefaultUnitModel(UnitName, MaxHpInit, CurrentHpInit, DefenseInit, PowerLevelInit, SpeedInit);
        }

        protected void SetUnitModel(UnitBase model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            UnsubscribeFromModel();
            _model = model;
            SubscribeToModel();
        }

        public void AddDamagePowerMod(float delta)
        {
            _damagePowerMod += delta;
        }

        private void SubscribeToModel()
        {
            if (_model == null) return;
            _model.Damaged += OnModelDamaged;
            _model.Healed += OnModelHealed;
            _model.Died += OnModelDied;
            _model.ActionStarted += OnModelActionStarted;
            _model.ActionCompleted += OnModelActionCompleted;
        }

        private void UnsubscribeFromModel()
        {
            if (_model == null) return;
            _model.Damaged -= OnModelDamaged;
            _model.Healed -= OnModelHealed;
            _model.Died -= OnModelDied;
            _model.ActionStarted -= OnModelActionStarted;
            _model.ActionCompleted -= OnModelActionCompleted;
        }

        private void EnsureHealthBar()
        {
            if (_healthBar != null) return;
            var existing = GetNodeOrNull<HealthBar2D>("HealthBar");
            if (existing != null)
            {
                _healthBar = existing;
                _healthBar.ZAsRelative = false;
                _healthBar.ZIndex = 10000;
                return;
            }

            var hb = new HealthBar2D
            {
                Name = "HealthBar",
                ZIndex = 10000,
                ZAsRelative = false
            };

            // Try to size and position based on a child sprite named "Sprite"
            var sprite = GetNodeOrNull<Sprite2D>("Sprite");
            if (sprite?.Texture != null)
            {
                var texSize = sprite.Texture.GetSize();
                var scale = sprite.Scale;
                float width = texSize.X * scale.X;
                float height = texSize.Y * scale.Y;
                hb.Size = new Vector2(Mathf.Max(40f, width), 6f);
                hb.Offset = new Vector2(0f, -(height * 0.5f + 8f));
            }

            AddChild(hb);
            _healthBar = hb;
            // Ensure highest world-space visibility
            _healthBar.ZAsRelative = false;
            _healthBar.ZIndex = 10000;
        }

        private void OnModelDamaged(IUnit unit, int amount, IUnit? source) => Damaged?.Invoke(this, amount, source);
        private void OnModelHealed(IUnit unit, int amount, IUnit? source) => Healed?.Invoke(this, amount, source);
        private void OnModelDied(IUnit unit, IUnit? source) => Died?.Invoke(this, source);
        private void OnModelActionStarted(IUnit unit, IAction action) => ActionStarted?.Invoke(this, action);
        private void OnModelActionCompleted(IUnit unit, IAction action) => ActionCompleted?.Invoke(this, action);

        public int TakeDamage(int amount, IUnit? source = null)
        {
            if (_model == null) return 0;
            return _model.TakeDamage(amount, source);
        }

        public int Heal(int amount, IUnit? source = null)
        {
            if (_model == null) return 0;
            return _model.Heal(amount, source);
        }

        public bool CanPerformAction(IAction action, IUnit? target = null)
        {
            if (_model == null) return false;
            if (ActionsLocked) return false;
            return _model.CanPerformAction(action, target);
        }

        public void PerformAction(IAction action, IUnit? target = null)
        {
            if (_model == null) return;
            if (ActionsLocked) return;
            _model.PerformAction(action, target);
        }

        private sealed class DefaultUnitModel : UnitBase
        {
            public DefaultUnitModel(string name, int maxHp, int currentHp, int defense, int powerLevel, float speed)
                : base(name, maxHp, currentHp, defense, powerLevel, speed) { }
        }
    }
}
