using RulesEngine.Analysis.AnalysisModel;

namespace RulesEngine.RuleModel.Analyzers;

public interface ICustomAnalyzer
{
    string Name { get; }

    IEnumerable<AnalyzerViolation> Analyze(RepositoryModel model);
}
