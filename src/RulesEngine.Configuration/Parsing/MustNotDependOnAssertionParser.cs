using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustNotDependOnAssertionParser : IAssertionParser
{
    public string Kind => "must_not_depend_on";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotDependOnAssertion(parameters.GetRequiredString("type"));
}
