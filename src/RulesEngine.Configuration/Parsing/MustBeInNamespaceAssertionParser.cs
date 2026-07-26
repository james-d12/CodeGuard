using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustBeInNamespaceAssertionParser : IAssertionParser
{
    public string Kind => "must_be_in_namespace";

    public IAssertion Parse(JsonObject parameters) =>
        new MustBeInNamespaceAssertion(parameters.GetRequiredString("pattern"));
}
