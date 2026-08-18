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

        private ImageInteractionMode interactionMode =
            ImageInteractionMode.Selection;

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

        private bool updatingInteractionMode = false;

        public ImageViewer()
        {
            InitializeComponent();
        }

        public event Action<int, int>? PixelHovered;
        public event Action<int, int>? PixelClicked;
        public event Action<bool>? PanModeChanged;

        private void ImageViewport_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (currentCube == null)
                return;

            if (interactionMode != ImageInteractionMode.Selection)
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

            if (interactionMode != ImageInteractionMode.Selection)
                return;

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

            if (e.Delta > 0)
            {
                ZoomAt(mousePosition, ZoomStep);
            }
            else
            {
                ZoomAt(mousePosition, 1.0 / ZoomStep);
            }

            e.Handled = true;
        }

        private void ZoomAt(
            Point viewportPosition,
            double factor)
        {
            double oldZoom = zoom;

            zoom *= factor;
            zoom = Math.Clamp(zoom, MinZoom, MaxZoom);

            if (Math.Abs(zoom - oldZoom) < 0.0001)
                return;

            double imageX =
                (viewportPosition.X - ImageTranslation.X) / oldZoom;

            double imageY =
                (viewportPosition.Y - ImageTranslation.Y) / oldZoom;

            ImageScale.ScaleX = zoom;
            ImageScale.ScaleY = zoom;

            ImageTranslation.X =
                viewportPosition.X - imageX * zoom;

            ImageTranslation.Y =
                viewportPosition.Y - imageY * zoom;
        }

        public void ZoomIn()
        {
            Point center = new Point(
                ImageViewport.ActualWidth / 2,
                ImageViewport.ActualHeight / 2);

            ZoomAt(center, ZoomStep);
        }

        private void ZoomInButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ZoomIn();
        }

        public void ZoomOut()
        {
            Point center = new Point(
                ImageViewport.ActualWidth / 2,
                ImageViewport.ActualHeight / 2);

            ZoomAt(center, 1.0 / ZoomStep);
        }

        private void ZoomOutButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ZoomOut();
        }

        private void ImageViewport_MouseDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Middle &&
                !(interactionMode == ImageInteractionMode.Pan &&
                  e.ChangedButton == MouseButton.Left))
            {
                return;
            }

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
            if (e.ChangedButton != MouseButton.Middle &&
                !(interactionMode == ImageInteractionMode.Pan &&
                  e.ChangedButton == MouseButton.Left))
            {
                return;
            }

            isPanning = false;

            ImageViewport.ReleaseMouseCapture();

            e.Handled = true;
        }

        public void SetInteractionMode(ImageInteractionMode mode)
        {
            if (updatingInteractionMode)
                return;

            updatingInteractionMode = true;

            interactionMode = mode;

            PanButton.IsChecked =
                mode == ImageInteractionMode.Pan;

            if (mode == ImageInteractionMode.Selection)
            {
                SelectionModeComboBox.SelectedItem =
                    AreaAverageSelectionItem;
            }

            if (mode != ImageInteractionMode.Pan)
            {
                isPanning = false;
                ImageViewport.ReleaseMouseCapture();
            }

            updatingInteractionMode = false;
        }

        public void SetInteractive(bool interactive)
        {
            if (interactive)
            {
                SetInteractionMode(ImageInteractionMode.Selection);
            }
            else
            {
                SetInteractionMode(ImageInteractionMode.Normal);
            }
        }

        private void PanButton_Checked(
            object sender,
            RoutedEventArgs e)
        {
            if (updatingInteractionMode)
                return;

            PanModeChanged?.Invoke(true);

            SetInteractionMode(ImageInteractionMode.Pan);
        }

        private void PanButton_Unchecked(
            object sender,
            RoutedEventArgs e)
        {
            if (updatingInteractionMode)
                return;

            PanModeChanged?.Invoke(false);

            SetInteractionMode(ImageInteractionMode.Selection);
        }

        public void FitImage()
        {
            if (currentCube == null)
                return;

            double viewportWidth = ImageViewport.ActualWidth;
            double viewportHeight = ImageViewport.ActualHeight;

            if (viewportWidth <= 0 || viewportHeight <= 0)
                return;

            double imageWidth = currentCube.Width;
            double imageHeight = currentCube.Height;

            double scaleX = viewportWidth / imageWidth;
            double scaleY = viewportHeight / imageHeight;

            zoom = Math.Min(scaleX, scaleY);

            // Fit is allowed to go below MinZoom.
            zoom = Math.Min(zoom, MaxZoom);

            ImageScale.ScaleX = zoom;
            ImageScale.ScaleY = zoom;

            double scaledWidth = imageWidth * zoom;
            double scaledHeight = imageHeight * zoom;

            ImageTranslation.X =
                (viewportWidth - scaledWidth) / 2;

            ImageTranslation.Y =
                (viewportHeight - scaledHeight) / 2;
        }

        private void FitButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            FitImage();
        }

        private void SelectionModeComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (!IsInitialized)
                return;

            if (SelectionModeComboBox.SelectedItem is not ComboBoxItem item)
                return;

            string? selectionMode = item.Content?.ToString();

            if (selectionMode == "Area Average")
            {
                SetInteractionMode(ImageInteractionMode.Selection);
            }
        }
    }
}
