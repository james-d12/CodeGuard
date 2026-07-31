using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHaveFileAssertionParser : IAssertionParser
{
    public string Kind => "must_have_file";

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveFileAssertion(parameters.GetRequiredString("path"));
}
