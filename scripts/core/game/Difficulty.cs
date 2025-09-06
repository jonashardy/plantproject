#nullable enable
using System;
using Godot;

namespace PlantProject.Core
{
    public static class Difficulty
    {
        private static DifficultyConfig? _config;
        private static DifficultyConfig Config => _config ??= LoadConfig();

        private static DifficultyConfig LoadConfig()
        {
            var res = ResourceLoader.Load<DifficultyConfig>("res://resources/difficulty.tres");
            return res ?? new DifficultyConfig();
        }

        public static float GetHpMultiplier(int level)
        {
            int n = Math.Max(1, level);
            return MathF.Pow(Config.HpGrowth, n - 1);
        }

        public static int GetDefenseAdd(int level)
        {
            int n = Math.Max(1, level);
            return Math.Clamp((int)MathF.Round(Config.DefensePerLevel * (n - 1)), 0, Config.DefenseCap);
        }

        public static float GetPowerMultiplier(int level)
        {
            int n = Math.Max(1, level);
            return 1f + Config.PowerGrowth * (n - 1);
        }

        public static float GetSpeedMultiplier(int level)
        {
            int n = Math.Max(1, level);
            return Math.Clamp(1f + Config.SpeedGrowth * (n - 1), 1f, Config.SpeedCap);
        }

        public static float GetEnemyCooldownMultiplier(int level)
        {
            int n = Math.Max(1, level);
            float x = 1.0f - Config.CooldownDecrease * (n - 1);
            return Math.Clamp(x, Config.CooldownMin, Config.CooldownMax);
        }

        public static int GetInitialSpawnCount(int baseCount, int level)
        {
            int n = Math.Max(1, level);
            return Math.Max(0, baseCount + Config.SpawnIncrement * (n - 1));
        }
    }
}
