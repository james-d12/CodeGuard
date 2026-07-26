using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustInheritFromAssertionParser : IAssertionParser
{
    public string Kind => "must_inherit_from";

    public IAssertion Parse(JsonObject parameters) =>
        new MustInheritFromAssertion(parameters.GetRequiredString("type"));
}
