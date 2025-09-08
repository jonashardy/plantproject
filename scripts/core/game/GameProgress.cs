#nullable enable

namespace PlantProject.Core
{
    public static class GameProgress
    {
        // Lives
        public static int LivesInitial { get; set; } = 3;
        public static int LivesRemaining { get; set; } = LivesInitial;
        // UI/flow flags that persist across reloads
        public static bool PendingRespawnCountdown { get; set; } = false;

        public static int CurrentLevelNumber { get; set; } = 1;
        // Run upgrades
        public static int BonusPowerLevels { get; set; } = 0;
        public static float SwordBaseDamageMultiplier { get; set; } = 1f;
        public static float SwordCooldownDelta { get; set; } = 0f;
        public static float SwordConeBonusDegrees { get; set; } = 0f;
        public static int MaxHpFlatBonus { get; set; } = 0;
        public static int DefenseAdd { get; set; } = 0; // percent
        public static float SpeedMultiplier { get; set; } = 1f;
        public static float RegenPercentBonus { get; set; } = 0f;

        public static void NextLevel() => CurrentLevelNumber = System.Math.Max(1, CurrentLevelNumber + 1);
        public static void Reset()
        {
            CurrentLevelNumber = 1;
            LivesRemaining = LivesInitial;
            BonusPowerLevels = 0;
            SwordBaseDamageMultiplier = 1f;
            SwordCooldownDelta = 0f;
            SwordConeBonusDegrees = 0f;
            MaxHpFlatBonus = 0;
            DefenseAdd = 0;
            SpeedMultiplier = 1f;
            RegenPercentBonus = 0f;
            PendingRespawnCountdown = false;
        }

        public static bool UseLife()
        {
            if (LivesRemaining <= 0) return false;
            LivesRemaining = System.Math.Max(0, LivesRemaining - 1);
            return LivesRemaining > 0;
        }
    }
}
