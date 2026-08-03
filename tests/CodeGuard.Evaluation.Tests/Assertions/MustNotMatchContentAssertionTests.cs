using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

public sealed class MustNotMatchContentAssertionTests : IDisposable
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"rulesengine-content-{Guid.NewGuid():N}.txt");

    private FileModel WriteFile(string content)
    {
        File.WriteAllText(_path, content);
        return new FileModel(_path, Path.GetFileName(_path), ".txt");
    }

    [Fact]
    public void Evaluate_Passes_WhenContentDoesNotMatchPattern()
    {
        var file = WriteFile("Console.WriteLine(\"ok\");");
        var outcome = new MustNotMatchContentAssertion(@"\.Result\b").Evaluate(file, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenContentMatchesPattern()
    {
        var file = WriteFile("var value = task.Result;");
        var outcome = new MustNotMatchContentAssertion(@"\.Result\b").Evaluate(file, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal($"File '{Path.GetFileName(_path)}' must not contain content matching '\\.Result\\b'.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustNotMatchContentAssertion(@"\.Result\b").Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("'must_not_match_content' can only be evaluated against files.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_UsingVirtualContent_WithNoBackingDiskFile()
    {
        var file = new FileModel("virtual/Order.cs", "Order.cs", ".cs", "var value = task.Result;");
        var outcome = new MustNotMatchContentAssertion(@"\.Result\b").Evaluate(file, EmptyModel);
        Assert.False(outcome.Passed);
    }

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }
}
