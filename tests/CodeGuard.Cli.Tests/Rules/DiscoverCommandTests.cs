using System.Text.Json;
using CodeGuard.Cli.Commands.Rules;

namespace CodeGuard.Cli.Tests.Rules;

/// <summary>Covers `rules discover` end-to-end via its System.CommandLine `Command`.</summary>
[Collection(ConsoleOutputCollection.Name)]
public sealed class DiscoverCommandTests
{
    [Fact]
    public async Task Run_Console_ListsEveryCategoryWithCounts()
    {
        var (exitCode, output) = await RunDiscover();

        Assert.Equal(0, exitCode);
        Assert.Contains("Target selectors (21)", output, StringComparison.Ordinal);
        Assert.Contains("Assertions (46)", output, StringComparison.Ordinal);
        Assert.Contains("Analyzers (11)", output, StringComparison.Ordinal);
        Assert.Contains("Conditions (3)", output, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Run_Json_EmitsParametersAndCandidateKinds()
    {
        var (exitCode, output) = await RunDiscover(["--format", "json"]);

        Assert.Equal(0, exitCode);
        using var document = JsonDocument.Parse(output);
        var root = document.RootElement;

        Assert.Equal(21, root.GetProperty("selectors").GetArrayLength());
        Assert.Equal(46, root.GetProperty("assertions").GetArrayLength());
        Assert.Equal(11, root.GetProperty("analyzers").GetArrayLength());

        var inheritFrom = root.GetProperty("assertions").EnumerateArray()
            .Single(a => a.GetProperty("kind").GetString() == "must_inherit_from");
        Assert.Equal("type", inheritFrom.GetProperty("parameters")[0].GetProperty("name").GetString());
        Assert.True(inheritFrom.GetProperty("parameters")[0].GetProperty("required").GetBoolean());
        Assert.Equal("type", inheritFrom.GetProperty("appliesTo")[0].GetString());

        var classSelector = root.GetProperty("selectors").EnumerateArray()
            .Single(s => s.GetProperty("kind").GetString() == "class");
        Assert.Equal("type", classSelector.GetProperty("produces").GetString());
    }

    [Theory]
    [InlineData("selectors", 21)]
    [InlineData("assertions", 46)]
    [InlineData("analyzers", 11)]
    public async Task Run_Markdown_EmitsAWellFormedTable(string section, int expectedRows)
    {
        var (exitCode, output) = await RunDiscover(["--format", "markdown", "--section", section]);

        Assert.Equal(0, exitCode);

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(expectedRows + 2, lines.Length);

        // Every data row must have exactly four unescaped pipes, or the table breaks when rendered.
        // Enum parameters render their allowed values as `a | b | c`, which is why escaping matters.
        foreach (var line in lines.Where((_, i) => i != 1))
        {
            Assert.Equal(4, CountUnescapedPipes(line));
        }
    }

    [Fact]
    public async Task Run_UnknownFormat_IsRejected()
    {
        var (exitCode, _) = await RunDiscover(["--format", "yaml"]);

        Assert.NotEqual(0, exitCode);
    }

    private static int CountUnescapedPipes(string line)
    {
        var count = 0;
        for (var i = 0; i < line.Length; i++)
        {
            if (line[i] == '|' && (i == 0 || line[i - 1] != '\\'))
            {
                count++;
            }
        }

        return count;
    }

    private static async Task<(int ExitCode, string Output)> RunDiscover(IReadOnlyList<string>? args = null)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetError(new StringWriter());
        try
        {
            var exitCode = await DiscoverCommand.Build().Parse((args ?? []).ToArray()).InvokeAsync();
            return (exitCode, writer.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }
}
