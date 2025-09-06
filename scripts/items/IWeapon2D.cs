#nullable enable
using Godot;
using PlantProject.Core;

namespace PlantProject.Items
{
    public interface IWeapon2D
    {
        string ItemName { get; }

        // Requests an attack in the given facing by the specified user.
        // Returns true if the attack started.
        bool RequestAttack(IUnit user, Vector2 facing);
    }
}

