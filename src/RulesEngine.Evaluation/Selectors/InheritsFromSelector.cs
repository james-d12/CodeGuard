using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Selectors;

namespace RulesEngine.Evaluation.Selectors;

public sealed class InheritsFromSelector(string baseTypePattern) : ITargetSelector
{
    public string Kind => "inherits_from";

    public IEnumerable<object> SelectCandidates(RepositoryModel model) =>
        model.Solutions
            .SelectMany(solution => solution.Projects)
            .SelectMany(project => project.Types)
            .Where(type => type.BaseType is not null && GlobMatcher.IsMatch(type.BaseType, baseTypePattern));
}
