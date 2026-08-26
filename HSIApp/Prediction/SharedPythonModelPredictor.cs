using HSIApp.IO;
using HSIApp.Models;
using System.Diagnostics;
using System.IO;

namespace HSIApp.Prediction;

public sealed class SharedPythonModelPredictor
{
    private readonly string pythonExecutablePath;

    public SharedPythonModelPredictor(string pythonExecutablePath)
    {
        this.pythonExecutablePath = Path.GetFullPath(
            pythonExecutablePath);
    }

    public async Task<PredictionResult> PredictAsync(
        PredictionRequest request,
        CancellationToken cancellationToken = default)
    {
        string cubePath = Path.GetFullPath(request.CubePath);
        string packagePath = Path.GetFullPath(request.ModelPackagePath);
        string outputDirectory = Path.GetFullPath(request.OutputDirectory);

        if (!File.Exists(cubePath))
        {
            throw new FileNotFoundException(
                "Cube file was not found.",
                cubePath);
        }

        if (!File.Exists(pythonExecutablePath))
        {
            throw new FileNotFoundException(
                "Configured Python executable was not found.",
                pythonExecutablePath);
        }

        ModelManifest manifest = ModelManifestLoader.Load(packagePath);
        HsiMetadata metadata = HsiLoader.ReadHeader(cubePath);

        ModelCompatibilityResult compatibility =
            ModelCompatibilityValidator.Validate(manifest, metadata);

        if (!compatibility.IsCompatible)
        {
            throw new InvalidDataException(
                "The selected cube is incompatible with this model:\n- " +
                string.Join("\n- ", compatibility.Issues));
        }

        string runnerPath = Path.Combine(
            AppContext.BaseDirectory,
            "PythonBridge",
            "predict.py");

        if (!File.Exists(runnerPath))
        {
            throw new FileNotFoundException(
                "The shared Python prediction runner was not found.",
                runnerPath);
        }

        Directory.CreateDirectory(outputDirectory);

        string predictionPath = Path.Combine(
            outputDirectory,
            "prediction.npy");

        ProcessStartInfo startInfo = new()
        {
            FileName = pythonExecutablePath,
            WorkingDirectory = outputDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add(runnerPath);
        startInfo.ArgumentList.Add("--cube");
        startInfo.ArgumentList.Add(cubePath);
        startInfo.ArgumentList.Add("--model-package");
        startInfo.ArgumentList.Add(packagePath);
        startInfo.ArgumentList.Add("--output");
        startInfo.ArgumentList.Add(predictionPath);

        using Process process = new()
        {
            StartInfo = startInfo
        };

        process.Start();

        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync(cancellationToken);

        string output = await outputTask;
        string error = await errorTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Prediction failed (exit code {process.ExitCode}).\n{error}");
        }

        if (!File.Exists(predictionPath))
        {
            throw new InvalidDataException(
                "Prediction runner completed without producing prediction.npy.");
        }

        return new PredictionResult(predictionPath, output);
    }
}
