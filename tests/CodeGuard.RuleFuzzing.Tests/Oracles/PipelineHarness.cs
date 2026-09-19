using System.Text.Json.Nodes;
using CodeGuard.Configuration.Analysis;
using CodeGuard.Configuration.Capabilities;
using CodeGuard.Configuration.Parsing;
using CodeGuard.Configuration.Validation;
using CodeGuard.Core.Evaluation;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.RuleFuzzing.Tests.Oracles;

/// <summary>
/// The real pipeline, wired up once and reused across iterations - mirrors
/// `RuleFileLoaderTests.CreateLoader()`'s registry construction. Stateless/reusable, so building it
/// per test class (not per generated document) keeps per-iteration overhead to just parsing/evaluation.
/// </summary>
internal sealed class PipelineHarness
{
    private readonly SelectorParserRegistry _selectorParsers;
    private readonly AssertionParserRegistry _assertionParsers;
    private readonly ConditionParserRegistry _conditionParsers;
    private readonly AnalyzerParserRegistry _analyzerParsers;
    private readonly RuleSchemaValidator _schemaValidator;

    public PipelineHarness()
    {
        _selectorParsers = DefaultParsers.CreateSelectorRegistry();
        _assertionParsers = DefaultParsers.CreateAssertionRegistry(_selectorParsers);
        _conditionParsers = DefaultParsers.CreateConditionRegistry(_assertionParsers);
        _analyzerParsers = DefaultAnalyzers.CreateRegistry();
        _schemaValidator = RuleSchemaValidator.CreateDefault();
        Catalog = CapabilityCatalog.Create();
        Evaluator = new RuleEvaluator();
    }

    public CapabilityCatalog Catalog { get; }

    public RuleEvaluator Evaluator { get; }

    public void ValidateSchema(JsonObject document) => _schemaValidator.Validate(document, "<fuzz>");

    public RuleDefinition Parse(JsonObject document) =>
        RuleDocumentParser.Parse(document, _selectorParsers, _assertionParsers, _conditionParsers, _analyzerParsers);

    /// <summary>
    /// <see cref="RuleSetAnalyzer.Analyze"/>'s exact-duplicate check re-reads each rule's source file
    /// from disk (`RuleFileLoader.ReadDocument`), so a single-rule analysis needs a real temp file even
    /// though every other oracle in this suite stays fully in-memory. JSON text is valid YAML, so the
    /// generated document is written as-is.
    /// </summary>
    public RuleAnalysisReport AnalyzeSingleRule(RuleDefinition rule, JsonObject document)
    {
        using var tempFile = new TempRuleFile(document);
        var report = new RuleSetValidationReport([(rule, tempFile.Path)], []);
        return RuleSetAnalyzer.Analyze(report, Catalog);
    }

    private sealed class TempRuleFile : IDisposable
    {
        public string Path { get; } = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(), $"codeguard-fuzz-{Guid.NewGuid():N}.yml");

        public TempRuleFile(JsonObject document) => File.WriteAllText(Path, document.ToJsonString());

        public void Dispose()
        {
            try
            {
                File.Delete(Path);
            }
            catch (IOException)
            {
                // best-effort cleanup; a leaked temp file doesn't affect test correctness
            }
        }
    }
}
