using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHaveMethodAssertionParser : IAssertionParser
{
    public string Kind => "must_have_method";

    public CapabilityDescriptor Descriptor => new(
        "must_have_method",
        "Type must declare a method with a matching name.",
        [
            ParameterDescriptor.RequiredGlob("name", "Method name.")
        ])
    { AppliesTo = [CandidateKind.Type] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveMethodAssertion(parameters.GetRequiredString("name"));
}
