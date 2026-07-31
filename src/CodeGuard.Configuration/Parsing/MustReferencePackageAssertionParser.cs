using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustReferencePackageAssertionParser : IAssertionParser
{
    public string Kind => "must_reference_package";

    public IAssertion Parse(JsonObject parameters) =>
        new MustReferencePackageAssertion(parameters.GetRequiredString("id"));
}
