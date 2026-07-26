using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Selectors;

namespace RulesEngine.Evaluation.Tests.Selectors;

public class ProjectSelectorTests
{
    [Fact]
    public void SelectCandidates_ReturnsOnlyProjectsMatchingNamePattern()
    {
        var model = TestModels.Repository(
            TestModels.Project("Contoso.Domain"),
            TestModels.Project("Contoso.Infrastructure"),
            TestModels.Project("Contoso.Application"));

        var candidates = new ProjectSelector("*.Domain").SelectCandidates(model).Cast<ProjectModel>().ToList();

        var project = Assert.Single(candidates);
        Assert.Equal("Contoso.Domain", project.Name);
    }
}
