using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotHaveModifierAssertionParser : IAssertionParser
{
    public string Kind => "must_not_have_modifier";

    public CapabilityDescriptor Descriptor => new(
        "must_not_have_modifier",
        "Candidate must not declare the given modifier.",
        [
            new ParameterDescriptor("modifier", ParameterType.Enum, true, "Modifier that must be absent.", AllowedValues: ["record", "sealed", "abstract", "static", "partial", "virtual", "override", "async", "const", "readonly", "required", "init"])
        ])
    { AppliesTo = [CandidateKind.Type, CandidateKind.Method, CandidateKind.Field, CandidateKind.Property] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotHaveModifierAssertion(parameters.GetRequiredString("modifier"));
}
