namespace HSIApp.Models
{
    public class ModelManifest
    {
        // Let us safely evolve the manifest format later.
        public int FormatVersion { get; init; } = 1;

        // Stable technical identifier, e.g. "apple-bruising-v1".
        public string ModelId { get; init; } = string.Empty;

        // User-facing information for the future MODELS panel.
        public string Name { get; init; } = string.Empty;
        public string Version { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public string Author { get; init; } = string.Empty;

        // The file expected beside manifest.json in the model package.
        public string ModelFile { get; init; } = "model.joblib";

        // Compatibility requirements.
        public CubeDataKind RequiredDataKind { get; init; }
            = CubeDataKind.Reflectance;

        public int ExpectedBandCount { get; init; }

        public double[] ExpectedWavelengthsNm { get; init; }
            = Array.Empty<double>();

        public double WavelengthToleranceNm { get; init; } = 1.0;

        // A model should normally include preprocessing inside its sklearn Pipeline.
        public bool PipelineIncludesPreprocessing { get; init; } = true;

        public string PythonVersion { get; init; } = string.Empty;
        public string ScikitLearnVersion { get; init; } = string.Empty;

        // Plain-language notes for users and researchers.
        public string Requirements { get; init; } = string.Empty;
        public string TrainingSummary { get; init; } = string.Empty;
    }
}
