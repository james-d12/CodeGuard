using RulesEngine.Cli.Support;

namespace RulesEngine.Cli.Tests;

public class ColorSupportTests
{
    [Fact]
    public void NoColorOption_WinsOverColorOption()
    {
        var useColor = ColorSupport.ShouldUseColor(
            colorOption: true,
            noColorOption: true,
            writingToFile: false,
            consoleOutputRedirected: false,
            noColorEnvVar: null);

        Assert.False(useColor);
    }

    [Fact]
    public void WritingToFile_WinsOverColorOption()
    {
        var useColor = ColorSupport.ShouldUseColor(
            colorOption: true,
            noColorOption: false,
            writingToFile: true,
            consoleOutputRedirected: false,
            noColorEnvVar: null);

        Assert.False(useColor);
    }

    [Fact]
    public void ColorOption_WinsOverNoColorEnvVarAndRedirection()
    {
        var useColor = ColorSupport.ShouldUseColor(
            colorOption: true,
            noColorOption: false,
            writingToFile: false,
            consoleOutputRedirected: true,
            noColorEnvVar: "1");

        Assert.True(useColor);
    }

    [Fact]
    public void NoColorEnvVar_DisablesColor_WhenNoFlagsGiven()
    {
        var useColor = ColorSupport.ShouldUseColor(
            colorOption: false,
            noColorOption: false,
            writingToFile: false,
            consoleOutputRedirected: false,
            noColorEnvVar: "1");

        Assert.False(useColor);
    }

    [Fact]
    public void AutoMode_UsesColor_WhenNotRedirectedAndNotWritingToFile()
    {
        var useColor = ColorSupport.ShouldUseColor(
            colorOption: false,
            noColorOption: false,
            writingToFile: false,
            consoleOutputRedirected: false,
            noColorEnvVar: null);

        Assert.True(useColor);
    }

    [Fact]
    public void AutoMode_DisablesColor_WhenConsoleOutputRedirected()
    {
        var useColor = ColorSupport.ShouldUseColor(
            colorOption: false,
            noColorOption: false,
            writingToFile: false,
            consoleOutputRedirected: true,
            noColorEnvVar: null);

        Assert.False(useColor);
    }
}
