using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustMatchFilenameAssertionParser : IAssertionParser
{
    public string Kind => "must_match_filename";

    public IAssertion Parse(JsonObject parameters) => new MustMatchFilenameAssertion();
}
