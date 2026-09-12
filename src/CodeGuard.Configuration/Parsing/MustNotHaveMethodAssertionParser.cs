using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotHaveMethodAssertionParser : IAssertionParser
{
    public string Kind => "must_not_have_method";

    public CapabilityDescriptor Descriptor => new(
        "must_not_have_method",
        "Type must not declare a method with a matching name.",
        [
            ParameterDescriptor.RequiredGlob("name", "Method name that must not be present.")
        ])
    { AppliesTo = [CandidateKind.Type] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotHaveMethodAssertion(parameters.GetRequiredString("name"));
}
