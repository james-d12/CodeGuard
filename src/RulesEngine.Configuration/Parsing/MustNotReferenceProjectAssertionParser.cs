using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustNotReferenceProjectAssertionParser : IAssertionParser
{
    public string Kind => "must_not_reference_project";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotReferenceProjectAssertion(parameters.GetRequiredString("name"));
}
