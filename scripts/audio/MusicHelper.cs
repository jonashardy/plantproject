#nullable enable
using Godot;

namespace PlantProject.Audio
{
    public static class MusicHelper
    {
        public const string MenuTrackPath = "res://assets/audio/music/Pixel Dreamscape.mp3";
        public const string GameTrackPath = "res://assets/audio/music/Neon Shadows.mp3";

        public static AmbientMusic EnsureAmbient(SceneTree tree)
        {
            var root = tree.Root;
            var ambient = root.GetNodeOrNull<AmbientMusic>("AmbientMusic");
            if (ambient == null)
            {
                ambient = new AmbientMusic { Name = "AmbientMusic" };
                root.AddChild(ambient);
            }
            return ambient;
        }

        public static void EnsureTrack(SceneTree tree, string resourcePath)
        {
            var ambient = EnsureAmbient(tree);
            var cur = ambient.CurrentTrackName ?? string.Empty;
            if (!cur.EndsWith(Normalize(resourcePath)))
            {
                ambient.PlayTrackByPath(resourcePath);
            }
            else if (!ambient.Playing)
            {
                ambient.Play();
            }
        }

        public static void EnsureMenuTrack(SceneTree tree) => EnsureTrack(tree, MenuTrackPath);
        public static void EnsureGameTrack(SceneTree tree) => EnsureTrack(tree, GameTrackPath);

        private static string Normalize(string path)
        {
            var p = path.Replace("\\", "/");
            int slash = p.LastIndexOf('/') + 1;
            if (slash > 0 && slash < p.Length) p = p.Substring(slash);
            return p;
        }
    }
}

