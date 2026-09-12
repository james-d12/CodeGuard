using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustDependOnAssertionParser : IAssertionParser
{
    public string Kind => "must_depend_on";

    public CapabilityDescriptor Descriptor => new(
        "must_depend_on",
        "Project must reference at least one matching type, via any type-reference site.",
        [
            ParameterDescriptor.RequiredGlob("type", "Type that must be depended on.")
        ])
    { AppliesTo = [CandidateKind.Project] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustDependOnAssertion(parameters.GetRequiredString("type"));
}
