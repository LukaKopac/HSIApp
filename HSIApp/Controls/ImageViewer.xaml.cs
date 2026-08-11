using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Media.Imaging;

namespace HSIApp.Controls
{
    public partial class ImageViewer : UserControl
    {
        private HsiCube? currentCube;

        public ImageViewer()
        {
            InitializeComponent();
        }

        public event Action<int, int>? PixelHovered;

        public void LoadCube(HsiCube cube)
        {
            currentCube = cube;

            BandSlider.Minimum = 0;
            BandSlider.Maximum = cube.Bands - 1;
            BandSlider.Value = 0;

            DisplayBand(0);

            Debug.WriteLine(
                $"Cube: {cube.Width} x {cube.Height} x {cube.Bands}");
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

        private void BandSlider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            if (currentCube == null)
                return;

            int bandIndex = (int)BandSlider.Value;

            DisplayBand(bandIndex);

            BandLabel.Text = $"Band {bandIndex}";
        }

        private void BandImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (currentCube == null)
                return;

            Point position = e.GetPosition(BandImage);

            int x = (int)position.X;
            int y = (int)position.Y;

            PixelHovered?.Invoke(x, y);
        }
    }
}
