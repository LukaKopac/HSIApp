using Microsoft.Win32;
using System.Windows;

namespace HSIApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenCube_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Title = "Select hyperspectral cube";
            dialog.Filter = "RAW files (*.raw)|*.raw|All files (*.*)|*.*";

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                HsiCube cube = HsiLoader.Load(dialog.FileName);

                Viewer.LoadCube(cube);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load cube:\n{ex.Message}",
                    "Load Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}