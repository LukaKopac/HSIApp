using HSIApp.Models;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace HSIApp.IO
{
    public static class ModelManifestLoader
    {

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

        public static ModelManifest Load(string packageFolderPath)
        {
            string fullPackagePath = Path.GetFullPath(packageFolderPath);

            if (!Directory.Exists(fullPackagePath))
            {
                throw new DirectoryNotFoundException(
                    $"Model package folder was not found: {fullPackagePath}");
            }

            string manifestPath = Path.Combine(
                fullPackagePath,
                "manifest.json");

            if (!File.Exists(manifestPath))
            {
                throw new FileNotFoundException(
                    "The model package does not contain manifest.json.",
                    manifestPath);
            }

            string json = File.ReadAllText(manifestPath);

            ModelManifest manifest =
                JsonSerializer.Deserialize<ModelManifest>(
                    json,
                    JsonOptions)
                ?? throw new InvalidDataException(
                    "manifest.json is empty or invalid.");

            Validate(manifest, fullPackagePath);

            return manifest;
        }

        private static void Validate(
            ModelManifest manifest,
            string packageFolderPath)
        {
            if (manifest.FormatVersion != 1)
            {
                throw new NotSupportedException(
                    $"Unsupported manifest format version: " +
                    $"{manifest.FormatVersion}.");
            }

            if (string.IsNullOrWhiteSpace(manifest.ModelId))
            {
                throw new InvalidDataException(
                    "manifest.json requires a modelId");
            }

            if (string.IsNullOrWhiteSpace(manifest.Name))
            {
                throw new InvalidDataException(
                    "manifest.json requires a name.");
            }

            if (manifest.RequiredDataKind == CubeDataKind.Unknown)
            {
                throw new InvalidDataException(
                    "manifest.json must specify requiredDataKind.");
            }

            if (manifest.ExpectedBandCount <= 0)
            {
                throw new InvalidDataException(
                    "expectedBandCount must be greater than zero.");
            }

            if (manifest.ExpectedWavelengthsNm.Length !=
                manifest.ExpectedBandCount)
            {
                throw new InvalidDataException(
                    "expectedWavelengthsNm must contain exactly " +
                    "expectedBandCount values.");
            }

            if (manifest.WavelengthToleranceNm <= 0)
            {
                throw new InvalidDataException(
                    "wavelengthToleranceNm must be greater than zero.");
            }

            ValidateModelFilePath(
                packageFolderPath,
                manifest.ModelFile);
        }

        private static void ValidateModelFilePath(
            string packageFolderPath,
            string modelFile)
        {
            if (string.IsNullOrWhiteSpace(modelFile) ||
                Path.IsPathRooted(modelFile))
            {
                throw new InvalidDataException(
                    "modelFile must be a relative path inside the model package.");
            }

            string modelPath = Path.GetFullPath(
                Path.Combine(packageFolderPath, modelFile));

            string relativePath = Path.GetRelativePath(
                packageFolderPath,
                modelPath);

            if (relativePath == ".." ||
                relativePath.StartsWith(
                    $"..{Path.DirectorySeparatorChar}",
                    StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    "modelFile must stay inside the model package folder.");
            }

            if (!File.Exists(modelPath))
            {
                throw new FileNotFoundException(
                    $"The declared model file was not found: {modelFile}",
                    modelPath);
            }
        }

    }
}
