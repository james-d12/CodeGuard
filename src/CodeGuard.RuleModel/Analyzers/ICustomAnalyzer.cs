using CodeGuard.Analysis.AnalysisModel;

namespace CodeGuard.RuleModel.Analyzers;

public interface ICustomAnalyzer
{
    string Name { get; }

    IEnumerable<AnalyzerViolation> Analyze(RepositoryModel model);
}
