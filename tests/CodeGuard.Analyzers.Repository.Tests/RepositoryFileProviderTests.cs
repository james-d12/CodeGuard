using CodeGuard.Analysis.Providers;

namespace CodeGuard.Analyzers.Repository.Tests;

public class RepositoryFileProviderTests
{
    [Fact]
    public async Task ContributeAsync_AddsFilesAndSkipsExcludedDirectories()
    {
        var root = Directory.CreateTempSubdirectory("codeguard-repo-provider-tests-");

        try
        {
            await File.WriteAllTextAsync(Path.Combine(root.FullName, "readme.md"), "hello");

            var srcDir = Directory.CreateDirectory(Path.Combine(root.FullName, "src"));
            await File.WriteAllTextAsync(Path.Combine(srcDir.FullName, "Program.cs"), "// program");

            var binDir = Directory.CreateDirectory(Path.Combine(root.FullName, "bin"));
            await File.WriteAllTextAsync(Path.Combine(binDir.FullName, "output.dll"), "binary");

            var gitDir = Directory.CreateDirectory(Path.Combine(root.FullName, ".git"));
            await File.WriteAllTextAsync(Path.Combine(gitDir.FullName, "HEAD"), "ref: refs/heads/main");

            var context = new AnalysisModelBuilderContext(root.FullName);
            await new RepositoryFileProvider().ContributeAsync(context, CancellationToken.None);

            var model = context.Build();

            Assert.Contains(model.Files, f => f.RelativePath == "readme.md" && f.Extension == ".md");
            Assert.Contains(model.Files, f => f.RelativePath == Path.Combine("src", "Program.cs") && f.Extension == ".cs");
            Assert.DoesNotContain(model.Files, f => f.RelativePath.StartsWith("bin", StringComparison.Ordinal));
            Assert.DoesNotContain(model.Files, f => f.RelativePath.StartsWith(".git", StringComparison.Ordinal));
            Assert.Equal(2, model.Files.Count);
        }
        finally
        {
            root.Delete(recursive: true);
        }
    }
}
