using System.Text.Json.Nodes;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.RuleModel.Analyzers;

namespace CodeGuard.Configuration.Parsing;

public interface IAnalyzerParser
{
    string Kind { get; }

    /// <summary>
    /// Declares this analyzer's parameters. Must be kept in step with <see cref="Parse"/>; a test
    /// asserts every registered kind has one. Analyzers run against the whole repository model rather
    /// than a candidate set, so they declare no <see cref="CapabilityDescriptor.AppliesTo"/>.
    /// </summary>
    CapabilityDescriptor Descriptor { get; }

    ICustomAnalyzer Parse(JsonObject node);
}
