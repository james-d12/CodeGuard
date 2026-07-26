using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustHaveDirectoryAssertionParser : IAssertionParser
{
    public string Kind => "must_have_directory";

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveDirectoryAssertion(parameters.GetRequiredString("path"));
}
