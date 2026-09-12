using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustNotHaveJsonFieldAssertionParser : IAssertionParser
{
    public string Kind => "must_not_have_json_field";

    public CapabilityDescriptor Descriptor => new(
        "must_not_have_json_field",
        "JSON file must not contain a field, or must not have it set to a value.",
        [
            new ParameterDescriptor("path", ParameterType.String, true, "Dotted path to the JSON field."),
            new ParameterDescriptor("equals", ParameterType.String, false, "Only fail when the field equals this value.")
        ])
    { AppliesTo = [CandidateKind.File] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustNotHaveJsonFieldAssertion(parameters.GetRequiredString("path"), parameters.GetOptionalString("equals"));
}
