namespace RulesEngine.Cli.Support;

/// <summary>Decides whether console output should be colorized. A pure function of its inputs
/// (no direct Console/Environment access) so precedence between --color/--no-color, file
/// redirection, NO_COLOR, and terminal auto-detection can be unit-tested deterministically.</summary>
public static class ColorSupport
{
    public static bool ShouldUseColor(
        bool colorOption,
        bool noColorOption,
        bool writingToFile,
        bool consoleOutputRedirected,
        string? noColorEnvVar)
    {
        if (noColorOption)
        {
            return false;
        }

        if (writingToFile)
        {
            return false;
        }

        if (colorOption)
        {
            return true;
        }

        if (!string.IsNullOrEmpty(noColorEnvVar))
        {
            return false;
        }

        return !consoleOutputRedirected;
    }
}
