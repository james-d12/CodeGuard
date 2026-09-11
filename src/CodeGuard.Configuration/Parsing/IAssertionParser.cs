using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Configuration.Parsing;

public interface IAssertionParser
{
    string Kind { get; }

    /// <summary>
    /// Declares this assertion's parameters and the candidate kinds it accepts. Must be kept in step
    /// with <see cref="Parse"/> and with the assertion's own candidate type check - the declared
    /// <see cref="CapabilityDescriptor.AppliesTo"/> is what lets an unreachable selector/assertion
    /// pairing be caught without evaluating the rule. A test asserts every registered kind has one.
    /// </summary>
    CapabilityDescriptor Descriptor { get; }

    IAssertion Parse(JsonObject parameters);
}
