using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Selectors;

public sealed class ConstructorSelector(
    string declaringTypePattern = "*",
    IReadOnlyList<string>? parameterTypePatterns = null) : ITargetSelector
{
    public string Kind => "constructor";

    public IEnumerable<object> SelectCandidates(RepositoryModel model) =>
        model.Solutions
            .SelectMany(solution => solution.Projects)
            .SelectMany(project => project.Types)
            .SelectMany(type => type.Constructors)
            .Where(constructor => GlobMatcher.IsMatch(constructor.DeclaringType, declaringTypePattern))
            .Where(MatchesParameterTypes);

    private bool MatchesParameterTypes(ConstructorModel constructor)
    {
        if (parameterTypePatterns is null)
        {
            return true;
        }

        if (constructor.Parameters.Count != parameterTypePatterns.Count)
        {
            return false;
        }

        return constructor.Parameters
            .Zip(parameterTypePatterns, (parameter, pattern) => GlobMatcher.IsMatch(parameter.Type, pattern))
            .All(matches => matches);
    }
}
