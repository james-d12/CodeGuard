using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotMatchContentAssertionParser : IAssertionParser
{
    public string Kind => "must_not_match_content";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotMatchContentAssertion(parameters.GetRequiredString("pattern"));
}
