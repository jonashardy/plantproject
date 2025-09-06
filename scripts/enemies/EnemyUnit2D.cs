#nullable enable
using System.Collections.Generic;
using Godot;
using PlantProject.Core;

namespace PlantProject.Enemies
{
    [GlobalClass]
    public partial class EnemyUnit2D : GodotUnit2D
    {
        [Export] public bool AddDefaultMelee { get; set; } = true;

        public override void _Ready()
        {
            var actions = new List<IAction>();
            if (AddDefaultMelee)
            {
                actions.Add(new MeleeAttackAction("Chomp", 0.8f));
            }

            var model = new EnemyUnit(UnitName, MaxHpInit, CurrentHpInit, DefenseInit, PowerLevelInit, SpeedInit, actions);
            SetUnitModel(model);

            base._Ready();
        }
    }
}

