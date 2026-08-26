namespace HSIApp.Models;

public static class ModelCompatibilityValidator
{
    public static ModelCompatibilityResult Validate(
        ModelManifest manifest,
        HsiMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        ArgumentNullException.ThrowIfNull(metadata);

        List<string> issues = [];

        if (metadata.DataKind != manifest.RequiredDataKind)
        {
            issues.Add(
                $"The model requires {manifest.RequiredDataKind} data, " +
                $"but the cube is marked as {metadata.DataKind}.");
        }

        if (metadata.Bands != manifest.ExpectedBandCount)
        {
            issues.Add(
                $"The model requires {manifest.ExpectedBandCount} bands, " +
                $"but the cube contains {metadata.Bands}.");
        }

        if (metadata.Wavelengths.Length != manifest.ExpectedWavelengthsNm.Length)
        {
            issues.Add(
                $"The cube supplies {metadata.Wavelengths.Length} wavelengths, " +
                $"but the model declares {manifest.ExpectedWavelengthsNm.Length}.");
        }
        else
        {
            for (int band = 0; band < metadata.Wavelengths.Length; band++)
            {
                double difference = Math.Abs(
                    metadata.Wavelengths[band] -
                    manifest.ExpectedWavelengthsNm[band]);

                if (difference > manifest.WavelengthToleranceNm)
                {
                    issues.Add(
                        $"Band {band + 1} is {difference:F2} nm away from the " +
                        $"model's expected wavelength, exceeding the " +
                        $"{manifest.WavelengthToleranceNm:F2} nm tolerance.");
                }
            }
        }

        return new ModelCompatibilityResult(issues);
    }
}
