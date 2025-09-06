#nullable enable
using System;
using System.Collections.Generic;

namespace PlantProject.Core
{
    public readonly struct RewardChoice
    {
        public readonly string Id;
        public readonly string Title;
        public readonly string Description;
        public RewardChoice(string id, string title, string description)
        {
            Id = id; Title = title; Description = description;
        }
    }

    public static class RewardSystem
    {
        private sealed class Def
        {
            public string Id = string.Empty;
            public string Title = string.Empty;
            public string Description = string.Empty;
            public int Weight = 1;
            public Action Apply = () => { };
        }

        private static readonly List<Def> _defs = new()
        {
            new Def { Id = "power_plus_1", Title = "Power Surge", Description = "+1 Power", Weight = 3,
                Apply = () => GameProgress.BonusPowerLevels += 1 },

            new Def { Id = "sword_dmg_20", Title = "Sharpened Blade", Description = "+20% Sword Damage", Weight = 3,
                Apply = () => GameProgress.SwordBaseDamageMultiplier *= 1.20f },

            new Def { Id = "cone_plus_10", Title = "Wider Arc", Description = "+10° Sword Cone", Weight = 2,
                Apply = () => GameProgress.SwordConeBonusDegrees += 10f },

            new Def { Id = "cd_minus_0_05", Title = "Quicker Strikes", Description = "-0.05s Sword Cooldown", Weight = 2,
                Apply = () => GameProgress.SwordCooldownDelta -= 0.05f },

            new Def { Id = "hp_plus_10", Title = "Vitality", Description = "+10 Max HP", Weight = 3,
                Apply = () => GameProgress.MaxHpFlatBonus += 10 },

            new Def { Id = "def_plus_5", Title = "Iron Skin", Description = "+5% Defense", Weight = 2,
                Apply = () => GameProgress.DefenseAdd += 5 },

            new Def { Id = "regen_plus_1", Title = "Second Wind", Description = "+1% Regen / sec", Weight = 2,
                Apply = () => GameProgress.RegenPercentBonus += 0.01f },
        };

        public static RewardChoice[] GetRandomChoices(int count, int? seed = null)
        {
            int n = Math.Clamp(count, 1, 5);
            var rng = seed.HasValue ? new Random(seed.Value) : new Random();

            // Weighted sampling without replacement (simple approach)
            var pool = new List<Def>(_defs);
            var res = new List<RewardChoice>(n);

            for (int k = 0; k < n && pool.Count > 0; k++)
            {
                int totalW = 0; foreach (var d in pool) totalW += Math.Max(1, d.Weight);
                int r = rng.Next(0, totalW);
                int acc = 0; int idx = 0;
                for (int i = 0; i < pool.Count; i++) { acc += Math.Max(1, pool[i].Weight); if (r < acc) { idx = i; break; } }
                var pick = pool[idx];
                res.Add(new RewardChoice(pick.Id, pick.Title, pick.Description));
                pool.RemoveAt(idx);
            }

            return res.ToArray();
        }

        public static void ApplyChoice(RewardChoice choice)
        {
            foreach (var d in _defs)
            {
                if (d.Id == choice.Id)
                {
                    d.Apply();
                    return;
                }
            }
        }
    }
}

