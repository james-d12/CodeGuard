namespace CodeGuard.Analysis.AnalysisModel;

public sealed record DependencyEdge(
    string FromNamespaceOrProject,
    string ToNamespaceOrProject,
    DependencyEdgeKind Kind);

public enum DependencyEdgeKind
{
    TypeReference,
    ProjectReference,
    PackageReference
}
