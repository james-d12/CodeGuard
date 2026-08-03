using BenchmarkDotNet.Attributes;
using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Core.Evaluation;
using CodeGuard.RuleModel.Rules;

namespace CodeGuard.Benchmarks;

/// <summary>
/// Benchmarks RuleEvaluator.Evaluate against a synthesized ~110-rule set (see
/// SyntheticRuleSetGenerator) and a synthetic RepositoryModel with enough candidates per rule
/// (see SyntheticModelBuilder) to exercise rule-level parallelism.
/// </summary>
[MemoryDiagnoser]
public class RuleEvaluationBenchmarks
{
    private IReadOnlyList<RuleDefinition> _rules = [];
    private RepositoryModel _model = null!;

    [GlobalSetup]
    public void Setup()
    {
        _rules = SyntheticRuleSetGenerator.Generate();
        _model = SyntheticModelBuilder.Build();
    }

    [Benchmark]
    public int Evaluate() => new RuleEvaluator().Evaluate(_rules, _model).Violations.Count;
}
