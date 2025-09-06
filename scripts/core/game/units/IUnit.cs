#nullable enable
using System.Collections.Generic;

namespace PlantProject.Core
{
    public delegate void UnitDamagedEventHandler(IUnit unit, int amount, IUnit? source);
    public delegate void UnitHealedEventHandler(IUnit unit, int amount, IUnit? source);
    public delegate void UnitDiedEventHandler(IUnit unit, IUnit? source);
    public delegate void UnitActionEventHandler(IUnit unit, IAction action);

    public interface IUnit
    {
        string Name { get; }
        int MaxHp { get; }
        int CurrentHp { get; }
        int Defense { get; }
        int PowerLevel { get; }
        float Speed { get; }
        IReadOnlyList<IAction> Actions { get; }
        bool IsAlive { get; }

        int TakeDamage(int amount, IUnit? source = null);
        int Heal(int amount, IUnit? source = null);

        bool CanPerformAction(IAction action, IUnit? target = null);
        void PerformAction(IAction action, IUnit? target = null);

        event UnitDamagedEventHandler? Damaged;
        event UnitHealedEventHandler? Healed;
        event UnitDiedEventHandler? Died;
        event UnitActionEventHandler? ActionStarted;
        event UnitActionEventHandler? ActionCompleted;
    }
}

