#nullable enable
using System.Collections.Generic;
using PlantProject.Core;

namespace PlantProject.Enemies
{
    public sealed class EnemyUnit : UnitBase
    {
        public EnemyUnit(
            string name,
            int maxHp,
            int currentHp,
            int defense,
            int powerLevel,
            float speed,
            IEnumerable<IAction>? actions = null)
            : base(name, maxHp, currentHp, defense, powerLevel, speed, actions)
        {
        }
    }
}

