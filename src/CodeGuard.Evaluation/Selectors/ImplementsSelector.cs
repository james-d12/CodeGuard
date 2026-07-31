using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Selectors;

public sealed class ImplementsSelector(string interfacePattern) : ITargetSelector
{
    public string Kind => "implements";

    public IEnumerable<object> SelectCandidates(RepositoryModel model) =>
        model.Solutions
            .SelectMany(solution => solution.Projects)
            .SelectMany(project => project.Types)
            .Where(type => type.Interfaces.Any(i => GlobMatcher.IsMatch(i, interfacePattern)));
}
