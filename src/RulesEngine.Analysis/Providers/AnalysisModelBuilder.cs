using RulesEngine.Analysis.AnalysisModel;

namespace RulesEngine.Analysis.Providers;

public sealed class AnalysisModelBuilder(IEnumerable<IAnalysisProvider> providers)
{
    public async Task<RepositoryModel> BuildAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        var context = new AnalysisModelBuilderContext(rootPath);

        foreach (var provider in providers)
        {
            await provider.ContributeAsync(context, cancellationToken);
        }

        return context.Build();
    }
}
