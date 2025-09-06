#nullable enable
namespace PlantProject.Core
{
    public interface IAction
    {
        string Name { get; }

        bool CanExecute(IUnit user, IUnit? target = null);

        void Execute(IUnit user, IUnit? target = null);
    }
}

