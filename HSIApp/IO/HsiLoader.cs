namespace HSIApp;

using HSIApp.Models;
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
            DataKind = DetermineDataKind(header),
            Wavelengths = wavelengths.ToArray(),
            Extra = header
        };
    }

    private static CubeDataKind DetermineDataKind(
        IReadOnlyDictionary<string, string> header)
    {
        if (!header.TryGetValue("description", out string? description))
            return CubeDataKind.Unknown;

        if (description.Contains(
                "[REFLECTANCE]",
                StringComparison.OrdinalIgnoreCase))
        {
            return CubeDataKind.Reflectance;
        }

        if (description.Contains(
                "[RAW]",
                StringComparison.OrdinalIgnoreCase))
        {
            return CubeDataKind.Raw;
        }

        return CubeDataKind.Unknown;
    }

    public static HsiCube ReadCube(string rawPath, HsiMetadata metadata)
    {
        ValidateSupportedCube(rawPath, metadata);

        using FileStream stream = File.OpenRead(rawPath);
        using BinaryReader reader = new(stream);

        // hard-code BIL
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
                    cube[y, x, b] = ReadPixelValue(reader, metadata.DataType);
                }
            }
        }

        return new HsiCube
        {
            Data = cube
        };

    }

    private static float ReadPixelValue(
        BinaryReader reader,
        int dataType)
    {
        const float scale = 10000f;

        return dataType switch
        {
            // ENVI Type 2: signed 16-bit integer
            2 => reader.ReadInt16() / scale,

            // ENVI Type 12: unsigned 16-bit integer
            12 => reader.ReadUInt16() / scale,

            _ => throw new NotSupportedException(
                $"Unsupported ENVI data type: {dataType}. " +
                "Currently supported: 2 (Int16) and 12 (UInt16).")
        };
    }

    public static HsiCube Load(string rawPath)
    {
        HsiMetadata metadata = ReadHeader(rawPath);

        HsiCube cube = ReadCube(rawPath, metadata);

        cube.Metadata = metadata;

        return cube;
    }

    private static void ValidateSupportedCube(
        string rawPath,
        HsiMetadata metadata)
    {
        if (!string.Equals(
                metadata.Interleave,
                "bil",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                $"Unsupported interleave '{metadata.Interleave}'. " +
                "Currently only BIL cubes are supported.");
        }

        if (metadata.DataType is not (2 or 12))
        {
            throw new NotSupportedException(
                $"Unsupported ENVI data type '{metadata.DataType}'. " +
                "Currently supported: 2 (Int16) and 12 (UInt16).");
        }

        if (metadata.Extra.TryGetValue(
                "byte order",
                out string? byteOrder) &&
                byteOrder.Trim() != "0")
        {
            throw new NotSupportedException(
                "Big-endian cubes are not currently supported.");
        }

        if (metadata.Extra.TryGetValue(
                "header offset",
                out string? headerOffset) &&
                headerOffset.Trim() != "0")
        {
            throw new NotSupportedException(
                "Cubes with a header offset are not currently supported.");
        }

        long expectedBytes = checked(
            (long)metadata.Lines *
            metadata.Samples *
            metadata.Bands *
            sizeof(ushort));

        long actualBytes = new FileInfo(rawPath).Length;

        if (actualBytes != expectedBytes)
        {
            throw new InvalidDataException(
                $"Cube file size does not match its header. " +
                $"Expected {expectedBytes:N0} bytes, found {actualBytes:N0} bytes.");
        }
    }
}