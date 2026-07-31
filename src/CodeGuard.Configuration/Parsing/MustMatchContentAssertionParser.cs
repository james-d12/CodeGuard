using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustMatchContentAssertionParser : IAssertionParser
{
    public string Kind => "must_match_content";

    public IAssertion Parse(JsonObject parameters) =>
        new MustMatchContentAssertion(parameters.GetRequiredString("pattern"));
}
