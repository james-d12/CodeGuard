using System.Text.Json.Nodes;
using RulesEngine.Evaluation.Assertions;
using RulesEngine.RuleModel.Assertions;

namespace RulesEngine.Configuration.Parsing;

public sealed class MustNotReferencePackageAssertionParser : IAssertionParser
{
    public string Kind => "must_not_reference_package";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotReferencePackageAssertion(parameters.GetRequiredString("id"));
}
