using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHaveFieldAssertionParser : IAssertionParser
{
    public string Kind => "must_have_field";

    public CapabilityDescriptor Descriptor => new(
        "must_have_field",
        "Type must declare a field with a matching name.",
        [
            ParameterDescriptor.RequiredGlob("name", "Field name.")
        ])
    { AppliesTo = [CandidateKind.Type] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveFieldAssertion(parameters.GetRequiredString("name"));
}
