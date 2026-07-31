using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustMatchFilenameAssertionParser : IAssertionParser
{
    public string Kind => "must_match_filename";

    public IAssertion Parse(JsonObject parameters) => new MustMatchFilenameAssertion();
}
