using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustHaveFileAssertionParser : IAssertionParser
{
    public string Kind => "must_have_file";

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveFileAssertion(parameters.GetRequiredString("path"));
}
