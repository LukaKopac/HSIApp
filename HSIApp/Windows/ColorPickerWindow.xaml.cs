using System.Windows;
using System.Windows.Media;
using System.Drawing;

namespace HSIApp.Windows
{
    public partial class ColorPickerWindow : Window
    {
        public System.Drawing.Color SelectedColor { get; private set; }

        public ColorPickerWindow(System.Drawing.Color initialColor)
        {
            InitializeComponent();

            RedSlider.Value = initialColor.R;
            GreenSlider.Value = initialColor.G;
            BlueSlider.Value = initialColor.B;

            UpdatePreview();
        }

        private void ColorSlider_ValueChanged(
            object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            UpdatePreview();
        }

        private void UpdatePreview()
        {
            byte r = (byte)RedSlider.Value;
            byte g = (byte)GreenSlider.Value;
            byte b = (byte)BlueSlider.Value;

            RedLabel.Text = r.ToString();
            GreenLabel.Text = g.ToString();
            BlueLabel.Text = b.ToString();

            ColorPreview.Background =
                new SolidColorBrush(
                    System.Windows.Media.Color.FromRgb(r, g, b));
        }

        private void Ok_Click(
            object sender,
            RoutedEventArgs e)
        {
            SelectedColor = System.Drawing.Color.FromArgb(
                (byte)RedSlider.Value,
                (byte)GreenSlider.Value,
                (byte)BlueSlider.Value);

            DialogResult = true;
        }

        private void Cancel_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
