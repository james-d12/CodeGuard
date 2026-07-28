using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustHaveMsBuildPropertyAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    private static ProjectModel ProjectWithProperty(string name, string value) => new(
        "Contoso.Domain", "Contoso.Domain.csproj", "net10.0", "Microsoft.NET.Sdk",
        [], [], new Dictionary<string, string> { [name] = value }, []);

    [Fact]
    public void Evaluate_Passes_WhenPropertyExistsWithNoValueConstraint()
    {
        var project = ProjectWithProperty("Nullable", "enable");
        var outcome = new MustHaveMsBuildPropertyAssertion("Nullable", null).Evaluate(project, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Passes_WhenPropertyMatchesExpectedValue()
    {
        var project = ProjectWithProperty("Nullable", "enable");
        var outcome = new MustHaveMsBuildPropertyAssertion("Nullable", "enable").Evaluate(project, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenPropertyMissing()
    {
        var project = ProjectWithProperty("Nullable", "enable");
        var outcome = new MustHaveMsBuildPropertyAssertion("TreatWarningsAsErrors", null).Evaluate(project, EmptyModel);
        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenPropertyValueDoesNotMatch()
    {
        var project = ProjectWithProperty("Nullable", "disable");
        var outcome = new MustHaveMsBuildPropertyAssertion("Nullable", "enable").Evaluate(project, EmptyModel);
        Assert.False(outcome.Passed);
    }
}
