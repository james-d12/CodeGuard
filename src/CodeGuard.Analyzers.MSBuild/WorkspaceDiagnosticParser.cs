using System.Text.RegularExpressions;

namespace CodeGuard.Analyzers.MSBuild;

/// <summary>
/// Turns a raw <c>WorkspaceDiagnostic.Message</c> from MSBuildWorkspace into a project path (when the
/// message names one) and a trimmed, single-purpose message - MSBuildWorkspace's project-load failures
/// embed the full failing project path plus, for build-task exceptions, the exception's entire
/// "   at Namespace.Type.Method(...)" call stack inline in the message text, which is noise for anyone
/// reading a report rather than debugging this engine.
/// </summary>
internal static partial class WorkspaceDiagnosticParser
{
    // MSBuildWorkspace's own wording for "a project failed to load", e.g.:
    // "Msbuild failed when processing the file '/path/Foo.csproj' with message: <details>"
    [GeneratedRegex(@"^Msbuild failed when processing the file '(?<path>[^']+)' with message:\s*(?<details>.*)$", RegexOptions.Singleline)]
    private static partial Regex ProjectLoadFailurePattern();

    // A stack frame line from Exception.ToString(), e.g. "   at Namespace.Type.Method(...)". Diagnostic
    // messages fold the exception's newlines into runs of whitespace, so this matches on the "  at "
    // marker rather than line boundaries.
    [GeneratedRegex(@"\s{2,}at\s+[A-Za-z_]")]
    private static partial Regex StackFrameMarker();

    public static (string? ProjectPath, string Message) Parse(string rawMessage)
    {
        var match = ProjectLoadFailurePattern().Match(rawMessage);
        if (!match.Success)
        {
            return (null, TrimStackFrames(rawMessage));
        }

        return (match.Groups["path"].Value, TrimStackFrames(match.Groups["details"].Value));
    }

    private static string TrimStackFrames(string message)
    {
        var stackFrame = StackFrameMarker().Match(message);
        var trimmed = stackFrame.Success ? message[..stackFrame.Index] : message;
        return trimmed.Trim();
    }
}
