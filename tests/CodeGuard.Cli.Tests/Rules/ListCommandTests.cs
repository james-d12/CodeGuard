using CodeGuard.Cli.Commands.Rules;
using CodeGuard.Cli.Tests;

namespace CodeGuard.Cli.Tests.Rules;

/// <summary>Covers the `rules list` command's "no rules directory configured" guard end-to-end.</summary>
[Collection(ConsoleOutputCollection.Name)]
public class ListCommandTests
{
    [Fact]
    public async Task Run_NoRulesConfigured_ExitsOneAndPrintsHint()
    {
        using var globalSettings = new IsolatedGlobalSettingsScope();
        var repoDir = Directory.CreateTempSubdirectory("codeguard-list-norules-repo-").FullName;
        try
        {
            var originalOut = Console.Out;
            var originalError = Console.Error;
            var outWriter = new StringWriter();
            var errorWriter = new StringWriter();
            Console.SetOut(outWriter);
            Console.SetError(errorWriter);
            int exitCode;
            try
            {
                exitCode = await ListCommand.Build().Parse(["--path", repoDir]).InvokeAsync();
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }

            Assert.Equal(1, exitCode);
            Assert.Contains("No rules directory is configured.", errorWriter.ToString());
            Assert.Contains("codeguard setup", errorWriter.ToString());
            Assert.Contains("--rules-source", errorWriter.ToString());
        }
        finally
        {
            Directory.Delete(repoDir, recursive: true);
        }
    }
}
