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
    public void IsMatch_ReturnsExpectedResult(string value, string pattern, bool expected)
    {
        Assert.Equal(expected, GlobMatcher.IsMatch(value, pattern));
    }
}
