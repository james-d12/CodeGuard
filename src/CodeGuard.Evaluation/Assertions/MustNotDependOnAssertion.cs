using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Assertions;

public sealed class MustNotDependOnAssertion(string forbiddenTypePattern) : IAssertion
{
    public string Kind => "must_not_depend_on";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not ProjectModel project)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against projects.");
        }

        var offendingTypes = project.Types
            .Where(type => ReferencedTypeNames(type).Any(name => GlobMatcher.IsMatch(name, forbiddenTypePattern)))
            .Select(type => type.FullName)
            .ToList();

        return offendingTypes.Count == 0
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure(
                $"Project '{project.Name}' must not depend on '{forbiddenTypePattern}' (via: {string.Join(", ", offendingTypes)}).");
    }

    private static IEnumerable<string> ReferencedTypeNames(TypeModel type)
    {
        if (type.BaseType is not null)
        {
            yield return type.BaseType;
        }

        foreach (var i in type.Interfaces)
        {
            yield return i;
        }

        foreach (var method in type.Methods)
        {
            yield return method.ReturnType;
            foreach (var parameter in method.Parameters)
            {
                yield return parameter.Type;
            }
        }
    }
}
