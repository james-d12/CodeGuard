using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

public class ProjectReferenceAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    [Fact]
    public void MustReferenceProject_Passes_WhenReferencePresent()
    {
        var project = TestModels.Project("Contoso.Application", projectReferences: ["Contoso.Domain"]);
        var outcome = new MustReferenceProjectAssertion("*.Domain").Evaluate(project, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void MustReferenceProject_Fails_WhenReferenceMissing()
    {
        var project = TestModels.Project("Contoso.Application");
        var outcome = new MustReferenceProjectAssertion("*.Domain").Evaluate(project, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("Project 'Contoso.Application' must reference a project matching '*.Domain'.", outcome.Message);
    }

    [Fact]
    public void MustNotReferenceProject_Passes_WhenReferenceAbsent()
    {
        var project = TestModels.Project("Contoso.Domain");
        var outcome = new MustNotReferenceProjectAssertion("*.Infrastructure").Evaluate(project, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void MustNotReferenceProject_Fails_WhenReferencePresent()
    {
        var project = TestModels.Project("Contoso.Domain", projectReferences: ["Contoso.Infrastructure"]);
        var outcome = new MustNotReferenceProjectAssertion("*.Infrastructure").Evaluate(project, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("Project 'Contoso.Domain' must not reference project 'Contoso.Infrastructure'.", outcome.Message);
    }

    [Fact]
    public void MustReferenceProject_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustReferenceProjectAssertion("*.Domain").Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("'must_reference_project' can only be evaluated against projects.", outcome.Message);
    }

    [Fact]
    public void MustNotReferenceProject_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustNotReferenceProjectAssertion("*.Infrastructure").Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
        Assert.Equal("'must_not_reference_project' can only be evaluated against projects.", outcome.Message);
    }
}
