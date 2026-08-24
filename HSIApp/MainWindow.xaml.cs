using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using HSIApp.Models;
using HSIApp.Controls;
using System.Windows.Controls;
using System.Drawing;
using System.Globalization;
using System.IO;

namespace HSIApp
{
    public partial class MainWindow : Window
    {

        private HsiCube? currentCube;
        private List<SpectrumSelection> selectedSpectra = new();
        private int spectrumAveragingSize = 5;
        private int nextSpectrumId = 1;

        private bool interactiveBeforePan = false;

        private ImageInteractionMode modeBeforePan =
            ImageInteractionMode.Selection;

        private readonly Color[] spectrumColors =
        {
            Color.Red,
            Color.Blue,
            Color.Green,
            Color.Orange,
            Color.Purple,
            Color.Brown,
            Color.Cyan,
            Color.Magenta
        };

        public MainWindow()
        {
            InitializeComponent();

            Viewer.PixelHovered += Viewer_PixelHovered;
            Viewer.PixelHoverEnded += Viewer_PixelHoverEnded;
            Viewer.PixelClicked += Viewer_PixelClicked;

            Viewer.PanModeChanged += Viewer_PanModeChanged;

            Viewer.RectangleSelected += Viewer_RectangleSelected;

            Spectrum.InteractiveChanged +=
                Spectrum_InteractiveChanged;

            SpectrumManager.SpectrumSelectionChanged +=
                SpectrumManager_SpectrumSelectionChanged;

            SpectrumManager.SaveSpectraRequested +=
                SpectrumManager_SaveSpectraRequested;
            
            SpectrumManager.SpectrumRemoved +=
                SpectrumManager_SpectrumRemoved;
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

            float[] spectrum = currentCube.GetAverageSpectrum(y, x, spectrumAveragingSize);

            Spectrum.DisplaySpectrum(
                currentCube.Metadata.Wavelengths,
                spectrum, x, y);
        }

        private void Viewer_PixelHoverEnded()
        {
            Spectrum.ClearCursorSpectrum();
        }

        private void Viewer_PixelClicked(int x, int y)
        {
            if (currentCube == null)
                return;

            if (!Spectrum.IsInteractive)
                return;

            float[] spectrum = currentCube.GetAverageSpectrum(y, x, spectrumAveragingSize);

            SpectrumSelection selection = new SpectrumSelection
            {
                Id = nextSpectrumId,
                Name = $"Spectrum {nextSpectrumId}",
                X = x,
                Y = y,
                Kind = SpectrumSelectionKind.AreaAverage,
                Width = spectrumAveragingSize,
                Height = spectrumAveragingSize,
                Wavelengths = currentCube.Metadata.Wavelengths,
                Spectrum = spectrum,
                Color = spectrumColors[(nextSpectrumId - 1) % spectrumColors.Length]
            };

            nextSpectrumId++;

            selectedSpectra.Add(selection);

            SpectrumManager.AddSpectrum(selection);

            Spectrum.AddSelectedSpectrum(selection);

            Viewer.AddSpectrumMarker(selection);

            Debug.WriteLine(
                $"Added {selection.Name} at ({selection.X}, {selection.Y})");

            Debug.WriteLine(
                $"Total selected spectra: {selectedSpectra.Count}");
        }

        private void SpectrumManager_SpectrumSelectionChanged(
            SpectrumSelection selection,
            bool selected)
        {
            Spectrum.SetSpectrumSelected(
                selection,
                selected);
        }

        private void SpectrumManager_SpectrumRemoved(
            SpectrumSelection selection)
        {
            Spectrum.RemoveSelectedSpectrum(selection);
            Viewer.RemoveSpectrumMarker(selection);

            selectedSpectra.Remove(selection);
        }

        private void SpectrumManager_SaveSpectraRequested(
            IList<SpectrumSelection> selections)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save spectra",
                Filter = "CSV files (*.csv)|*.csv",
                DefaultExt = ".csv",
                FileName = "spectra.csv"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                using var writer = new StreamWriter(dialog.FileName);

                writer.WriteLine(
                    "SpectrumName,X,Y,WavelengthNm,Reflectance");

                foreach (var selection in selections)
                {
                    int pointCount = Math.Min(
                        selection.Wavelengths.Length,
                        selection.Spectrum.Length);

                    for (int i = 0; i < pointCount; i++)
                    {
                        writer.WriteLine(string.Join(",",
                            CsvEscape(selection.Name),
                            selection.X.ToString(CultureInfo.InvariantCulture),
                            selection.Y.ToString(CultureInfo.InvariantCulture),
                            selection.Wavelengths[i].ToString(CultureInfo.InvariantCulture),
                            selection.Spectrum[i].ToString(CultureInfo.InvariantCulture)));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to save spectra:\n{ex.Message}",
                    "Save Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static string CsvEscape(string value)
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        private void Spectrum_InteractiveChanged(bool interactive)
        {
            Viewer.SetInteractive(interactive);
        }

        private void Viewer_PanModeChanged(bool isPanMode)
        {
            if (isPanMode)
            {
                modeBeforePan =
                    Spectrum.IsInteractive
                        ? ImageInteractionMode.Selection
                        : ImageInteractionMode.Normal;

                if (Spectrum.IsInteractive)
                {
                    Spectrum.SetInteractive(false);
                }
            }
            else
            {
                if (modeBeforePan == ImageInteractionMode.Selection)
                {
                    Spectrum.SetInteractive(true);
                }
                else
                {
                    Viewer.SetInteractionMode(ImageInteractionMode.Normal);
                }
            }
        }

        private void Viewer_RectangleSelected(
            int x,
            int y,
            int width,
            int height)
        {
            if (currentCube == null)
                return;

            if (!Spectrum.IsInteractive)
                return;

            float[] spectrum = currentCube.GetAverageRectangleSpectrum(
                x,
                y,
                width,
                height);

            SpectrumSelection selection = new SpectrumSelection
            {
                Id = nextSpectrumId,
                Name = $"Rectangle {nextSpectrumId} ({width} × {height})",
                X = x,
                Y = y,
                Kind = SpectrumSelectionKind.Rectangle,
                Width = width,
                Height = height,
                Wavelengths = currentCube.Metadata.Wavelengths,
                Spectrum = spectrum,
                Color = spectrumColors[
                    (nextSpectrumId - 1) % spectrumColors.Length]
            };

            nextSpectrumId++;

            selectedSpectra.Add(selection);
            SpectrumManager.AddSpectrum(selection);
            Spectrum.AddSelectedSpectrum(selection);
            Viewer.AddSpectrumMarker(selection);
        }
    }
}