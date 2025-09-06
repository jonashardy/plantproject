#nullable enable
using System;

namespace PlantProject.Core
{
    public sealed class MeleeAttackAction : IAction
    {
        public string Name { get; }
        public float DamageMultiplier { get; }

        public MeleeAttackAction(string name = "Melee Attack", float damageMultiplier = 1.0f)
        {
            Name = name;
            DamageMultiplier = Math.Max(0f, damageMultiplier);
        }

        public bool CanExecute(IUnit user, IUnit? target = null)
        {
            if (user == null || !user.IsAlive) return false;
            if (target == null) return true;
            return target.IsAlive;
        }

        public void Execute(IUnit user, IUnit? target = null)
        {
            if (!CanExecute(user, target)) return;
            if (target == null) return; // No-op if no target provided.

            int raw = (int)Math.Max(0, MathF.Round(user.PowerLevel * DamageMultiplier));
            target.TakeDamage(raw, user);
        }
    }
}

