namespace CodeGuard.Analyzers.MSBuild;

public sealed class SolutionLoadException(string solutionPath, Exception innerException)
    : Exception($"Failed to open solution '{solutionPath}': {innerException.Message}", innerException);
