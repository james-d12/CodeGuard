using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustHaveJsonFieldAssertionTests : IDisposable
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"rulesengine-json-{Guid.NewGuid():N}.json");

    private FileModel WriteFile(string json)
    {
        File.WriteAllText(_path, json);
        return new FileModel(_path, Path.GetFileName(_path), ".json");
    }

    [Fact]
    public void Evaluate_Passes_WhenFieldExistsWithNoValueConstraint()
    {
        var file = WriteFile("""{ "profiles": { "http": { "applicationUrl": "http://localhost:5000" } } }""");
        var outcome = new MustHaveJsonFieldAssertion("profiles.http.applicationUrl", null).Evaluate(file, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Passes_WhenFieldMatchesExpectedValue()
    {
        var file = WriteFile("""{ "profiles": { "http": { "applicationUrl": "http://localhost:5000" } } }""");
        var outcome = new MustHaveJsonFieldAssertion("profiles.http.applicationUrl", "http://localhost:5000").Evaluate(file, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenFieldMissing()
    {
        var file = WriteFile("""{ "profiles": {} }""");
        var outcome = new MustHaveJsonFieldAssertion("profiles.http.applicationUrl", null).Evaluate(file, EmptyModel);
        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenFieldValueDoesNotMatch()
    {
        var file = WriteFile("""{ "profiles": { "http": { "applicationUrl": "http://localhost:6000" } } }""");
        var outcome = new MustHaveJsonFieldAssertion("profiles.http.applicationUrl", "http://localhost:5000").Evaluate(file, EmptyModel);
        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenOnlyLeafSegmentMissing()
    {
        var file = WriteFile("""{ "profiles": { "http": {} } }""");
        var outcome = new MustHaveJsonFieldAssertion("profiles.http.applicationUrl", null).Evaluate(file, EmptyModel);
        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustHaveJsonFieldAssertion("profiles.http.applicationUrl", null).Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
    }

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }
}
