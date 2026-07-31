using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHaveDirectoryAssertionParser : IAssertionParser
{
    public string Kind => "must_have_directory";

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveDirectoryAssertion(parameters.GetRequiredString("path"));
}
