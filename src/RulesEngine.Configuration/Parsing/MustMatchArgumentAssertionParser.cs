using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustMatchArgumentAssertionParser : IAssertionParser
{
    public string Kind => "must_match_argument";

    public IAssertion Parse(JsonObject parameters) => new MustMatchArgumentAssertion(
        parameters.GetOptionalInt("index") ?? throw new RuleParsingException("'must_match_argument' requires an 'index'."),
        parameters.GetRequiredString("pattern"));
}
