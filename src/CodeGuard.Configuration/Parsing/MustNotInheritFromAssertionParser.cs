using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotInheritFromAssertionParser : IAssertionParser
{
    public string Kind => "must_not_inherit_from";

    public CapabilityDescriptor Descriptor => new(
        "must_not_inherit_from",
        "Type must not derive from a matching base type.",
        [
            ParameterDescriptor.RequiredGlob("type", "Base type that must not be inherited.")
        ])
    { AppliesTo = [CandidateKind.Type] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotInheritFromAssertion(parameters.GetRequiredString("type"));
}
