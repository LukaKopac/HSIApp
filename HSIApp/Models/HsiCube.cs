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
