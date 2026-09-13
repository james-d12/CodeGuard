using Microsoft.Extensions.Logging;

namespace CodeGuard.Cli.Support;

/// <summary>
/// Discovers .sln/.slnx files under a repository root for the validate command, recursively so the
/// solution doesn't need to sit at the repo root, skipping build/tooling directories. Auto-discovery
/// (no explicit solution paths) only succeeds unattended when exactly one is found - if more than one
/// turns up, that's ambiguous (a genuine multi-solution repo vs. --path pointed at a parent directory
/// containing several unrelated repos, which look identical from here) so Resolve throws and lists
/// what it found rather than silently analyzing all of them together.
/// </summary>
public static class SolutionFileLocator
{
    private static readonly HashSet<string> ExcludedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", ".vs", ".idea", "node_modules",
        // .claude may contain full git worktree checkouts of this same repo (see
        // `.claude/worktrees/`) - without this, validate would discover the worktree's own copy
        // of every .sln alongside the real one, duplicating every project name and tripping any
        // analyzer that assumes (ProjectName, FullName) is unique across the analysis model.
        ".claude"
    };

    public static IReadOnlyList<string> Resolve(string repoRoot, IReadOnlyList<string> explicitSolutionPaths, ILogger? logger = null)
    {
        if (explicitSolutionPaths.Count > 0)
        {
            var resolved = explicitSolutionPaths.Select(p => Path.GetFullPath(p, repoRoot)).ToList();
            var missing = resolved.Where(p => !File.Exists(p)).ToList();
            if (missing.Count > 0)
            {
                logger?.LogError("Solution file(s) not found: {MissingFiles}", string.Join(", ", missing));
                throw new FileNotFoundException(
                    "Solution file(s) not found: " + string.Join(", ", missing));
            }

            logger?.LogDebug("Using {Count} explicitly specified solution file(s)", resolved.Count);
            return resolved;
        }

        var candidates = FindSolutionFiles(repoRoot).ToList();
        if (candidates.Count == 0)
        {
            logger?.LogError("No .sln or .slnx file found under {RepoRoot}", repoRoot);
            throw new InvalidOperationException($"No .sln or .slnx file found under '{repoRoot}'.");
        }

        if (candidates.Count > 1)
        {
            candidates.Sort(StringComparer.OrdinalIgnoreCase);
            var list = string.Join(Environment.NewLine, candidates.Select(c => "  " + c));
            logger?.LogError(
                "Multiple solution files found under {RepoRoot}: {Candidates}", repoRoot, string.Join(", ", candidates));
            throw new InvalidOperationException(
                $"Multiple solution files found under '{repoRoot}':{Environment.NewLine}{list}{Environment.NewLine}" +
                "Specify which to analyze with --solution <path> (repeatable).");
        }

        logger?.LogDebug("Discovered {Count} solution file(s) under {RepoRoot}", candidates.Count, repoRoot);
        return candidates;
    }

    private static IEnumerable<string> FindSolutionFiles(string directoryPath)
    {
        foreach (var file in Directory.EnumerateFiles(directoryPath, "*.sln"))
        {
            yield return file;
        }

        foreach (var file in Directory.EnumerateFiles(directoryPath, "*.slnx"))
        {
            yield return file;
        }

        foreach (var subdirectory in Directory.EnumerateDirectories(directoryPath))
        {
            if (ExcludedDirectoryNames.Contains(Path.GetFileName(subdirectory)))
            {
                continue;
            }

            foreach (var file in FindSolutionFiles(subdirectory))
            {
                yield return file;
            }
        }
    }
}
