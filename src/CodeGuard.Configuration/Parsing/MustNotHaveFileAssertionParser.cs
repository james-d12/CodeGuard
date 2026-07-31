using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotHaveFileAssertionParser : IAssertionParser
{
    public string Kind => "must_not_have_file";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotHaveFileAssertion(parameters.GetRequiredString("path"));
}
