using System;

namespace HSIApp;

public class HsiCube
{
    public float[,,] Data { get; set; } = null!;

    public HsiMetadata Metadata { get; set; } = null!;

    public int Height => Data.GetLength(0);

    public int Width => Data.GetLength(1);

    public int Bands => Data.GetLength(2);

    public float this[int y, int x, int b]
    {
        get => Data[y, x, b];
        set => Data[y, x, b] = value;
    }

    public float[] GetSpectrum(int y, int x)
    {
        float[] spectrum = new float[Bands];

        for (int b = 0; b < Bands; b++)
        {
            spectrum[b] = this[y, x, b];
        }

        return spectrum;
    }

    public float[] GetAverageSpectrum(int centerY, int centerX, int size)
    {
        if (size <= 0 || size % 2 == 0)
            throw new ArgumentException("Size must be a positive odd number.");

        int radius = size / 2;

        int minY = Math.Max(0, centerY - radius);
        int maxY = Math.Min(Height - 1, centerY + radius);

        int minX = Math.Max(0, centerX - radius);
        int maxX = Math.Min(Width - 1, centerX + radius);

        float[] spectrum = new float[Bands];
        int pixelCount = 0;

        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                for (int b = 0; b < Bands; b++)
                {
                    spectrum[b] += Data[y, x, b];
                }

                pixelCount++;
            }
        }

        for (int b = 0; b < Bands; b++)
        {
            spectrum[b] /= pixelCount;
        }

        return spectrum;
    }

    public float[,] GetBand(int band)
    {
        float[,] image = new float[Height, Width];

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                image[y, x] = Data[y, x, band];
            }
        }

        return image;
    }

    public void PrintShape()
    {
        Console.WriteLine($"{Height} × {Width} × {Bands}");
    }
}
