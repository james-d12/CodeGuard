using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustReferenceProjectAssertionParser : IAssertionParser
{
    public string Kind => "must_reference_project";

    public IAssertion Parse(JsonObject parameters) =>
        new MustReferenceProjectAssertion(parameters.GetRequiredString("name"));
}
