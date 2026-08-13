using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Selectors;

public sealed class DirectorySelector(string pathPattern = "*") : ITargetSelector
{
    public string Kind => "directory";

    public IEnumerable<object> SelectCandidates(RepositoryModel model) =>
        model.Directories.Where(directory => GlobMatcher.IsMatch(directory, pathPattern));
}
