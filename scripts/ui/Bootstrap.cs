#nullable enable
using Godot;
using PlantProject.Audio;

namespace PlantProject.UI
{
    [GlobalClass]
    public partial class Bootstrap : Node
    {
        public override async void _Ready()
        {
            ProcessMode = ProcessModeEnum.Always;
            // Ensure menu music is playing before loading Start Menu
            MusicHelper.EnsureMenuTrack(GetTree());
            // Allow one idle frame for audio init (safe on all platforms)
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
            GetTree().ChangeSceneToFile("res://scenes/ui/start_menu.tscn");
        }
    }
}

