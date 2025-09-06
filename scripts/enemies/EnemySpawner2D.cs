#nullable enable
using System;
using Godot;
using PlantProject.Core;
using PlantProject.Levels;
using PlantProject.Core;

namespace PlantProject.Enemies
{
    [GlobalClass]
    public partial class EnemySpawner2D : Node2D
    {
        [Export] public PackedScene? EnemyScene { get; set; }
        [Export] public int InitialCount { get; set; } = 8;
        [Export] public int MinDistanceFromPlayer { get; set; } = 200;

        [Signal]
        public delegate void EnemiesSpawnedChangedEventHandler(int total);
        [Signal]
        public delegate void EnemiesAliveChangedEventHandler(int alive);

        public int TotalSpawned { get; private set; }
        public int TotalDead { get; private set; }
        public int TotalAlive => Math.Max(0, TotalSpawned - TotalDead);

        public override void _Ready()
        {
            if (EnemyScene == null)
            {
                EnemyScene = GD.Load<PackedScene>("res://scenes/enemies/enemy_unit.tscn");
            }

            var level = GetAncestorLevel();
            var player = level?.GetNodeOrNull<Node2D>("Player");
            var world = level?.WorldSize ?? new Vector2(2048, 2048);
            int levelNum = level?.LevelNumber ?? 1;

            int spawnCount = Difficulty.GetInitialSpawnCount(InitialCount, levelNum);
            var rnd = new Random();
            for (int i = 0; i < spawnCount; i++)
            {
                if (EnemyScene == null) break;
                var inst = EnemyScene.Instantiate<Node2D>();

                // Place at a random location, avoiding near-player spawns
                Vector2 pos;
                int tries = 0;
                do
                {
                    pos = new Vector2(
                        (float)rnd.NextDouble() * world.X,
                        (float)rnd.NextDouble() * world.Y
                    );
                } while (player != null && pos.DistanceTo(player.Position) < MinDistanceFromPlayer && ++tries < 20);

                inst.Position = pos;

                // Apply difficulty scaling to the enemy's baseline stats before adding to tree
                if (inst is Core.GodotUnit2D unit)
                {
                    float hpMul = Difficulty.GetHpMultiplier(levelNum);
                    int newMax = Math.Max(1, (int)MathF.Round(unit.MaxHpInit * hpMul));
                    unit.MaxHpInit = newMax;
                    unit.CurrentHpInit = newMax;

                    int defAdd = Difficulty.GetDefenseAdd(levelNum);
                    unit.DefenseInit = Math.Clamp(unit.DefenseInit + defAdd, 0, 100);

                    float powMul = Difficulty.GetPowerMultiplier(levelNum);
                    unit.PowerLevelInit = Math.Max(1, (int)MathF.Round(unit.PowerLevelInit * powMul));

                    float spdMul = Difficulty.GetSpeedMultiplier(levelNum);
                    unit.SpeedInit = unit.SpeedInit * spdMul;
                }

                AddChild(inst);
                TotalSpawned++;
                EmitSignal(SignalName.EnemiesSpawnedChanged, TotalSpawned);
                EmitSignal(SignalName.EnemiesAliveChanged, TotalAlive);

                if (inst is GodotUnit2D u)
                {
                    u.Died += OnEnemyDied;
                }
            }
        }

        private void OnEnemyDied(IUnit unit, IUnit? source)
        {
            TotalDead++;
            EmitSignal(SignalName.EnemiesAliveChanged, TotalAlive);
        }

        private SimpleLevel? GetAncestorLevel()
        {
            Node? n = GetParent();
            while (n != null)
            {
                if (n is SimpleLevel sl) return sl;
                n = n.GetParent();
            }
            return null;
        }
    }
}
