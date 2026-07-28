using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Selectors;

namespace RulesEngine.Evaluation.Tests.Selectors;

public class FileSelectorTests
{
    private static RepositoryModel BuildModel(params FileModel[] files) => new(".", [], files, [], [], [], [], [], [], []);

    [Fact]
    public void SelectCandidates_FiltersByPathPattern()
    {
        var model = BuildModel(
            new FileModel("/repo/.editorconfig", ".editorconfig", ""),
            new FileModel("/repo/src/Foo.cs", "src/Foo.cs", ".cs"));

        var candidates = new FileSelector("*.editorconfig").SelectCandidates(model).Cast<FileModel>().ToList();

        var file = Assert.Single(candidates);
        Assert.Equal(".editorconfig", file.RelativePath);
    }

    [Fact]
    public void SelectCandidates_FiltersByExtension()
    {
        var model = BuildModel(
            new FileModel("/repo/src/Foo.cs", "src/Foo.cs", ".cs"),
            new FileModel("/repo/src/Foo.csproj", "src/Foo.csproj", ".csproj"));

        var candidates = new FileSelector(extension: ".csproj").SelectCandidates(model).Cast<FileModel>().ToList();

        var file = Assert.Single(candidates);
        Assert.Equal(".csproj", file.Extension);
    }
}
