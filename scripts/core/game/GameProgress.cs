#nullable enable

namespace PlantProject.Core
{
    public static class GameProgress
    {
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
            BonusPowerLevels = 0;
            SwordBaseDamageMultiplier = 1f;
            SwordCooldownDelta = 0f;
            SwordConeBonusDegrees = 0f;
            MaxHpFlatBonus = 0;
            DefenseAdd = 0;
            SpeedMultiplier = 1f;
            RegenPercentBonus = 0f;
        }
    }
}
