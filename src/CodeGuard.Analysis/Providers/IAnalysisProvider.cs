namespace CodeGuard.Analysis.Providers;

public interface IAnalysisProvider
{
    string Name { get; }

    Task ContributeAsync(AnalysisModelBuilderContext context, CancellationToken cancellationToken);
}
