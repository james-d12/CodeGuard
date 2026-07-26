using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustMatchNameAssertionParser : IAssertionParser
{
    public string Kind => "must_match_name";

    public IAssertion Parse(JsonObject parameters) =>
        new MustMatchNameAssertion(parameters.GetRequiredString("regex"));
}
