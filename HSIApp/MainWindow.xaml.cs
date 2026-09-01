using HSIApp.Models;
using HSIApp.Controls;
using HSIApp.ViewModels;
using HSIApp.Windows;
using Microsoft.Win32;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Media.Media3D;
using System.ComponentModel;
using System.Linq;

namespace HSIApp
{
    public partial class MainWindow : Window
    {

        private readonly ProjectViewModel project = new();

        private HsiCube? currentCube => project.ActiveCube?.Cube;
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

            DataContext = project;

            project.PropertyChanged += Project_PropertyChanged;

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

        private void Project_PropertyChanged(
            object? sender,
            PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ProjectViewModel.ActiveCube))
                return;

            Spectrum.ClearCursorSpectrum();

            if (project.ActiveCube == null)
            {
                Viewer.ClearCube();
                return;
            }

            Viewer.LoadCube(project.ActiveCube.Cube);

            foreach (SpectrumSelection selection in selectedSpectra
                .Where(selection => selection.CubeId == project.ActiveCube.Id))
            {
                Viewer.AddSpectrumMarker(selection);
            }
        }
        
        private void OpenCube_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Select hyperspectra cube",
                Filter = "RAW files (*.raw)|*.raw|All files (*.*)|*.*"
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                string sourcePath = Path.GetFullPath(dialog.FileName);

                LoadedCube? existingCube = project.FindByPath(sourcePath);

                if (existingCube != null)
                {
                    project.ActiveCube = existingCube;
                    return;
                }

                HsiCube cube = HsiLoader.Load(sourcePath);

                project.AddCube(new LoadedCube
                {
                    SourcePath = sourcePath,
                    Cube = cube
                });
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
            if (currentCube == null || project.ActiveCube == null)
                return;

            if (!Spectrum.IsInteractive)
                return;

            float[] spectrum = currentCube.GetAverageSpectrum(y, x, spectrumAveragingSize);

            SpectrumSelection selection = new SpectrumSelection
            {
                Id = nextSpectrumId,
                CubeId = project.ActiveCube!.Id,
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
            if (selections == null || selections.Count == 0)
                return;

            // Validate that all spectra use the same wavelength axis
            var referenceWavelengths = selections[0].Wavelengths;

            for (int s = 0; s < selections.Count; s++)
            {
                var selection = selections[s];

                if (selection.Wavelengths.Length != selection.Spectrum.Length)
                {
                    MessageBox.Show(
                        $"Spectrum \"{selection.Name}\" has a different number " +
                        "of wavelengths and reflectance values.",
                        "Cannot Save Spectra",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                if (selection.Wavelengths.Length != referenceWavelengths.Length)
                {
                    MessageBox.Show(
                        $"Spectrum \"{selection.Name}\" has a different number " +
                        "of wavelengths than the other spectra.",
                        "Cannot Save Spectra",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }

                for (int i = 0;  i < referenceWavelengths.Length; i++)
                {
                    if (Math.Abs(
                        selection.Wavelengths[i] - referenceWavelengths[i])
                        > 1e-6)
                    {
                        MessageBox.Show(
                            $"Spectrum \"{selection.Name}\" does not use the " +
                            "same wavelength axis as the other spectra.",
                            "Cannot Save Spectra",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);

                        return;
                    }
                }
            }

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

                // Header
                writer.Write("Wavelength");

                foreach (var selection in selections)
                {
                    writer.Write(",");
                    writer.Write(CsvEscape(selection.Name));
                }

                writer.WriteLine();

                // Data
                for (int i = 0; i < referenceWavelengths.Length; i++)
                {
                    writer.Write(
                        referenceWavelengths[i]
                            .ToString(CultureInfo.InvariantCulture));

                    foreach (var selection in selections)
                    {
                        writer.Write(",");
                        writer.Write(
                            selection.Spectrum[i]
                                .ToString(CultureInfo.InvariantCulture));
                    }

                    writer.WriteLine();
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
            if (currentCube == null || project.ActiveCube == null)
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
                CubeId = project.ActiveCube!.Id,
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

        private void CubeInfo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: LoadedCube cube })
                return;

            var window = new CubeInfoWindow(cube)
            {
                Owner = this
            };

            window.ShowDialog();
        }

        private void RemoveCube_Click(Object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: LoadedCube cube })
                return;

            List<SpectrumSelection> spectraToRemove = selectedSpectra
                .Where(selection => selection.CubeId == cube.Id)
                .ToList();

            string message =
                spectraToRemove.Count == 0
                ? $"Remove '{cube.DisplayName}' from this project?"
                : $"Remove '{cube.DisplayName}' and its " +
                  $"{spectraToRemove.Count} associated spectra?";

            if (MessageBox.Show(
                    message,
                    "Remove cube",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
            {
                return;
            }

            foreach (SpectrumSelection selection in spectraToRemove)
            {
                SpectrumManager.RemoveSpectrum(selection);
                Spectrum.RemoveSelectedSpectrum(selection);
                Viewer.RemoveSpectrumMarker(selection);

                selectedSpectra.Remove(selection);
            }

            project.RemoveCube(cube);
        }
    }
}