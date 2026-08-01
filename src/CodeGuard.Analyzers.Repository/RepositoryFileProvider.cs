using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Analysis.Providers;

namespace CodeGuard.Analyzers.Repository;

public sealed class RepositoryFileProvider : IAnalysisProvider
{
    private static readonly HashSet<string> ExcludedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", ".vs", ".idea", "node_modules"
    };

    public string Name => "Repository";

    public Task ContributeAsync(AnalysisModelBuilderContext context, CancellationToken cancellationToken)
    {
        foreach (var file in EnumerateFiles(context.RootPath, context.RootPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.AddFile(file);
        }

        context.AddDirectories(EnumerateDirectories(context.RootPath, context.RootPath));

        return Task.CompletedTask;
    }

    private static IEnumerable<FileModel> EnumerateFiles(string rootPath, string directoryPath)
    {
        foreach (var file in Directory.EnumerateFiles(directoryPath))
        {
            var relativePath = Path.GetRelativePath(rootPath, file);
            yield return new FileModel(file, relativePath, Path.GetExtension(file));
        }

        foreach (var subdirectory in Directory.EnumerateDirectories(directoryPath))
        {
            if (ExcludedDirectoryNames.Contains(Path.GetFileName(subdirectory)))
            {
                continue;
            }

            foreach (var file in EnumerateFiles(rootPath, subdirectory))
            {
                yield return file;
            }
        }
    }

    private static IEnumerable<string> EnumerateDirectories(string rootPath, string directoryPath)
    {
        foreach (var subdirectory in Directory.EnumerateDirectories(directoryPath))
        {
            if (ExcludedDirectoryNames.Contains(Path.GetFileName(subdirectory)))
            {
                continue;
            }

            yield return Path.GetRelativePath(rootPath, subdirectory);

            foreach (var nested in EnumerateDirectories(rootPath, subdirectory))
            {
                yield return nested;
            }
        }
    }
}
