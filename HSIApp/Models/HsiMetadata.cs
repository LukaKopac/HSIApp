using HSIApp.Models;

namespace HSIApp;

public class HsiMetadata
{
    public int Samples { get; init; }

    public int Lines { get; init; }

    public int Bands { get; init; }

    public string Interleave { get; init; } = null!;

    public int DataType { get; init; }

    public CubeDataKind DataKind { get; init; } = CubeDataKind.Unknown;

    public double[] Wavelengths { get; init; } = null!;

    public Dictionary<string, string> Extra { get; init; } = new();


    public void PrintInfo()
    {
        Console.WriteLine("Metadata");
        Console.WriteLine("--------");

        Console.WriteLine($"Samples: {Samples}");
        Console.WriteLine($"Lines: {Lines}");
        Console.WriteLine($"Bands: {Bands}");
        Console.WriteLine($"Interleave: {Interleave}");
        Console.WriteLine($"Data type: {DataType}");

        Console.WriteLine();

        Console.WriteLine($"Wavelengths: {Wavelengths.Length}");
        Console.WriteLine($"Min: {Wavelengths.Min()} nm");
        Console.WriteLine($"Max: {Wavelengths.Max()} nm");

        if (Extra.Count > 0)
        {
            foreach (var pair in Extra)
            {
                Console.WriteLine($"{pair.Key}: {pair.Value}");
            }
        }
    }
}