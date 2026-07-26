using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustNotMatchContentAssertionParser : IAssertionParser
{
    public string Kind => "must_not_match_content";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotMatchContentAssertion(parameters.GetRequiredString("pattern"));
}
