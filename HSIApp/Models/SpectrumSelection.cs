using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace HSIApp.Models
{
    
    public enum SpectrumSelectionKind
    {
        AreaAverage,
        Rectangle
    }
    
    public class SpectrumSelection : INotifyPropertyChanged
    {
        private string name = "";
        private System.Drawing.Color color;

        public int Id { get; set; }

        public Guid CubeId { get; set; }

        public string Name
        {
            get => name;
            set
            {
                if (name == value)
                    return;

                name = value;
                OnPropertyChanged();
            }
        }

        public int X { get; set; }

        public int Y { get; set; }

        public SpectrumSelectionKind Kind { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        public double[] Wavelengths { get; set; }
            = Array.Empty<double>();

        public float[] Spectrum { get; set; }
            = Array.Empty<float>();

        public System.Drawing.Color Color
        {
            get => color;
            set
            {
                if (color == value)
                    return;

                color = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BrushColor));
            }
        }

        public Brush BrushColor =>
            new SolidColorBrush(
                System.Windows.Media.Color.FromArgb(
                    Color.A,
                    Color.R,
                    Color.G,
                    Color.B));

        public event PropertyChangedEventHandler?
            PropertyChanged;

        protected void OnPropertyChanged(
            [CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}
