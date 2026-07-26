namespace RulesEngine.Analysis.AnalysisModel;

public sealed record ProjectModel(
    string Name,
    string Path,
    string TargetFramework,
    string Sdk,
    IReadOnlyList<string> ProjectReferences,
    IReadOnlyList<PackageReferenceModel> PackageReferences,
    IReadOnlyDictionary<string, string> Properties,
    IReadOnlyList<TypeModel> Types);

public sealed record PackageReferenceModel(string Id, string Version);
