using HSIApp.Models;
using System.Windows;

namespace HSIApp.Windows
{
    public partial class CubeInfoWindow : Window
    {
        public CubeInfoWindow(LoadedCube cube)
        {
            InitializeComponent();
            DataContext = cube;
        }
    }
}
