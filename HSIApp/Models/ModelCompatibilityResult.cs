namespace HSIApp.Models;

public sealed class ModelCompatibilityResult
{
    public IReadOnlyList<string> Issues { get; }

    public bool IsCompatible => Issues.Count == 0;

    public ModelCompatibilityResult(IReadOnlyList<string> issues)
    {
        Issues = issues;
    }
}
