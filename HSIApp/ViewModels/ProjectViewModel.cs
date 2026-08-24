using HSIApp.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace HSIApp.ViewModels
{
    public class ProjectViewModel : ViewModelBase
    {
        private LoadedCube? activeCube;

        public ObservableCollection<LoadedCube> Cubes { get; } = new();

        public LoadedCube? ActiveCube
        {
            get => activeCube;
            set
            {
                if (SetProperty(ref activeCube, value))
                {
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public string Status =>
            ActiveCube == null
                ? "No cube loaded"
                : $"Active: {ActiveCube.DisplayName}";

        public LoadedCube? FindByPath(string sourcePath)
        {
            return Cubes.FirstOrDefault(cube =>
                string.Equals(
                    cube.SourcePath,
                    sourcePath,
                    StringComparison.OrdinalIgnoreCase));
        }

        public void AddCube(LoadedCube cube)
        {
            Cubes.Add(cube);
            ActiveCube = cube;
        }

        public void RemoveCube(LoadedCube cube)
        {
            int index = Cubes.IndexOf(cube);

            if (index < 0)
                return;

            bool wasActive = ActiveCube == cube;

            Cubes.Remove(cube);

            if (!wasActive)
                return;

            ActiveCube =
                Cubes.Count == 0
                    ? null
                    : Cubes[Math.Min(index, Cubes.Count - 1)];
        }
    }
}
