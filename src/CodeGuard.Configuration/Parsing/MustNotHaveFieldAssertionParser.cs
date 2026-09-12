using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotHaveFieldAssertionParser : IAssertionParser
{
    public string Kind => "must_not_have_field";

    public CapabilityDescriptor Descriptor => new(
        "must_not_have_field",
        "Type must not declare a field with a matching name.",
        [
            ParameterDescriptor.RequiredGlob("name", "Field name that must not be present.")
        ])
    { AppliesTo = [CandidateKind.Type] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotHaveFieldAssertion(parameters.GetRequiredString("name"));
}
