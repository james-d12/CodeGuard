using System.Text.RegularExpressions;
using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Evaluation.Assertions;

public sealed class MustMatchNameAssertion(string regex) : IAssertion
{
    public string Kind => "must_match_name";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        var name = candidate switch
        {
            TypeModel type => type.Name,
            ProjectModel project => project.Name,
            MethodModel method => method.Name,
            PropertyModel property => property.Name,
            ConstructorModel => null,
            FieldModel field => field.Name,
            FileModel file => file.RelativePath,
            _ => null
        };

        if (name is null)
        {
            return AssertionOutcome.Failure($"'{Kind}' cannot be evaluated against this candidate type.");
        }

        return Regex.IsMatch(name, regex)
            ? AssertionOutcome.Success()
            : AssertionOutcome.Failure($"'{name}' must match name pattern '{regex}'.");
    }
}
