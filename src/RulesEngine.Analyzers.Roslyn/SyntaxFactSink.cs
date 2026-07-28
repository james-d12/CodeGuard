using RulesEngine.Analysis.AnalysisModel;

namespace RulesEngine.Analyzers.Roslyn;

public sealed record ExtractedSyntaxFacts(
    IReadOnlyList<CallSiteModel> CallSites,
    IReadOnlyList<SwitchModel> Switches,
    IReadOnlyList<ThrowSiteModel> ThrowSites,
    IReadOnlyList<MutationSiteModel> MutationSites,
    IReadOnlyList<TryBlockModel> TryBlocks,
    IReadOnlyList<MethodBodyShapeModel> MethodBodyShapes);

internal sealed class SyntaxFactSink
{
    private readonly List<CallSiteModel> _callSites = [];
    private readonly List<SwitchModel> _switches = [];
    private readonly List<ThrowSiteModel> _throwSites = [];
    private readonly List<MutationSiteModel> _mutationSites = [];
    private readonly List<TryBlockModel> _tryBlocks = [];
    private readonly List<MethodBodyShapeModel> _methodBodyShapes = [];

    public void AddCallSite(CallSiteModel callSite) => _callSites.Add(callSite);

    public void AddSwitch(SwitchModel model) => _switches.Add(model);

    public void AddThrowSite(ThrowSiteModel model) => _throwSites.Add(model);

    public void AddMutationSite(MutationSiteModel model) => _mutationSites.Add(model);

    public void AddTryBlock(TryBlockModel model) => _tryBlocks.Add(model);

    public void AddMethodBodyShape(MethodBodyShapeModel model) => _methodBodyShapes.Add(model);

    public ExtractedSyntaxFacts Build() =>
        new(_callSites, _switches, _throwSites, _mutationSites, _tryBlocks, _methodBodyShapes);
}
