using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.RuleModel.Selectors;

namespace CodeGuard.Evaluation.Selectors;

public sealed class FileSelector(string pathPattern = "*", string? extension = null) : ITargetSelector
{
    public string Kind => "file";

    public IEnumerable<object> SelectCandidates(RepositoryModel model) =>
        model.Files
            .Where(file => GlobMatcher.IsMatch(file.RelativePath, pathPattern))
            .Where(file => extension is null || string.Equals(file.Extension, extension, StringComparison.OrdinalIgnoreCase));
}
