using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public sealed class MustMatchContentAssertionTests : IDisposable
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"rulesengine-content-{Guid.NewGuid():N}.txt");

    private FileModel WriteFile(string content)
    {
        File.WriteAllText(_path, content);
        return new FileModel(_path, Path.GetFileName(_path), ".txt");
    }

    [Fact]
    public void Evaluate_Passes_WhenContentMatchesPattern()
    {
        var file = WriteFile("<Nullable>enable</Nullable>");
        var outcome = new MustMatchContentAssertion("<Nullable>enable</Nullable>").Evaluate(file, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenContentDoesNotMatchPattern()
    {
        var file = WriteFile("<Nullable>disable</Nullable>");
        var outcome = new MustMatchContentAssertion("<Nullable>enable</Nullable>").Evaluate(file, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal($"File '{Path.GetFileName(_path)}' must contain content matching '<Nullable>enable</Nullable>'.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustMatchContentAssertion("<Nullable>enable</Nullable>").Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("'must_match_content' can only be evaluated against files.", outcome.Message);
    }

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }
}
