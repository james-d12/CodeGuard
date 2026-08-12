using CodeGuard.Evaluation.Selectors;

namespace CodeGuard.Evaluation.Tests.Selectors;

public class DirectorySelectorTests
{
    [Fact]
    public void SelectCandidates_FiltersByPathPattern()
    {
        var model = TestModels.Repository() with { Directories = ["src", "src/Domain", "tests"] };

        var candidates = new DirectorySelector(pathPattern: "src*").SelectCandidates(model).Cast<string>().ToList();

        Assert.Equal(["src", "src/Domain"], candidates);
    }

    [Fact]
    public void SelectCandidates_MatchesAllDirectories_WhenPatternIsBareWildcard()
    {
        var model = TestModels.Repository() with { Directories = ["src", "tests"] };

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
