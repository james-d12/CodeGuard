using System.Text.Json.Nodes;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Assertions;

public sealed class MustNotHaveJsonFieldAssertion(string path, string? equals) : IAssertion
{
    public string Kind => "must_not_have_json_field";

    public AssertionOutcome Evaluate(object candidate, RepositoryModel model)
    {
        if (candidate is not FileModel file)
        {
            return AssertionOutcome.Failure($"'{Kind}' can only be evaluated against files.");
        }

        var root = JsonNode.Parse(File.ReadAllText(file.Path));
        var fieldValue = JsonFieldPath.Resolve(root, path);

        if (fieldValue is null)
        {
            return AssertionOutcome.Success();
        }

        if (equals is not null && fieldValue.ToString() != equals)
        {
            return AssertionOutcome.Success();
        }

        return AssertionOutcome.Failure($"File '{file.RelativePath}' must not have JSON field '{path}'{(equals is null ? "" : $" equal to '{equals}'")}.");
    }
}
