using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Selectors;

namespace CodeGuard.Evaluation.Tests.Selectors;

public class DirectorySelectorTests
{
    private static DirectoryModel Dir(string relativePath) => new(relativePath, relativePath, System.IO.Path.GetFileName(relativePath));

    [Fact]
    public void SelectCandidates_FiltersByPathPattern_UnderDirectoryAtAnyDepth()
    {
        var model = TestModels.Repository() with
        {
            Directories = [Dir("src"), Dir("src/Domain"), Dir("src/Domain/Sub"), Dir("tests")]
        };

        var candidates = new DirectorySelector(pathPattern: "src/**").SelectCandidates(model).Cast<DirectoryModel>().ToList();

        Assert.Equal(["src/Domain", "src/Domain/Sub"], candidates.Select(d => d.RelativePath));
    }

    [Fact]
    public void SelectCandidates_FiltersByPathPattern_DirectChildrenOnly()
    {
        var model = TestModels.Repository() with
        {
            Directories = [Dir("src"), Dir("src/Domain"), Dir("src/Domain/Sub"), Dir("tests")]
        };

        var candidates = new DirectorySelector(pathPattern: "src/*").SelectCandidates(model).Cast<DirectoryModel>().ToList();

        Assert.Equal(["src/Domain"], candidates.Select(d => d.RelativePath));
    }

    [Fact]
    public void SelectCandidates_MatchesAllDirectories_WhenPatternIsBareWildcard()
    {
        var model = TestModels.Repository() with { Directories = [Dir("src"), Dir("tests")] };

        var candidates = new DirectorySelector().SelectCandidates(model).ToList();

        Assert.Equal(2, candidates.Count);
    }

    [Fact]
    public void SelectCandidates_ReturnsEmpty_WhenRepositoryHasNoDirectories()
    {
        var model = TestModels.Repository();

        var candidates = new DirectorySelector().SelectCandidates(model).ToList();

        Assert.Empty(candidates);
    }
}
