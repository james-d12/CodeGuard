using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustBeInProjectAssertionParser : IAssertionParser
{
    public string Kind => "must_be_in_project";

    public CapabilityDescriptor Descriptor => new(
        "must_be_in_project",
        "Type must be declared in a matching project.",
        [
            ParameterDescriptor.RequiredGlob("pattern", "Project the type must belong to.")
        ])
    { AppliesTo = [CandidateKind.Type] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustBeInProjectAssertion(parameters.GetRequiredString("pattern"));
}
