namespace HSIApp.Models
{
    public sealed record PredictionRequest(
        string CubePath,
        string ModelPackagePath,
        string OutputDirectory);
}
