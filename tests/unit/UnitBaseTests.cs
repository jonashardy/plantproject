#nullable enable
using System.Diagnostics;
using PlantProject.Core;

namespace PlantProject.Tests
{
    internal static class UnitBaseTests
    {
        public static void RunAll()
        {
            DamageReducesByDefense();
            HealClampsToMax();
            DiedEventFiresOnce();
        }

        private static void DamageReducesByDefense()
        {
            var u = new TestUnit("Test", maxHp: 10, currentHp: 10, defense: 3, power: 1, speed: 1f);
            int applied = u.TakeDamage(10);
            Debug.Assert(applied == 7, $"Expected 7, got {applied}");
            Debug.Assert(u.CurrentHp == 3, $"Expected HP 3, got {u.CurrentHp}");
        }

        private static void HealClampsToMax()
        {
            var u = new TestUnit("Test", maxHp: 10, currentHp: 5, defense: 0, power: 1, speed: 1f);
            int healed = u.Heal(10);
            Debug.Assert(healed == 5, $"Expected 5, got {healed}");
            Debug.Assert(u.CurrentHp == 10, $"Expected HP 10, got {u.CurrentHp}");
        }

        private static void DiedEventFiresOnce()
        {
            var u = new TestUnit("Test", maxHp: 5, currentHp: 5, defense: 0, power: 1, speed: 1f);
            int diedCount = 0;
            u.Died += (_, __) => diedCount++;

            u.TakeDamage(10);
            u.TakeDamage(1);
            Debug.Assert(diedCount == 1, $"Expected Died once, got {diedCount}");
            Debug.Assert(!u.IsAlive, "Unit should be dead");
        }

        private sealed class TestUnit : UnitBase
        {
            public TestUnit(string name, int maxHp, int currentHp, int defense, int power, float speed)
                : base(name, maxHp, currentHp, defense, power, speed) { }
        }
    }
}

