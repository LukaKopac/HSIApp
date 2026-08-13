using HSIApp.Models;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using System.Collections.Generic;

namespace HSIApp.Controls
{
    public partial class SpectrumViewer : UserControl
    {
        private ScottPlot.Plottables.Scatter? cursorSpectrum;
        private Dictionary<SpectrumSelection, ScottPlot.Plottables.Scatter> selectedSpectra = new();

        public SpectrumViewer()
        {
            InitializeComponent();
        }

        public bool IsInteractive =>
            InteractiveCheckBox.IsChecked == true;

        public void DisplaySpectrum(
            double[] wavelengths,
            float[] spectrum,
            int x,
            int y)
        {
            if (InteractiveCheckBox.IsChecked != true)
                return;
            
            PixelLabel.Text = $"Pixel: X = {x}, Y = {y}";

            double[] values = spectrum
                .Select(value => (double)value)
                .ToArray();

            // Remove the previous cursor spectrum
            if (cursorSpectrum != null)
            {
                SpectrumPlot.Plot.Remove(cursorSpectrum);
            }

            // Add the new cursor spectrum
            cursorSpectrum = SpectrumPlot.Plot.Add.Scatter(
                wavelengths,
                values);

            cursorSpectrum.Color = new ScottPlot.Color(
                100,
                100,
                100,
                255);

            cursorSpectrum.MarkerStyle.IsVisible = false;

            // Axis labels
            SpectrumPlot.Plot.Axes.Left.Label.Text = "Reflectance";
            SpectrumPlot.Plot.Axes.Bottom.Label.Text = "Wavelength (nm)";

            // Fixed axis ranges
            SpectrumPlot.Plot.Axes.SetLimitsX(
                wavelengths.Min(),
                wavelengths.Max());

            SpectrumPlot.Plot.Axes.SetLimitsY(0, 1.2);

            SpectrumPlot.Refresh();
        }

        public void AddSelectedSpectrum(SpectrumSelection selection)
        {
            double[] values = selection.Spectrum
                .Select(value => (double)value)
                .ToArray();

            var line = SpectrumPlot.Plot.Add.Scatter(
                selection.Wavelengths,
                values);

            line.Color = new ScottPlot.Color(
                selection.Color.R,
                selection.Color.G,
                selection.Color.B,
                selection.Color.A);

            line.MarkerStyle.IsVisible = false;

            selectedSpectra.Add(selection, line);

            selection.PropertyChanged += SpectrumSelection_PropertyChanged;

            SpectrumPlot.Refresh();
        }

        private void SpectrumSelection_PropertyChanged(
            object? sender,
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is not SpectrumSelection selection)
                return;

            if (e.PropertyName != nameof(SpectrumSelection.Color))
                return;

            if (!selectedSpectra.TryGetValue(
                    selection,
                    out var line))
                return;

            line.Color = new ScottPlot.Color(
                selection.Color.R,
                selection.Color.G,
                selection.Color.B,
                selection.Color.A);

            SpectrumPlot.Refresh();
        }

        private void InteractiveCheckBox_Checked(
            object sender,
            System.Windows.RoutedEventArgs e)
        {
            // Nothing to do yet.
        }

        private void InteractiveCheckBox_Unchecked(
            object sender,
            System.Windows.RoutedEventArgs e)
        {
            if (cursorSpectrum != null)
            {
                SpectrumPlot.Plot.Remove(cursorSpectrum);
                cursorSpectrum = null;
            }

            PixelLabel.Text = "Pixel: X = -, Y = -";

            SpectrumPlot.Refresh();
        }

        public void RemoveSelectedSpectrum(SpectrumSelection selection)
        {
            if (!selectedSpectra.TryGetValue(selection, out var line))
                return;

            selection.PropertyChanged -=
                SpectrumSelection_PropertyChanged;

            SpectrumPlot.Plot.Remove(line);

            selectedSpectra.Remove(selection);

            SpectrumPlot.Refresh();
        }

        public void SetSpectrumSelected(
            SpectrumSelection selection,
            bool selected)
        {
            if (!selectedSpectra.TryGetValue(selection, out var line))
                return;

            line.LineWidth = selected ? 3 : 1;

            SpectrumPlot.Refresh();
        }
    }
}
