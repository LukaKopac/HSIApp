using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace HSIApp.Controls
{
    /// <summary>
    /// Interaction logic for ImageViewer.xaml
    /// </summary>
    public partial class ImageViewer : UserControl
    {

        private HsiCube? currentCube;

        public ImageViewer()
        {
            InitializeComponent();
        }

        public void LoadCube(HsiCube cube)
        {
            currentCube = cube;

            BandSlider.Minimum = 0;
            BandSlider.Maximum = cube.Bands - 1;
            BandSlider.Value = 0;

            UpdateMetadata();
            DisplayBand(0);
        }

        private void DisplayBand(int bandIndex)
        {
            if (currentCube == null)
                return;

            float[,] band = currentCube.GetBand(bandIndex);

            float[,] normalized = BandRenderer.NormalizeImage(band);

            BitmapSource bitmap = BandRenderer.ToBitmap(normalized);

            BandImage.Source = bitmap;
        }

        private void UpdateMetadata()
        {
            if (currentCube == null)
                return;

            MetadataDisplay.Text =
                $"Width: {currentCube.Width}\n" +
                $"Height: {currentCube.Height}\n" +
                $"Bands: {currentCube.Bands}\n";
        }

        private void BandSlider_ValueChanged(object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            if (currentCube == null)
                return;

            int bandIndex = (int)BandSlider.Value;

            DisplayBand(bandIndex);
            BandLabel.Text = $"Band {bandIndex}";
        }
    }
}
