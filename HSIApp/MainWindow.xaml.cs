using Microsoft.Win32;
using System.Windows;

namespace HSIApp
{
    public partial class MainWindow : Window
    {

        private HsiCube? currentCube;

        public MainWindow()
        {
            InitializeComponent();
            Viewer.PixelHovered += Viewer_PixelHovered;
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
                currentCube = HsiLoader.Load(dialog.FileName);

                Viewer.LoadCube(currentCube);
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

        private void Viewer_PixelHovered(int x, int y)
        {
            if (currentCube == null)
                return;

            float[] spectrum = currentCube.GetSpectrum(y, x);

            Spectrum.DisplaySpectrum(
                currentCube.Metadata.Wavelengths,
                spectrum, x, y);
        }
    }
}