using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using CodeGuard.Configuration.Loading;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Benchmarks;

/// <summary>
/// The real `rules/` directory is gitignored (company-derived content, local-only), so a benchmark
/// can't depend on it - a fresh clone or CI wouldn't have it. This replicates the small portable
/// fixture set under tests/CodeGuard.IntegrationTests/Fixtures/ExampleRules/ (11 rules) up to a
/// representative rule count instead, so `dotnet run` works anywhere.
/// </summary>
internal static class SyntheticRuleSetGenerator
{
    private static readonly Regex IdLine = new("^id:\\s*(\\S+)", RegexOptions.Multiline);

    public static IReadOnlyList<RuleDefinition> Generate(int targetCount = 110, [CallerFilePath] string sourceFilePath = "")
    {
        var sourceDirectory = Path.Combine(
            Path.GetDirectoryName(sourceFilePath)!, "..", "..", "tests", "CodeGuard.IntegrationTests", "Fixtures", "ExampleRules");
        var sourceFiles = Directory.EnumerateFiles(sourceDirectory, "*.yml", SearchOption.AllDirectories).ToList();

        var outputDirectory = Path.Combine(Path.GetTempPath(), $"codeguard-benchmark-rules-{Guid.NewGuid()}");
        Directory.CreateDirectory(outputDirectory);

        var copiesNeeded = (int)Math.Ceiling(targetCount / (double)sourceFiles.Count);
        for (var copy = 0; copy < copiesNeeded; copy++)
        {
            foreach (var sourceFile in sourceFiles)
            {
                var text = File.ReadAllText(sourceFile);
                var match = IdLine.Match(text);
                var replicatedText = IdLine.Replace(text, $"id: {match.Groups[1].Value}-COPY{copy}", 1);

                var fileName = $"{Path.GetFileNameWithoutExtension(sourceFile)}-copy{copy}.yml";
                File.WriteAllText(Path.Combine(outputDirectory, fileName), replicatedText);
            }
        }

        return RuleFileLoader.CreateDefault().LoadFromDirectory(outputDirectory);
    }
}
