#nullable enable
using System.Collections.Generic;
using PlantProject.Core;

namespace PlantProject.Player
{
    public sealed class PlayerUnit : UnitBase
    {
        public PlayerUnit(
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

