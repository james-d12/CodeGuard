using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustNotHaveFileAssertionParser : IAssertionParser
{
    public string Kind => "must_not_have_file";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotHaveFileAssertion(parameters.GetRequiredString("path"));
}
