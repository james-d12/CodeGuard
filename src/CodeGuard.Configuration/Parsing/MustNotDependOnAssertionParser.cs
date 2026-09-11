using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotDependOnAssertionParser : IAssertionParser
{
    public string Kind => "must_not_depend_on";

    public CapabilityDescriptor Descriptor => new(
        "must_not_depend_on",
        "Project must not reference any matching type.",
        [
            ParameterDescriptor.RequiredGlob("type", "Type that must not be depended on.")
        ])
    { AppliesTo = [CandidateKind.Project] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotDependOnAssertion(parameters.GetRequiredString("type"));
}
