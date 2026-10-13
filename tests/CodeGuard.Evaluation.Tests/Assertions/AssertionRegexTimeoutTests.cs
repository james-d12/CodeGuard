using CodeGuard.Analysis.AnalysisModel;
using CodeGuard.Evaluation.Assertions;
using CodeGuard.RuleModel.Assertions;

namespace CodeGuard.Evaluation.Tests.Assertions;

/// <summary>
/// Every assertion that matches a rule-author-supplied regex must give up after
/// <see cref="AssertionRegex.MatchTimeout"/> rather than hang on a catastrophically backtracking
/// pattern. "^(a+)+$" against a run of 'a's followed by a non-matching character is exponential in
/// the run length, so without a timeout these tests would never finish.
/// </summary>
public class AssertionRegexTimeoutTests
{
    private const string CatastrophicPattern = "^(a+)+$";
    private static readonly string PathologicalInput = new string('a', 40) + "_";
    private static readonly RepositoryModel EmptyModel = new(".", [], [], [], [], [], [], [], [], []);

    public static TheoryData<string, IAssertion, object> Cases => new()
    {
        {
            "must_match_content",
            new MustMatchContentAssertion(CatastrophicPattern),
            new FileModel("virtual/a.txt", "a.txt", ".txt", PathologicalInput)
        },
        {
            "must_not_match_content",
            new MustNotMatchContentAssertion(CatastrophicPattern),
            new FileModel("virtual/a.txt", "a.txt", ".txt", PathologicalInput)
        },
        {
            "must_match_name",
            new MustMatchNameAssertion(CatastrophicPattern),
            TestModels.Type($"Contoso.Domain.{PathologicalInput}")
        },
        {
            "must_match_namespace_pattern",
            new MustMatchNamespacePatternAssertion(CatastrophicPattern),
            TestModels.Type($"{PathologicalInput}.Order")
        },
        {
            "must_match_argument",
            new MustMatchArgumentAssertion(0, CatastrophicPattern),
            new CallSiteModel(
                Kind: CallSiteKind.Invocation, InvokedMember: "Log", TargetTypeName: null,
                ContainingMethod: "Run", ContainingType: "Worker", ProjectName: "Contoso.Domain",
                Arguments: [new CallSiteArgument(0, PathologicalInput, IsLiteral: true)],
                FilePath: "Worker.cs", Line: 1, Column: 1)
        }
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void Evaluate_ThrowsRegexAssertionTimeoutException_OnCatastrophicBacktracking(
        string kind, IAssertion assertion, object candidate)
    {
        var ex = Assert.Throws<RegexAssertionTimeoutException>(() => assertion.Evaluate(candidate, EmptyModel));

        Assert.Equal(kind, ex.AssertionKind);
        Assert.Equal(CatastrophicPattern, ex.Pattern);
        Assert.Contains($"'{kind}'", ex.Message);
        Assert.IsAssignableFrom<TimeoutException>(ex);
    }
}
