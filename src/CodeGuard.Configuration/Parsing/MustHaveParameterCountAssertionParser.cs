using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHaveParameterCountAssertionParser : IAssertionParser
{
    public string Kind => "must_have_parameter_count";

    public CapabilityDescriptor Descriptor => new(
        "must_have_parameter_count",
        "Method or constructor parameter count must fall within the given bounds.",
        [
            ParameterDescriptor.OptionalInt("min", "Minimum parameter count, inclusive."),
            ParameterDescriptor.OptionalInt("max", "Maximum parameter count, inclusive.")
        ])
    { AppliesTo = [CandidateKind.Method, CandidateKind.Constructor] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveParameterCountAssertion(parameters.GetOptionalInt("min"), parameters.GetOptionalInt("max"));
}
