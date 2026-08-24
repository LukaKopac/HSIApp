using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace HSIApp.Models
{
    public class LoadedCube
    {
        public Guid Id { get; } = Guid.NewGuid();

        public string SourcePath { get; init; } = string.Empty;

        public HsiCube Cube { get; init; } = null;

        public string DisplayName => Path.GetFileName(SourcePath);

        public int Width => Cube.Width;

        public int Height => Cube.Height;

        public int BandCount => Cube.Bands;

        public string ShapeDescription =>
            $"{Width} × {Height} × {BandCount}";

        public string WavelengthDescription
        {
            get
            {
                double[] wavelengths = Cube.Metadata.Wavelengths;

                if (wavelengths.Length == 0)
                    return "No wavelength information";

                return
                    $"{wavelengths.Length} bands, " +
                    $"{wavelengths.Min():F1}-{wavelengths.Max():F1} nm";
            }
        }

        public IReadOnlyDictionary<string, string> HeaderValues =>
            Cube.Metadata.Extra;
    }
}
