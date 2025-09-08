#nullable enable
using Godot;

namespace PlantProject.Core
{
    public static class UI
    {
        private static UIConfig? _config;
        private static UIConfig Config => _config ??= LoadConfig();

        private static UIConfig LoadConfig()
        {
            var res = ResourceLoader.Load<UIConfig>("res://resources/ui.tres");
            return res ?? new UIConfig();
        }

        public static float CountdownSeconds => Config.CountdownSeconds;
        public static float FightFadeSeconds => Config.FightFadeSeconds;
    }
}
