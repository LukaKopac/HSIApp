using HSIApp;
using HSIApp.IO;
using HSIApp.Models;

return args.Length == 0
    ? PrintUsage()
    : RunCommand(args);

static int RunCommand(string[] args)
{
    try
    {
        return args[0].ToLowerInvariant() switch
        {
            "inspect-cube" when args.Length == 2 => InspectCube(args[1]),
            "inspect-model" when args.Length == 2 => InspectModel(args[1]),
            "validate-model" when args.Length == 3 =>
                ValidateModel(args[1], args[2]),
            "help" or "--help" or "-h" => PrintUsage(),
            _ => PrintUsage("Unknown command or incorrect number of arguments.")
        };
    }
    catch (Exception exception)
    {
        Console.Error.WriteLine($"Error: {exception.Message}");
        return 1;
    }
}

static int InspectCube(string rawPath)
{
    HsiMetadata metadata = HsiLoader.ReadHeader(rawPath);

    Console.WriteLine($"Cube: {Path.GetFullPath(rawPath)}");
    Console.WriteLine($"Shape: {metadata.Lines} x {metadata.Samples} x {metadata.Bands} (lines x samples x bands)");
    Console.WriteLine($"Data kind: {metadata.DataKind}");
    Console.WriteLine($"Layout: {metadata.Interleave}, ENVI data type {metadata.DataType}");

    if (metadata.Wavelengths.Length > 0)
    {
        Console.WriteLine($"Wavelength range: {metadata.Wavelengths.Min():F2}–{metadata.Wavelengths.Max():F2} nm");
    }

    return 0;
}

static int InspectModel(string packageFolderPath)
{
    ModelManifest manifest = ModelManifestLoader.Load(packageFolderPath);

    Console.WriteLine($"Model: {manifest.Name} ({manifest.Version})");
    Console.WriteLine($"ID: {manifest.ModelId}");
    Console.WriteLine($"Model file: {manifest.ModelFile}");
    Console.WriteLine($"Requires: {manifest.RequiredDataKind}, {manifest.ExpectedBandCount} bands");
    Console.WriteLine($"Wavelength tolerance: {manifest.WavelengthToleranceNm:F2} nm");
    return 0;
}

static int ValidateModel(string rawPath, string packageFolderPath)
{
    HsiMetadata metadata = HsiLoader.ReadHeader(rawPath);
    ModelManifest manifest = ModelManifestLoader.Load(packageFolderPath);
    ModelCompatibilityResult result = ModelCompatibilityValidator.Validate(manifest, metadata);

    if (result.IsCompatible)
    {
        Console.WriteLine("Compatible: the cube meets this model package's declared requirements.");
        return 0;
    }

    Console.Error.WriteLine("Incompatible:");
    foreach (string issue in result.Issues)
    {
        Console.Error.WriteLine($"- {issue}");
    }

    return 2;
}

static int PrintUsage(string? error = null)
{
    if (error is not null)
    {
        Console.Error.WriteLine(error);
        Console.Error.WriteLine();
    }

    Console.WriteLine("HSI Playground - development tools for HSIApp");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  inspect-cube <cube.raw>");
    Console.WriteLine("  inspect-model <model-package-folder>");
    Console.WriteLine("  validate-model <cube.raw> <model-package-folder>");
    return error is null ? 0 : 1;
}
