using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustMatchNamespacePatternAssertionParser : IAssertionParser
{
    public string Kind => "must_match_namespace_pattern";

    public IAssertion Parse(JsonObject parameters) =>
        new MustMatchNamespacePatternAssertion(parameters.GetRequiredString("regex"));
}
