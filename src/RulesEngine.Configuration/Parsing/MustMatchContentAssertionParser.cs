using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustMatchContentAssertionParser : IAssertionParser
{
    public string Kind => "must_match_content";

    public IAssertion Parse(JsonObject parameters) =>
        new MustMatchContentAssertion(parameters.GetRequiredString("pattern"));
}
