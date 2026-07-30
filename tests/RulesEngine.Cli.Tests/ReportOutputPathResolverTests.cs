using RulesEngine.Cli.Support;

namespace RulesEngine.Cli.Tests;

public class ReportOutputPathResolverTests
{
    [Fact]
    public void NullOutput_ResolvesToNull()
    {
        var path = ReportOutputPathResolver.Resolve(null, "html", outputIsExistingDirectory: false);

        Assert.Null(path);
    }

    [Fact]
    public void ExactFilePath_IsReturnedUnchanged()
    {
        var path = ReportOutputPathResolver.Resolve("report.html", "html", outputIsExistingDirectory: false);

        Assert.Equal("report.html", path);
    }

    [Theory]
    [InlineData("html", "validation-report.html")]
    [InlineData("json", "validation-report.json")]
    [InlineData("sarif", "validation-report.sarif")]
    [InlineData("console", "validation-report.txt")]
    public void ExistingDirectory_AppendsDefaultFilenameForFormat(string format, string expectedFileName)
    {
        var path = ReportOutputPathResolver.Resolve("./reports", format, outputIsExistingDirectory: true);

        Assert.Equal(Path.Combine("./reports", expectedFileName), path);
    }

    [Fact]
    public void TrailingSlash_IsTreatedAsDirectory_EvenIfItDoesNotExistYet()
    {
        var path = ReportOutputPathResolver.Resolve("./reports/", "html", outputIsExistingDirectory: false);

        Assert.Equal(Path.Combine("./reports/", "validation-report.html"), path);
    }
}
