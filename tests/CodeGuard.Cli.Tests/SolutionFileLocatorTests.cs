using CodeGuard.Cli.Support;

namespace CodeGuard.Cli.Tests;

public sealed class SolutionFileLocatorTests : IDisposable
{
    private readonly string _repoDir = Directory.CreateTempSubdirectory("codeguard-solutionlocator-").FullName;

    [Fact]
    public void Resolve_WithNoExplicitPaths_DiscoversSlnFilesRecursively()
    {
        var nested = Directory.CreateDirectory(Path.Combine(_repoDir, "src", "Nested"));
        var slnPath = Path.Combine(nested.FullName, "Foo.sln");
        File.WriteAllText(slnPath, "");

        var resolved = SolutionFileLocator.Resolve(_repoDir, []);

        Assert.Equal([slnPath], resolved);
    }

    [Fact]
    public void Resolve_WithNoExplicitPaths_DiscoversSlnxFilesRecursively()
    {
        var nested = Directory.CreateDirectory(Path.Combine(_repoDir, "src", "Nested"));
        var slnxPath = Path.Combine(nested.FullName, "Foo.slnx");
        File.WriteAllText(slnxPath, "");

        var resolved = SolutionFileLocator.Resolve(_repoDir, []);

        Assert.Equal([slnxPath], resolved);
    }

    [Fact]
    public void Resolve_WithNoExplicitPaths_AndMultipleSolutionsDiscovered_ThrowsAndListsAllOfThem()
    {
        var slnPath = Path.Combine(_repoDir, "Root.sln");
        File.WriteAllText(slnPath, "");

        var nested = Directory.CreateDirectory(Path.Combine(_repoDir, "nested"));
        var slnxPath = Path.Combine(nested.FullName, "Nested.slnx");
        File.WriteAllText(slnxPath, "");

        var ex = Assert.Throws<InvalidOperationException>(() => SolutionFileLocator.Resolve(_repoDir, []));

        Assert.Contains(slnPath, ex.Message);
        Assert.Contains(slnxPath, ex.Message);
        Assert.Contains("--solution", ex.Message);
    }

    [Fact]
    public void Resolve_WithNoExplicitPaths_AndThreeOrMoreSolutionsDiscovered_ListsEveryOneOfThem()
    {
        var paths = Enumerable.Range(1, 3)
            .Select(i =>
            {
                var dir = Directory.CreateDirectory(Path.Combine(_repoDir, $"proj{i}"));
                var path = Path.Combine(dir.FullName, $"Proj{i}.sln");
                File.WriteAllText(path, "");
                return path;
            })
            .ToList();

        var ex = Assert.Throws<InvalidOperationException>(() => SolutionFileLocator.Resolve(_repoDir, []));

        Assert.All(paths, path => Assert.Contains(path, ex.Message));
    }

    [Fact]
    public void Resolve_SkipsExcludedDirectories_ForSlnxToo()
    {
        var binDir = Directory.CreateDirectory(Path.Combine(_repoDir, "bin"));
        File.WriteAllText(Path.Combine(binDir.FullName, "ShouldBeSkipped.slnx"), "");

        var objDir = Directory.CreateDirectory(Path.Combine(_repoDir, "obj"));
        File.WriteAllText(Path.Combine(objDir.FullName, "ShouldBeSkipped.sln"), "");

        var includedPath = Path.Combine(_repoDir, "Included.slnx");
        File.WriteAllText(includedPath, "");

        var resolved = SolutionFileLocator.Resolve(_repoDir, []);

        Assert.Equal([includedPath], resolved);
    }

    [Fact]
    public void Resolve_SkipsClaudeDirectory_WhichMayContainWorktreeCheckouts()
    {
        var claudeWorktreeDir = Directory.CreateDirectory(Path.Combine(_repoDir, ".claude", "worktrees", "some-branch"));
        File.WriteAllText(Path.Combine(claudeWorktreeDir.FullName, "ShouldBeSkipped.sln"), "");

        var includedPath = Path.Combine(_repoDir, "Included.sln");
        File.WriteAllText(includedPath, "");

        var resolved = SolutionFileLocator.Resolve(_repoDir, []);

        Assert.Equal([includedPath], resolved);
    }

    [Fact]
    public void Resolve_WithExplicitSlnxPath_ReturnsResolvedFullPath()
    {
        var slnxPath = Path.Combine(_repoDir, "Explicit.slnx");
        File.WriteAllText(slnxPath, "");

        var resolved = SolutionFileLocator.Resolve(_repoDir, ["Explicit.slnx"]);

        Assert.Equal([slnxPath], resolved);
    }

    [Fact]
    public void Resolve_WithExplicitMissingSlnxPath_ThrowsFileNotFoundException()
    {
        var ex = Assert.Throws<FileNotFoundException>(
            () => SolutionFileLocator.Resolve(_repoDir, ["Missing.slnx"]));

        Assert.Contains("Missing.slnx", ex.Message);
    }

    [Fact]
    public void Resolve_WithNoSolutionFilesOfEitherKind_ThrowsInvalidOperationException()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => SolutionFileLocator.Resolve(_repoDir, []));

        Assert.Contains(".sln", ex.Message);
        Assert.Contains(".slnx", ex.Message);
    }

    public void Dispose()
    {
        Directory.Delete(_repoDir, recursive: true);
    }
}
