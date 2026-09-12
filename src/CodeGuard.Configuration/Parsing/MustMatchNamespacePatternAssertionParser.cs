using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustMatchNamespacePatternAssertionParser : IAssertionParser
{
    public string Kind => "must_match_namespace_pattern";

    public CapabilityDescriptor Descriptor => new(
        "must_match_namespace_pattern",
        "Type's namespace must match a regex, for shapes a glob cannot express.",
        [
            new ParameterDescriptor("regex", ParameterType.Regex, true, "Regex matched against the type's namespace.")
        ])
    { AppliesTo = [CandidateKind.Type] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustMatchNamespacePatternAssertion(parameters.GetRequiredString("regex"));
}
