using System.Windows.Controls;
using System.Linq;
using System.Diagnostics;

namespace HSIApp.Controls
{
    public partial class SpectrumViewer : UserControl
    {
        public SpectrumViewer()
        {
            InitializeComponent();
        }

        public void DisplaySpectrum(double[] wavelengths, float[] spectrum, int x, int y)
        {
            PixelLabel.Text = $"Pixel: X = {x}, Y = {y}";

            double[] values = spectrum
                .Select(value => (double)value)
                .ToArray();

            SpectrumPlot.Plot.Clear();

            var line = SpectrumPlot.Plot.Add.Scatter(wavelengths, values);
            
            // no individual markers
            line.MarkerStyle.IsVisible = false;

            // axis labels
            SpectrumPlot.Plot.Axes.Left.Label.Text = "Reflectance";
            SpectrumPlot.Plot.Axes.Bottom.Label.Text = "Wavelength (nm)";

            // fixed axis ranges

            SpectrumPlot.Plot.Axes.SetLimitsX(wavelengths.Min(), wavelengths.Max());

            //SpectrumPlot.Plot.Axes.AutoScale();
            SpectrumPlot.Plot.Axes.SetLimitsY(0, 1.5);

            SpectrumPlot.Refresh();
        }
    }
}
