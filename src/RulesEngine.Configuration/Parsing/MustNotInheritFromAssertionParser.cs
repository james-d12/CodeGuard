using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustNotInheritFromAssertionParser : IAssertionParser
{
    public string Kind => "must_not_inherit_from";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotInheritFromAssertion(parameters.GetRequiredString("type"));
}
