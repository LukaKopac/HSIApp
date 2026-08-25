using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using HSIApp.Models;
using System.Linq;

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

        private bool isDrawingRectangle;
        private int rectangleStartX;
        private int rectangleStartY;

        private bool updatingInteractionMode = false;

        private readonly Dictionary<SpectrumSelection, Rectangle> spectrumMarkers = new();

        public ImageViewer()
        {
            InitializeComponent();
        }

        public event Action<int, int>? PixelHovered;
        public event Action? PixelHoverEnded;
        public event Action<int, int>? PixelClicked;
        public event Action<bool>? PanModeChanged;
        public event Action<int, int, int, int>? RectangleSelected;

        private void ImageViewport_MouseLeftButtonDown(
            object sender,
            MouseButtonEventArgs e)
        {
            if (currentCube == null)
                return;

            if (interactionMode == ImageInteractionMode.RectangleSelection)
            {
                Point rectangleViewportPosition = e.GetPosition(ImageViewport);

                if (!TryGetImagePixel(
                        rectangleViewportPosition,
                        out rectangleStartX,
                        out rectangleStartY))
                {
                    return;
                }

                isDrawingRectangle = true;

                Canvas.SetLeft(SelectionRectangle, rectangleStartX);
                Canvas.SetTop(SelectionRectangle, rectangleStartY);

                SelectionRectangle.Width = 1;
                SelectionRectangle.Height = 1;
                SelectionRectangle.Visibility = Visibility.Visible;

                ImageViewport.CaptureMouse();

                e.Handled = true;
                return;
            }

            if (interactionMode != ImageInteractionMode.Selection)
                return;

            Point viewportPosition = e.GetPosition(ImageViewport);

            if (!TryGetImagePixel(viewportPosition, out int x, out int y))
                return;

            PixelClicked?.Invoke(x, y);
        }

        public void LoadCube(HsiCube cube)
        {
            ClearSpectrumMarkers();

            double? previousWavelength = null;

            if (currentCube != null &&
                currentCube.Metadata.Wavelengths.Length > 0)
            {
                int currentBand = Math.Clamp(
                    (int)BandSlider.Value,
                    0,
                    currentCube.Metadata.Wavelengths.Length - 1);

                previousWavelength =
                    currentCube.Metadata.Wavelengths[currentBand];
            }
            
            currentCube = cube;

            BandSlider.Minimum = 0;
            BandSlider.Maximum = cube.Bands - 1;

            int bandIndex = previousWavelength.HasValue
                ? FindClosestBandIndex(
                    cube.Metadata.Wavelengths,
                    previousWavelength.Value)
                : 0;

            BandSlider.Value = bandIndex;
            DisplayBand(bandIndex);

            BandLabel.Text =
                $"{cube.Metadata.Wavelengths[bandIndex]:F1} nm";

            Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Loaded,
                new Action(() =>
                {
                    // Avoid fitting an old cube if the active cube changed meanwhile.
                    if (ReferenceEquals(currentCube, cube))
                    {
                        FitImage();
                    }
                }));
        }

        private static int FindClosestBandIndex(
            double[] wavelengths,
            double targetWavelength)
        {
            int closestIndex = 0;
            double smallestDifference =
                Math.Abs(wavelengths[0] - targetWavelength);

            for (int index = 1; index < wavelengths.Length; index++)
            {
                double difference =
                    Math.Abs(wavelengths[index] - targetWavelength);

                if (difference < smallestDifference)
                {
                    smallestDifference = difference;
                    closestIndex = index;
                }
            }

            return closestIndex;
        }

        public void AddSpectrumMarker(SpectrumSelection selection)
        {
            if (currentCube == null || spectrumMarkers.ContainsKey(selection))
                return;

            int left;
            int top;
            int width;
            int height;

            if (selection.Kind == SpectrumSelectionKind.AreaAverage)
            {
                int horizontalRadius = selection.Width / 2;
                int verticalRadius = selection.Height / 2;

                left = Math.Max(0, selection.X - horizontalRadius);
                top = Math.Max(0, selection.Y - verticalRadius);

                int right = Math.Min(currentCube.Width - 1, selection.X + horizontalRadius);
                int bottom = Math.Min(currentCube.Height - 1, selection.Y + verticalRadius);

                width = right - left + 1;
                height = bottom - top + 1;
            }
            else
            {
                left = selection.X;
                top = selection.Y;
                width = selection.Width;
                height = selection.Height;
            }

            var marker = new Rectangle
            {
                Width = width,
                Height = height,
                Stroke = CreateMarkerBrush(selection.Color, 255),
                Fill = CreateMarkerBrush(selection.Color, 48),
                StrokeThickness = 1,
                IsHitTestVisible = false
            };

            Canvas.SetLeft(marker, left);
            Canvas.SetTop(marker, top);

            spectrumMarkers.Add(selection, marker);
            ImageCanvas.Children.Add(marker);

            selection.PropertyChanged += SpectrumSelection_PropertyChanged;
        }

        public void RemoveSpectrumMarker(SpectrumSelection selection)
        {
            if (!spectrumMarkers.Remove(selection, out Rectangle? marker))
                return;

            selection.PropertyChanged -= SpectrumSelection_PropertyChanged;
            ImageCanvas.Children.Remove(marker);
        }

        private void SpectrumSelection_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (sender is not SpectrumSelection selection ||
                e.PropertyName != nameof(SpectrumSelection.Color) ||
                !spectrumMarkers.TryGetValue(selection, out Rectangle? marker))
            {
                return;
            }

            marker.Stroke = CreateMarkerBrush(selection.Color, 255);
            marker.Fill = CreateMarkerBrush(selection.Color, 48);
        }

        private static Brush CreateMarkerBrush(System.Drawing.Color color, byte alpha)
        {
            return new SolidColorBrush(
                Color.FromArgb(alpha, color.R, color.G, color.B));
        }

        public void ClearSpectrumMarkers()
        {
            foreach (SpectrumSelection selection in
                spectrumMarkers.Keys.ToList())
            {
                RemoveSpectrumMarker(selection);
            }
        }

        public void ClearCube()
        {
            ClearSpectrumMarkers();

            currentCube = null;
            BandImage.Source = null;

            BandSlider.Minimum = 0;
            BandSlider.Maximum = 0;
            BandSlider.Value = 0;

            BandLabel.Text = "No cube loaded";
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

            double wavelength = currentCube.Metadata.Wavelengths[bandIndex];

            BandLabel.Text = $"{wavelength:F1} nm";
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

        private void BandImage_MouseLeave(object sender, MouseEventArgs e)
        {
            PixelHoverEnded?.Invoke();
        }

        private void ImageViewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (currentCube == null)
                return;

            if (isDrawingRectangle)
            {
                Point rectangleViewportPosition = e.GetPosition(ImageViewport);

                if (TryGetClampedImagePixel(
                        rectangleViewportPosition,
                        out int currentX,
                        out int currentY))
                {
                    UpdateSelectionRectangle(currentX, currentY);
                }

                e.Handled = true;
                return;
            }

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
            else
            {
                PixelHoverEnded?.Invoke();
            }
        }

        private void ImageViewport_MouseLeave(object sender, MouseEventArgs e)
        {
            PixelHoverEnded?.Invoke();
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
            if (isDrawingRectangle &&
                e.ChangedButton == MouseButton.Left)
            {
                Point rectangleViewportPosition = e.GetPosition(ImageViewport);

                if (TryGetClampedImagePixel(
                        rectangleViewportPosition,
                        out int endX,
                        out int endY))
                {
                    int left = Math.Min(rectangleStartX, endX);
                    int top = Math.Min(rectangleStartY, endY);

                    int width = Math.Abs(endX - rectangleStartX) + 1;
                    int height = Math.Abs(endY - rectangleStartY) + 1;

                    RectangleSelected?.Invoke(left, top, width, height);
                }

                isDrawingRectangle = false;
                SelectionRectangle.Visibility = Visibility.Collapsed;

                ImageViewport.ReleaseMouseCapture();

                e.Handled = true;
                return;
            }

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

            string? selectionMode = item.Tag?.ToString();

            if (selectionMode == "Area Average")
            {
                SetInteractionMode(ImageInteractionMode.Selection);
            }
            else if (selectionMode == "Rectangle")
            {
                SetInteractionMode(ImageInteractionMode.RectangleSelection);
            }
        }

        private bool TryGetClampedImagePixel(
            Point viewportPosition,
            out int x,
            out int y)
        {
            x = 0;
            y = 0;

            if (currentCube == null)
                return false;

            double imageX =
                (viewportPosition.X - ImageTranslation.X) / ImageScale.ScaleX;

            double imageY =
                (viewportPosition.Y - ImageTranslation.Y) / ImageScale.ScaleY;

            x = Math.Clamp(
                (int)Math.Floor(imageX),
                0,
                currentCube.Width - 1);

            y = Math.Clamp(
                (int)Math.Floor(imageY),
                0,
                currentCube.Height - 1);

            return true;
        }

        private void UpdateSelectionRectangle(int endX, int endY)
        {
            int left = Math.Min(rectangleStartX, endX);
            int top = Math.Min(rectangleStartY, endY);

            int width = Math.Abs(endX - rectangleStartX) + 1;
            int height = Math.Abs(endY - rectangleStartY) + 1;

            Canvas.SetLeft(SelectionRectangle, left);
            Canvas.SetTop(SelectionRectangle, top);

            SelectionRectangle.Width = width;
            SelectionRectangle.Height = height;
        }
    }
}
