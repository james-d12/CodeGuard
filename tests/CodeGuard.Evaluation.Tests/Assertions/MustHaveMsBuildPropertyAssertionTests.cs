using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

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
        Assert.Equal("Project 'Contoso.Domain' must define MSBuild property 'TreatWarningsAsErrors'.", outcome.Message);
    }

    [Fact]
    public void Evaluate_Fails_WhenPropertyValueDoesNotMatch()
    {
        var project = ProjectWithProperty("Nullable", "disable");
        var outcome = new MustHaveMsBuildPropertyAssertion("Nullable", "enable").Evaluate(project, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("Project 'Contoso.Domain' must have MSBuild property 'Nullable' set to 'enable' (found 'disable').", outcome.Message);
    }

    [Fact]
    public void Evaluate_Passes_WhenPropertyValueMatches_IgnoringCase()
    {
        var project = ProjectWithProperty("Nullable", "Enable");
        var outcome = new MustHaveMsBuildPropertyAssertion("Nullable", "enable").Evaluate(project, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustHaveMsBuildPropertyAssertion("Nullable", null).Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("'must_have_msbuild_property' can only be evaluated against projects.", outcome.Message);
    }
}
