using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.RuleModel.Analyzers;
using YamlDotNet.RepresentationModel;

namespace RulesEngine.Evaluation.Analyzers;

/// <summary>
/// Flags YAML config files whose configured field doesn't match a C# const field's literal value
/// - the const is treated as the source of truth, and the YAML is expected to mirror it exactly
/// (docs/RULE_COVERAGE_PLAN.md skill.client-config.typename-consistency is one example
/// configuration of this). Which const/YAML field pair to compare is inherently rule-specific, so
/// all four coordinates are rule YAML parameters, not hardcoded here.
/// </summary>
public sealed class ConstYamlValueConsistencyAnalyzer(
    string constTypePattern,
    string constName,
    string yamlFilePattern,
    string yamlFieldPath) : ICustomAnalyzer
{
    public string Name => "const-yaml-value-consistency";

    public IEnumerable<AnalyzerViolation> Analyze(RepositoryModel model)
    {
        var constField = model.Solutions
            .SelectMany(s => s.Projects)
            .SelectMany(p => p.Types)
            .Where(t => GlobMatcher.IsMatch(t.FullName, constTypePattern))
            .SelectMany(t => t.Fields)
            .FirstOrDefault(f => f.Name == constName && f.ConstantValue is not null);

        if (constField?.ConstantValue is not { } constValue)
        {
            yield break;
        }

        foreach (var file in model.Files.Where(f => GlobMatcher.IsMatch(f.RelativePath, yamlFilePattern)))
        {
            var yamlValue = ReadYamlField(file.Path);

            if (yamlValue != constValue)
            {
                yield return new AnalyzerViolation(
                    Message: $"'{file.RelativePath}' field '{yamlFieldPath}' is '{yamlValue ?? "<missing>"}', but expected '{constValue}' to match {constField.DeclaringType}.{constField.Name}.",
                    FilePath: file.Path);
            }
        }
    }

    private string? ReadYamlField(string filePath)
    {
        var yamlStream = new YamlStream();
        using var reader = new StringReader(File.ReadAllText(filePath));
        yamlStream.Load(reader);

        return yamlStream.Documents.Count > 0
            ? YamlFieldPath.Resolve(yamlStream.Documents[0].RootNode, yamlFieldPath)
            : null;
    }
}
