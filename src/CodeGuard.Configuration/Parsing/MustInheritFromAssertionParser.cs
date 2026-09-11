using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public sealed class MustInheritFromAssertionParser : IAssertionParser
{
    public string Kind => "must_inherit_from";

    public CapabilityDescriptor Descriptor => new(
        "must_inherit_from",
        "Type must derive from a matching base type.",
        [
            ParameterDescriptor.RequiredGlob("type", "Base type. Roslyn renders closed generics, so use Entity<*> not Entity<TId>.")
        ])
    { AppliesTo = [CandidateKind.Type] };

    public IAssertion Parse(JsonObject parameters) =>
        new MustInheritFromAssertion(parameters.GetRequiredString("type"));
}
