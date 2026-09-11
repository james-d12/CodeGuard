using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using CodeGuard.Configuration.Capabilities;

namespace CodeGuard.Cli.Support;

/// <summary>
/// Renders the <see cref="CapabilityCatalog"/> for `rules discover`. The markdown format is the
/// source the authoring skill's reference tables are generated from, so its shape is load-bearing:
/// see scripts/sync-skill-references.sh.
/// </summary>
public static class CapabilityReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static void WriteConsole(CapabilityCatalog catalog, TextWriter writer)
    {
        WriteSection(writer, "Target selectors", catalog.Selectors, d => d.Produces is { } p ? $"yields {p}" : null);
        WriteSection(writer, "Assertions", catalog.Assertions, d =>
            d.AppliesTo.Count == 0 ? "any candidate" : "on " + string.Join("/", d.AppliesTo));
        WriteSection(writer, "Analyzers", catalog.Analyzers, _ => null);

        writer.WriteLine($"Conditions ({catalog.Conditions.Count})");
        writer.WriteLine($"  {string.Join(", ", catalog.Conditions)}");
        writer.WriteLine("  Any assertion kind above is also valid as a `when:` leaf.");
    }

    private static void WriteSection(
        TextWriter writer,
        string title,
        IReadOnlyList<CapabilityDescriptor> descriptors,
        Func<CapabilityDescriptor, string?> annotate)
    {
        writer.WriteLine($"{title} ({descriptors.Count})");
        foreach (var descriptor in descriptors)
        {
            var annotation = annotate(descriptor);
            writer.WriteLine($"  {descriptor.Kind}{(annotation is null ? "" : $"  [{annotation}]")}");
            writer.WriteLine($"    {descriptor.Summary}");
            foreach (var parameter in descriptor.Parameters)
            {
                writer.WriteLine($"      {parameter.Name}: {DescribeType(parameter)}");
            }
        }

        writer.WriteLine();
    }

    public static void WriteJson(CapabilityCatalog catalog, TextWriter writer) =>
        writer.WriteLine(JsonSerializer.Serialize(catalog, JsonOptions));

    public static void WriteMarkdown(CapabilityCatalog catalog, TextWriter writer, string section)
    {
        switch (section)
        {
            case "selectors":
                WriteTable(writer, catalog.Selectors, "selects", d => d.Summary);
                break;
            case "assertions":
                WriteTable(writer, catalog.Assertions, "applies to", d =>
                    d.AppliesTo.Count == 0 ? "any candidate" : string.Join(", ", d.AppliesTo));
                break;
            case "analyzers":
                WriteTable(writer, catalog.Analyzers, "checks", d => d.Summary);
                break;
            default:
                throw new ArgumentException($"Unknown markdown section '{section}'.", nameof(section));
        }
    }

    private static void WriteTable(
        TextWriter writer,
        IReadOnlyList<CapabilityDescriptor> descriptors,
        string lastColumn,
        Func<CapabilityDescriptor, string> lastValue)
    {
        writer.WriteLine($"| `kind` | params | {lastColumn} |");
        writer.WriteLine("|---|---|---|");
        foreach (var descriptor in descriptors)
        {
            var parameters = descriptor.Parameters.Count == 0
                ? "*(none)*"
                : Escape(string.Join(", ", descriptor.Parameters.Select(FormatParameterCell)));
            writer.WriteLine($"| `{descriptor.Kind}` | {parameters} | {Escape(lastValue(descriptor))} |");
        }
    }

    private static string FormatParameterCell(ParameterDescriptor parameter)
    {
        var cell = new StringBuilder($"`{parameter.Name}` ({DescribeType(parameter)}");
        cell.Append(parameter.Required ? ", required" : ", optional");
        if (parameter.Default is { } @default)
        {
            cell.Append($", default `{@default}`");
        }

        return cell.Append(')').ToString();
    }

    private static string DescribeType(ParameterDescriptor parameter) =>
        parameter is { Type: ParameterType.Enum, AllowedValues: { Count: > 0 } values }
            ? string.Join(" | ", values)
            : parameter.Type switch
            {
                ParameterType.Glob => "glob",
                ParameterType.Regex => "regex",
                ParameterType.Bool => "bool",
                ParameterType.Int => "int",
                ParameterType.StringList => "string[]",
                ParameterType.Selector => "nested selector",
                ParameterType.AssertionList => "nested assertions",
                ParameterType.Enum => "enum",
                _ => "string"
            };

    // Table cells are pipe-delimited, so an unescaped pipe splits the row. Enum parameters render
    // their allowed values as `a | b | c`, so this matters for the params column too, not just prose.
    private static string Escape(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);
}
