#nullable enable
using System.Collections.Generic;
using Godot;
using PlantProject.Core;

namespace PlantProject.Player
{
    [GlobalClass]
    public partial class PlayerUnit2D : GodotUnit2D
    {
        [Export] public bool AddDefaultMelee { get; set; } = true;

        public override void _Ready()
        {
            // Build a logical PlayerUnit model and attach to the Node wrapper
            var actions = new List<IAction>();
            if (AddDefaultMelee)
            {
                actions.Add(new MeleeAttackAction("Slash", 1.0f));
            }

            var model = new PlayerUnit(UnitName, MaxHpInit, CurrentHpInit, DefenseInit, PowerLevelInit, SpeedInit, actions);
            // Hook model into the Node wrapper
            SetUnitModel(model);

            base._Ready();
        }
    }
}
