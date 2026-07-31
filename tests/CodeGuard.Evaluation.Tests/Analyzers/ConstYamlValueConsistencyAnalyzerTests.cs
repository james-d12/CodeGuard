using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Analyzers;

namespace CodeGuard.Evaluation.Tests.Analyzers;

public sealed class ConstYamlValueConsistencyAnalyzerTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"rulesengine-typename-{Guid.NewGuid():N}.yml");

    private FileModel WriteYamlFile(string yaml)
    {
        File.WriteAllText(_path, yaml);
        return new FileModel(_path, Path.GetFileName(_path), ".yml");
    }

    private static FieldModel ConstField(string value) => new(
        Name: "TypeName", Type: "string", Accessibility: Accessibility.Public, Modifiers: FieldModifiers.Const,
        Attributes: [], DeclaringType: "Contoso.Client.OrderConfig", ProjectName: "Contoso.Client",
        FilePath: "OrderConfig.cs", Line: 1, Column: 1, ConstantValue: value);

    [Fact]
    public void Analyze_DoesNotFlag_WhenYamlFieldMatchesConstValue()
    {
        var file = WriteYamlFile("client:\n  typeName: Order\n");
        var type = TestModels.Type("Contoso.Client.OrderConfig", fields: [ConstField("Order")]);
        var project = TestModels.Project("Contoso.Client", types: [type]);
        var model = TestModels.RepositoryWithFacts(projects: [project], files: [file]);
        var analyzer = new ConstYamlValueConsistencyAnalyzer("Contoso.Client.OrderConfig", "TypeName", "*.yml", "client.typeName");

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Analyze_Flags_WhenYamlFieldDoesNotMatchConstValue()
    {
        var file = WriteYamlFile("client:\n  typeName: WrongName\n");
        var type = TestModels.Type("Contoso.Client.OrderConfig", fields: [ConstField("Order")]);
        var project = TestModels.Project("Contoso.Client", types: [type]);
        var model = TestModels.RepositoryWithFacts(projects: [project], files: [file]);
        var analyzer = new ConstYamlValueConsistencyAnalyzer("Contoso.Client.OrderConfig", "TypeName", "*.yml", "client.typeName");

        var violations = analyzer.Analyze(model).ToList();

        var violation = Assert.Single(violations);
        Assert.Contains("WrongName", violation.Message);
        Assert.Contains("Order", violation.Message);
    }

    [Fact]
    public void Analyze_Flags_WhenYamlFieldIsMissing()
    {
        var file = WriteYamlFile("client:\n  other: value\n");
        var type = TestModels.Type("Contoso.Client.OrderConfig", fields: [ConstField("Order")]);
        var project = TestModels.Project("Contoso.Client", types: [type]);
        var model = TestModels.RepositoryWithFacts(projects: [project], files: [file]);
        var analyzer = new ConstYamlValueConsistencyAnalyzer("Contoso.Client.OrderConfig", "TypeName", "*.yml", "client.typeName");

        var violations = analyzer.Analyze(model).ToList();

        var violation = Assert.Single(violations);
        Assert.Contains("<missing>", violation.Message);
    }

    [Fact]
    public void Analyze_Flags_WhenYamlFileHasNoDocuments()
    {
        var file = WriteYamlFile("");
        var type = TestModels.Type("Contoso.Client.OrderConfig", fields: [ConstField("Order")]);
        var project = TestModels.Project("Contoso.Client", types: [type]);
        var model = TestModels.RepositoryWithFacts(projects: [project], files: [file]);
        var analyzer = new ConstYamlValueConsistencyAnalyzer("Contoso.Client.OrderConfig", "TypeName", "*.yml", "client.typeName");

        var violations = analyzer.Analyze(model).ToList();

        var violation = Assert.Single(violations);
        Assert.Contains("<missing>", violation.Message);
    }

    [Fact]
    public void Analyze_ProducesNothing_WhenConstFieldNotFound()
    {
        var file = WriteYamlFile("client:\n  typeName: Order\n");
        var model = TestModels.RepositoryWithFacts(files: [file]);
        var analyzer = new ConstYamlValueConsistencyAnalyzer("Contoso.Client.OrderConfig", "TypeName", "*.yml", "client.typeName");

        var violations = analyzer.Analyze(model).ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Name_IsConstYamlValueConsistency()
    {
        var analyzer = new ConstYamlValueConsistencyAnalyzer("*", "TypeName", "*.yml", "client.typeName");

        Assert.Equal("const-yaml-value-consistency", analyzer.Name);
    }

    public void Dispose()
    {
        if (File.Exists(_path))
        {
            File.Delete(_path);
        }
    }
}
