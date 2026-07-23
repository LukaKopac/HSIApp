namespace HSIApp;

using System.Globalization;
using System.IO;

public static class HsiLoader
{
    public static HsiMetadata ReadHeader(string rawPath)
    {
        string hdrPath = Path.ChangeExtension(rawPath, ".hdr");

        string[] lines = File.ReadAllLines(hdrPath);

        Dictionary<string, string> header = new();
        List<double> wavelengths = new();

        bool readingMetadata = true;

        foreach (string line in lines)
        {
            string text = line.Trim();
            
            if (readingMetadata)
            {
                if (text.StartsWith("wavelength ="))
                {
                    readingMetadata = false;
                    continue;
                }

                if (!text.Contains('='))
                    continue;

                string[] parts = text.Split('=', 2); // split line by '='

                string key = parts[0].Trim().ToLower(); // first part = key (trim and to lower)
                string value = parts[1].Trim(); // second part = value (trim)

                header[key] = value; // add key-value pairs to dictionary

                continue;
            }

            // Now reading wavelengths until closing brace

            if (text == "}")
                break;

            double wavelength = double.Parse(text.Trim().TrimEnd(','), CultureInfo.InvariantCulture);

            wavelengths.Add(wavelength);
        }

        return new HsiMetadata
        {
            Samples = int.Parse(header["samples"]),
            Lines = int.Parse(header["lines"]),
            Bands = int.Parse(header["bands"]),
            Interleave = header["interleave"],
            DataType = int.Parse(header["data type"]),
            Wavelengths = wavelengths.ToArray(),
            Extra = header
        };
    }


    public static HsiCube ReadCube(string rawPath, HsiMetadata metadata)
    {

        using FileStream stream = File.OpenRead(rawPath);
        using BinaryReader reader = new(stream);

        // hard-code BIL
        // hard-code data type = 12  (UInt16)
        // hard-code division by 10000

        int samples = metadata.Samples;
        int lines = metadata.Lines;
        int bands = metadata.Bands;

        float[,,] cube = new float[lines, samples, bands];

        for (int y = 0; y < lines; y++)
        {
            for (int b = 0; b < bands; b++)
            {
                for (int x = 0; x < samples; x++)
                {
                    cube[y, x, b] = reader.ReadUInt16() / 10000f;
                }
            }
        }

        return new HsiCube
        {
            Data = cube
        };

    }

    public static HsiCube Load(string rawPath)
    {
        HsiMetadata metadata = ReadHeader(rawPath);

        HsiCube cube = ReadCube(rawPath, metadata);

        cube.Metadata = metadata;

        return cube;
    }
}