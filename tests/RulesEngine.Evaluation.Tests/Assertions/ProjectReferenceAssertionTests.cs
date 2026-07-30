using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

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
    }

    [Fact]
    public void MustReferenceProject_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustReferenceProjectAssertion("*.Domain").Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
    }

    [Fact]
    public void MustNotReferenceProject_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustNotReferenceProjectAssertion("*.Infrastructure").Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
    }
}
