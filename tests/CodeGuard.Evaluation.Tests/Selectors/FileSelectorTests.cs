using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Selectors;

namespace CodeGuard.Evaluation.Tests.Selectors;

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

    [Fact]
    public void SelectCandidates_FiltersByExtension_IgnoringCase()
    {
        var model = BuildModel(new FileModel("/repo/src/Foo.CS", "src/Foo.CS", ".CS"));

        var candidates = new FileSelector(extension: ".cs").SelectCandidates(model).ToList();

        Assert.Single(candidates);
    }

    [Fact]
    public void SelectCandidates_ReturnsEmpty_WhenRepositoryHasNoFiles()
    {
        var model = BuildModel();

        var candidates = new FileSelector().SelectCandidates(model).ToList();

        Assert.Empty(candidates);
    }

    [Fact]
    public void SelectCandidates_FiltersByName_IndependentlyOfPath()
    {
        var model = BuildModel(
            new FileModel("/repo/src/Handlers/OrderHandler.cs", "src/Handlers/OrderHandler.cs", ".cs"),
            new FileModel("/repo/src/Handlers/RateLimitingMiddleware.cs", "src/Handlers/RateLimitingMiddleware.cs", ".cs"),
            new FileModel("/repo/src/Other/PaymentHandler.cs", "src/Other/PaymentHandler.cs", ".cs"));

        var candidates = new FileSelector(pathPattern: "src/Handlers/*", name: "*Handler.cs")
            .SelectCandidates(model).Cast<FileModel>().ToList();

        var file = Assert.Single(candidates);
        Assert.Equal("src/Handlers/OrderHandler.cs", file.RelativePath);
    }

    [Fact]
    public void SelectCandidates_MatchesAllFiles_WhenNameIsOmitted()
    {
        var model = BuildModel(
            new FileModel("/repo/src/Handlers/OrderHandler.cs", "src/Handlers/OrderHandler.cs", ".cs"),
            new FileModel("/repo/src/Handlers/RateLimitingMiddleware.cs", "src/Handlers/RateLimitingMiddleware.cs", ".cs"));

        var candidates = new FileSelector(pathPattern: "src/Handlers/*").SelectCandidates(model).ToList();

        Assert.Equal(2, candidates.Count);
    }
}
