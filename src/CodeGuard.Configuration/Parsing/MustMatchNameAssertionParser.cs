using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustMatchNameAssertionParser : IAssertionParser
{
    public string Kind => "must_match_name";

    public IAssertion Parse(JsonObject parameters) =>
        new MustMatchNameAssertion(parameters.GetRequiredString("regex"));
}
