using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Configuration.Parsing;

public interface ISelectorParser
{
    string Kind { get; }

    /// <summary>
    /// Declares this selector's parameters and the candidate kind it yields. Must be kept in step with
    /// <see cref="Parse"/> - a parameter read there but not declared here is invisible to
    /// `codeguard rules discover` and to the generated authoring docs. A test asserts every registered
    /// kind has one.
    /// </summary>
    CapabilityDescriptor Descriptor { get; }

    ITargetSelector Parse(JsonObject node);
}
