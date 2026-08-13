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

        // Zooming
        private double zoom = 1.0;
        private const double ZoomStep = 1.2;
        private const double MinZoom = 0.5;
        private const double MaxZoom = 10.0;

        // Panning
        private bool isPanning = false;
        private Point panStartMouse;
        private double panStartX;
        private double panStartY;

        public ImageViewer()
        {
            InitializeComponent();
        }

        public event Action<int, int>? PixelHovered;
        public event Action<int, int>? PixelClicked;

        private void ImageViewport_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (currentCube == null)
                return;

            Point viewportPosition = e.GetPosition(ImageViewport);

            if (!TryGetImagePixel(viewportPosition, out int x, out int y))
                return;

            PixelClicked?.Invoke(x, y);
        }

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

        private bool TryGetImagePixel(
            Point viewportPosition,
            out int x,
            out int y)
        {
            x = 0;
            y = 0;

            if (currentCube == null)
                return false;

            // Undo translation
            double imageX =
                (viewportPosition.X - ImageTranslation.X) / ImageScale.ScaleX;

            double imageY =
                (viewportPosition.Y - ImageTranslation.Y) / ImageScale.ScaleY;

            x = (int)Math.Floor(imageX);
            y = (int)Math.Floor(imageY);

            // Check whether the position is actually inside the cube
            if (x < 0 || x >= currentCube.Width ||
                y < 0 || y >= currentCube.Height)
            {
                return false;
            }

            return true;
        }

        private void ImageViewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (currentCube == null)
                return;

            if (isPanning)
            {
                Point currentMouse = e.GetPosition(ImageViewport);

                Vector delta = currentMouse - panStartMouse;

                ImageTranslation.X = panStartX + delta.X;
                ImageTranslation.Y = panStartY + delta.Y;

                e.Handled = true;
                return;
            }

            Point viewportPosition = e.GetPosition(ImageViewport);

            if (TryGetImagePixel(viewportPosition, out int x, out int y))
            {
                PixelHovered?.Invoke(x, y);
            }
        }

        private void ImageViewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (currentCube == null)
                return;

            Point mousePosition = e.GetPosition(ImageViewport);

            double oldZoom = zoom;

            if (e.Delta > 0)
            {
                zoom *= ZoomStep;
            }
            else
            {
                zoom /= ZoomStep;
            }

            zoom = Math.Clamp(zoom, MinZoom, MaxZoom);

            if (Math.Abs(zoom - oldZoom) < 0.0001)
                return;

            // Position of the mouse relative to the image
            double imageX = (mousePosition.X - ImageTranslation.X) / oldZoom;
            double imageY = (mousePosition.Y - ImageTranslation.Y) / oldZoom;

            // Update scale
            ImageScale.ScaleX = zoom;
            ImageScale.ScaleY = zoom;

            // Keep the same image point underneath the mouse
            ImageTranslation.X = mousePosition.X - imageX * zoom;
            ImageTranslation.Y = mousePosition.Y - imageY * zoom;

            e.Handled = true;
        }

        private void ImageViewport_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle)
                return;

            isPanning = true;

            panStartMouse = e.GetPosition(ImageViewport);

            panStartX = ImageTranslation.X;
            panStartY = ImageTranslation.Y;

            ImageViewport.CaptureMouse();

            e.Handled = true;
        }

        private void ImageViewport_MouseUp(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle)
                return;

            isPanning = false;

            ImageViewport.ReleaseMouseCapture();

            e.Handled = true;
        }
    }
}
