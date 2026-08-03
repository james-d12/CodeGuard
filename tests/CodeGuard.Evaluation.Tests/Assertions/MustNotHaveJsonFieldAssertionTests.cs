using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

public sealed class MustNotHaveJsonFieldAssertionTests : IDisposable
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"rulesengine-json-{Guid.NewGuid():N}.json");

    private FileModel WriteFile(string json)
    {
        File.WriteAllText(_path, json);
        return new FileModel(_path, Path.GetFileName(_path), ".json");
    }

    [Fact]
    public void Evaluate_Passes_WhenFieldDoesNotExist()
    {
        var file = WriteFile("""{ "profiles": {} }""");
        var outcome = new MustNotHaveJsonFieldAssertion("profiles.http.applicationUrl", null).Evaluate(file, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenFieldExists()
    {
        var file = WriteFile("""{ "profiles": { "http": { "applicationUrl": "http://localhost:5000" } } }""");
        var outcome = new MustNotHaveJsonFieldAssertion("profiles.http.applicationUrl", null).Evaluate(file, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal($"File '{Path.GetFileName(_path)}' must not have JSON field 'profiles.http.applicationUrl'.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Passes_WhenFieldExists_ButValueDiffersFromForbiddenValue()
    {
        var file = WriteFile("""{ "profiles": { "http": { "applicationUrl": "http://localhost:6000" } } }""");
        var outcome = new MustNotHaveJsonFieldAssertion("profiles.http.applicationUrl", "http://localhost:5000").Evaluate(file, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenFieldExists_AndMatchesForbiddenValue()
    {
        var file = WriteFile("""{ "profiles": { "http": { "applicationUrl": "http://localhost:5000" } } }""");
        var outcome = new MustNotHaveJsonFieldAssertion("profiles.http.applicationUrl", "http://localhost:5000").Evaluate(file, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal(
            $"File '{Path.GetFileName(_path)}' must not have JSON field 'profiles.http.applicationUrl' equal to 'http://localhost:5000'.",
            outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustNotHaveJsonFieldAssertion("profiles.http.applicationUrl", null).Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("'must_not_have_json_field' can only be evaluated against files.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Passes_UsingVirtualContent_WithNoBackingDiskFile()
    {
        var file = new FileModel("virtual/appsettings.json", "appsettings.json", ".json", """{ "profiles": {} }""");
        var outcome = new MustNotHaveJsonFieldAssertion("profiles.http.applicationUrl", null).Evaluate(file, EmptyModel);
        Assert.True(outcome.Passed);
    }

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }
}
