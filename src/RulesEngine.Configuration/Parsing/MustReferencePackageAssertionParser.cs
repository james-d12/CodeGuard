using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustReferencePackageAssertionParser : IAssertionParser
{
    public string Kind => "must_reference_package";

    public IAssertion Parse(JsonObject parameters) =>
        new MustReferencePackageAssertion(parameters.GetRequiredString("id"));
}
