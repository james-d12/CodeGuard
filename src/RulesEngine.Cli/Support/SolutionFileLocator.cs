namespace RulesEngine.Cli.Support;

/// <summary>
/// Discovers .sln files under a repository root for the validate command, recursively so the
/// solution doesn't need to sit at the repo root, skipping build/tooling directories.
/// </summary>
public static class SolutionFileLocator
{
    private static readonly HashSet<string> ExcludedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", ".vs", ".idea", "node_modules"
    };

    public static IReadOnlyList<string> Resolve(string repoRoot, IReadOnlyList<string> explicitSolutionPaths)
    {
        if (explicitSolutionPaths.Count > 0)
        {
            var resolved = explicitSolutionPaths.Select(p => Path.GetFullPath(p, repoRoot)).ToList();
            var missing = resolved.Where(p => !File.Exists(p)).ToList();
            if (missing.Count > 0)
            {
                throw new FileNotFoundException(
                    "Solution file(s) not found: " + string.Join(", ", missing));
            }

            return resolved;
        }

        var candidates = FindSolutionFiles(repoRoot, repoRoot).ToList();
        if (candidates.Count == 0)
        {
            throw new InvalidOperationException($"No .sln file found under '{repoRoot}'.");
        }

        return candidates;
    }

    private static IEnumerable<string> FindSolutionFiles(string rootPath, string directoryPath)
    {
        foreach (var file in Directory.EnumerateFiles(directoryPath, "*.sln"))
        {
            yield return file;
        }

        foreach (var subdirectory in Directory.EnumerateDirectories(directoryPath))
        {
            if (ExcludedDirectoryNames.Contains(Path.GetFileName(subdirectory)))
            {
                continue;
            }

            foreach (var file in FindSolutionFiles(rootPath, subdirectory))
            {
                yield return file;
            }
        }
    }
}
