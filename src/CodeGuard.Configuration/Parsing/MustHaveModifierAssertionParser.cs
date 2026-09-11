using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHaveModifierAssertionParser : IAssertionParser
{
    public string Kind => "must_have_modifier";

    public CapabilityDescriptor Descriptor => new(
        "must_have_modifier",
        "Candidate must declare the given modifier. Valid values depend on the candidate kind.",
        [
            new ParameterDescriptor("modifier", ParameterType.Enum, true, "Modifier that must be present.", AllowedValues: ["record", "sealed", "abstract", "static", "partial", "virtual", "override", "async", "const", "readonly", "required", "init"])
        ])
    { AppliesTo = [CandidateKind.Type, CandidateKind.Method, CandidateKind.Field, CandidateKind.Property] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveModifierAssertion(parameters.GetRequiredString("modifier"));
}
