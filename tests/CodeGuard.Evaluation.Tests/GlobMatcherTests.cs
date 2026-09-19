namespace CodeGuard.Evaluation.Tests;

public class GlobMatcherTests
{
    [Theory]
    [InlineData("Foo", "Foo", true)]
    [InlineData("Foo", "Bar", false)]
    [InlineData("", "*", true)]
    [InlineData("AnythingAtAll", "*", true)]
    [InlineData("Contoso.Domain.Order", "Contoso.*", true)]
    [InlineData("Contoso.Domain.Order", "*.Order", true)]
    [InlineData("Contoso.Domain.Order", "Contoso.*.Order", true)]
    [InlineData("Contoso.Domain.Order.V2", "Contoso.*.Order.*", true)]
    [InlineData("", "Foo", false)]
    [InlineData("foo", "Foo", false)]
    [InlineData("Contoso.Domain.OrderExtra", "Contoso.Domain.Order", false)]
    [InlineData("Foo(Bar)+.Baz", "Foo(Bar)+.Baz", true)]
    [InlineData("FooBarBarXBaz", "Foo(Bar)+.Baz", false)]
    // '*' matches within one path segment only - it does not cross '/'.
    [InlineData("src/Api/Program.cs", "*/Program.cs", false)]
    [InlineData("Api/Program.cs", "*/Program.cs", true)]
    [InlineData("src/Handlers/Foo.cs", "src/Handlers/*", true)]
    [InlineData("src/Handlers/Sub/Foo.cs", "src/Handlers/*", false)]
    // '**' as a whole segment matches zero or more full path segments.
    [InlineData("", "**", true)]
    [InlineData("a/b/c", "**", true)]
    [InlineData("Properties/launchSettings.json", "**/Properties/launchSettings.json", true)]
    [InlineData("src/Api/Properties/launchSettings.json", "**/Properties/launchSettings.json", true)]
    [InlineData("Other/launchSettings.json", "**/Properties/launchSettings.json", false)]
    [InlineData("src/Foo.cs", "src/**/*.cs", true)]
    [InlineData("src/a/b/Foo.cs", "src/**/*.cs", true)]
    [InlineData("other/Foo.cs", "src/**/*.cs", false)]
    [InlineData("Foo/a.cs", "Foo/**", true)]
    [InlineData("Foo/a/b.cs", "Foo/**", true)]
    [InlineData("Foo", "Foo/**", false)]
    [InlineData("Bar/a.cs", "Foo/**", false)]
    // '?' matches exactly one character, never '/'.
    [InlineData("Foo1.cs", "Foo?.cs", true)]
    [InlineData("Foo12.cs", "Foo?.cs", false)]
    [InlineData("Foo/1.cs", "Foo?1.cs", false)]
    public void IsMatch_ReturnsExpectedResult(string value, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(value, pattern));
    }
}
