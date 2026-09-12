using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustHaveJsonFieldAssertionParser : IAssertionParser
{
    public string Kind => "must_have_json_field";

    public CapabilityDescriptor Descriptor => new(
        "must_have_json_field",
        "JSON file must contain a field, optionally with a specific value.",
        [
            new ParameterDescriptor("path", ParameterType.String, true, "Dotted path to the JSON field."),
            new ParameterDescriptor("equals", ParameterType.String, false, "Required value. Presence alone is checked when omitted.")
        ])
    { AppliesTo = [CandidateKind.File] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustHaveJsonFieldAssertion(parameters.GetRequiredString("path"), parameters.GetOptionalString("equals"));
}
