using System.Text.Json.Nodes;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotReferencePackageAssertionParser : IAssertionParser
{
    public string Kind => "must_not_reference_package";

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotReferencePackageAssertion(parameters.GetRequiredString("id"));
}
