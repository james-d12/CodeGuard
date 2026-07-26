using RulesEngine.Analysis.AnalysisModel;
using RulesEngine.Evaluation.Assertions;

namespace RulesEngine.Evaluation.Tests.Assertions;

public class MustInheritFromAssertionTests
{
    private static readonly RepositoryModel EmptyModel = new(".", [], []);

    [Fact]
    public void Evaluate_Passes_WhenBaseTypeMatches()
    {
        var assertion = new MustInheritFromAssertion("Contoso.Domain.Entity<TId>");
        var type = CreateType(baseType: "Contoso.Domain.Entity<TId>");

        var outcome = assertion.Evaluate(type, EmptyModel);

        Assert.True(outcome.Passed);
    }

    [Fact]
    public void Evaluate_Fails_WhenBaseTypeDoesNotMatch()
    {
        var assertion = new MustInheritFromAssertion("Contoso.Domain.Entity<TId>");
        var type = CreateType(baseType: null);

        var outcome = assertion.Evaluate(type, EmptyModel);

        Assert.False(outcome.Passed);
        Assert.Contains("must inherit from", outcome.Message);
    }

    private static TypeModel CreateType(string? baseType) => new(
        Name: "LegacyThing",
        FullName: "Contoso.Domain.Entities.LegacyThing",
        Namespace: "Contoso.Domain.Entities",
        Kind: TypeKind.Class,
        BaseType: baseType,
        Interfaces: [],
        Accessibility: Accessibility.Public,
        Modifiers: TypeModifiers.None,
        Attributes: [],
        Methods: [],
        Properties: [],
        Constructors: [],
        Fields: [],
        ProjectName: "Contoso.Domain",
        FilePath: "LegacyThing.cs",
        Line: 1,
        Column: 1);
}
