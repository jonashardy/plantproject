#nullable enable
using System;
using System.Collections.Generic;

namespace PlantProject.Core
{
    public abstract class UnitBase : IUnit
    {
        private readonly List<IAction> _actions = new();
        private bool _hasDied;

        protected UnitBase(
            string name,
            int maxHp,
            int currentHp,
            int defense,
            int powerLevel,
            float speed,
            IEnumerable<IAction>? actions = null)
        {
            Name = name;
            MaxHp = Math.Max(1, maxHp);
            CurrentHp = Math.Clamp(currentHp, 0, MaxHp);
            Defense = Math.Max(0, defense);
            PowerLevel = Math.Max(0, powerLevel);
            Speed = Math.Max(0f, speed);
            if (actions != null) _actions.AddRange(actions);
            _hasDied = CurrentHp <= 0;
        }

        public string Name { get; protected set; }
        public int MaxHp { get; protected set; }
        public int CurrentHp { get; protected set; }
        public int Defense { get; protected set; }
        public int PowerLevel { get; protected set; }
        public float Speed { get; protected set; }
        public IReadOnlyList<IAction> Actions => _actions;
        public bool IsAlive => CurrentHp > 0;

        public event UnitDamagedEventHandler? Damaged;
        public event UnitHealedEventHandler? Healed;
        public event UnitDiedEventHandler? Died;
        public event UnitActionEventHandler? ActionStarted;
        public event UnitActionEventHandler? ActionCompleted;

        public virtual int TakeDamage(int amount, IUnit? source = null)
        {
            if (amount <= 0 || !IsAlive) return 0;
            int actual = Math.Max(0, CalculateDamageTaken(amount, source));
            if (actual <= 0) return 0;

            int before = CurrentHp;
            int newHp = Math.Max(0, before - actual);
            int applied = before - newHp;
            CurrentHp = newHp;

            if (applied > 0) OnDamaged(applied, source);

            if (CurrentHp <= 0 && !_hasDied)
            {
                _hasDied = true;
                OnDied(source);
            }

            return applied;
        }

        public virtual int Heal(int amount, IUnit? source = null)
        {
            if (amount <= 0 || !IsAlive) return 0;
            int before = CurrentHp;
            CurrentHp = Math.Min(MaxHp, before + amount);
            int healed = CurrentHp - before;
            if (healed > 0) OnHealed(healed, source);
            return healed;
        }

        public virtual bool CanPerformAction(IAction action, IUnit? target = null)
        {
            if (action == null) return false;
            if (!IsAlive) return false;
            if (!_actions.Contains(action)) return false;
            return action.CanExecute(this, target);
        }

        public virtual void PerformAction(IAction action, IUnit? target = null)
        {
            if (!CanPerformAction(action, target)) return;
            OnActionStarted(action);
            action.Execute(this, target);
            OnActionCompleted(action);
        }

        protected virtual int CalculateDamageTaken(int amount, IUnit? source)
        {
            // Convert Defense to percentage damage reduction (0-100 -> 0% - 100%)
            if (amount <= 0) return 0;
            int clampedDef = Math.Clamp(Defense, 0, 100);
            float reduction = clampedDef / 100f;
            float reduced = amount * (1f - reduction);
            int final = Math.Max(0, (int)MathF.Round(reduced));
            return final;
        }

        protected void AddAction(IAction action)
        {
            if (action == null) return;
            if (!_actions.Contains(action)) _actions.Add(action);
        }

        protected bool RemoveAction(IAction action) => _actions.Remove(action);

        protected virtual void OnDamaged(int amount, IUnit? source) => Damaged?.Invoke(this, amount, source);
        protected virtual void OnHealed(int amount, IUnit? source) => Healed?.Invoke(this, amount, source);
        protected virtual void OnDied(IUnit? source) => Died?.Invoke(this, source);
        protected virtual void OnActionStarted(IAction action) => ActionStarted?.Invoke(this, action);
        protected virtual void OnActionCompleted(IAction action) => ActionCompleted?.Invoke(this, action);
    }
}
