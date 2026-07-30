using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustMatchNameAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    [Fact]
    public void Evaluate_Passes_WhenTypeNameMatchesRegex()
    {
        var type = TestModels.Type("Contoso.Domain.OrderPlacedEvent");
        var outcome = new MustMatchNameAssertion("^.+Event$").Evaluate(type, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenTypeNameDoesNotMatchRegex()
    {
        var type = TestModels.Type("Contoso.Domain.Order");
        var outcome = new MustMatchNameAssertion("^.+Event$").Evaluate(type, EmptyModel);
        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_ForUnsupportedCandidate()
    {
        var outcome = new MustMatchNameAssertion("^.+$").Evaluate(42, EmptyModel);
        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_MatchesAgainstFileRelativePath()
    {
        var file = new FileModel("/repo/scripts/001_Init.sql", "scripts/001_Init.sql", ".sql");
        var outcome = new MustMatchNameAssertion(@"^scripts/\d{3}_.+\.sql$").Evaluate(file, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_ForConstructorCandidate_NameIsAlwaysNull()
    {
        var constructor = new ConstructorModel(Accessibility.Public, [], [], "Contoso.Domain.Order", "Contoso.Domain", "Order.cs", 1, 1);
        var outcome = new MustMatchNameAssertion("^.+$").Evaluate(constructor, EmptyModel);
        Assert.False(outcome.Passed);
    }

    [Fact]
    public void Evaluate_MatchesAgainstProjectName()
    {
        var project = TestModels.Project("Contoso.Domain");
        var outcome = new MustMatchNameAssertion("^Contoso\\..+$").Evaluate(project, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_MatchesAgainstMethodName()
    {
        var method = new MethodModel("Save", "System.Void", [], Accessibility.Public, MethodModifiers.None, [], "Contoso.Domain.Order", "Contoso.Domain", "Order.cs", 1, 1);
        var outcome = new MustMatchNameAssertion("^Save$").Evaluate(method, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_MatchesAgainstPropertyName()
    {
        var property = new PropertyModel("Name", "System.String", Accessibility.Public, true, true, null, false, false, false, [], "Contoso.Domain.Order", "Contoso.Domain", "Order.cs", 1, 1);
        var outcome = new MustMatchNameAssertion("^Name$").Evaluate(property, EmptyModel);
        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_MatchesAgainstFieldName()
    {
        var field = new FieldModel("_name", "System.String", Accessibility.Private, FieldModifiers.None, [], "Contoso.Domain.Order", "Contoso.Domain", "Order.cs", 1, 1);
        var outcome = new MustMatchNameAssertion("^_name$").Evaluate(field, EmptyModel);
        Assert.True(outcome.Passed);
    }
}
