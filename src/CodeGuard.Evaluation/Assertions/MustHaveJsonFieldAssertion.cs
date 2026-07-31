using System.Text.Json.Nodes;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Assertions;

public sealed class MustHaveJsonFieldAssertion(string path, string? equals) : IAssertion
{
    public string Kind => "must_have_json_field";

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
            return AssertionOutcome.Failure($"File '{file.RelativePath}' must have JSON field '{path}'.");
        }

        if (equals is not null && fieldValue.ToString() != equals)
        {
            return AssertionOutcome.Failure(
                $"File '{file.RelativePath}' must have JSON field '{path}' equal to '{equals}' (found '{fieldValue}').");
        }

        return AssertionOutcome.Success();
    }
}

internal static class JsonFieldPath
{
    public static JsonNode? Resolve(JsonNode? root, string dottedPath)
    {
        var current = root;
        foreach (var segment in dottedPath.Split('.'))
        {
            if (current is null)
            {
                return null;
            }

            current = current[segment];
        }

        return current;
    }
}
