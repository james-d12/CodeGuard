using CodeGuard.Analysis.AnalysisModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CodeGuard.Analysis.Providers;

public sealed class AnalysisModelBuilder(IEnumerable<IAnalysisProvider> providers, ILogger<AnalysisModelBuilder>? logger = null)
{
    private readonly ILogger<AnalysisModelBuilder> _logger = logger ?? NullLogger<AnalysisModelBuilder>.Instance;

    public async Task<RepositoryModel> BuildAsync(string rootPath, CancellationToken cancellationToken = default)
    {
        var providerList = providers.ToList();
        var context = new AnalysisModelBuilderContext(rootPath);

        _logger.LogInformation(
            "Building analysis model for {RootPath} using {ProviderCount} provider(s): {ProviderNames}",
            rootPath, providerList.Count, string.Join(", ", providerList.Select(p => p.Name)));

        foreach (var provider in providerList)
        {
            _logger.LogDebug("Running analysis provider {ProviderName}", provider.Name);
            await provider.ContributeAsync(context, cancellationToken);
        }

        var model = context.Build();
        _logger.LogInformation(
            "Analysis model built: {SolutionCount} solution(s), {ProjectCount} project(s), {FileCount} file(s)",
            model.Solutions.Count, model.Solutions.Sum(s => s.Projects.Count), model.Files.Count);

        return model;
    }
}
