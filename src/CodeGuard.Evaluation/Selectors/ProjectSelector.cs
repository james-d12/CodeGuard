using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Selectors;

public sealed class ProjectSelector(string namePattern) : ITargetSelector
{
    public string Kind => "project";

    public IEnumerable<object> SelectCandidates(RepositoryModel model) =>
        model.Solutions
            .SelectMany(solution => solution.Projects)
            .Where(project => GlobMatcher.IsMatch(project.Name, namePattern));
}
