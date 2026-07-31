namespace CodeGuard.Cli.Support;

/// <summary>Decides the actual file path a --output value resolves to, so --output can name a
/// directory (a default filename derived from --format is appended) as well as an exact file path.
/// A pure function of its inputs (directory-existence is passed in rather than checked directly)
/// so directory-vs-file-path resolution can be unit-tested deterministically.</summary>
public static class ReportOutputPathResolver
{
    public static string? Resolve(string? output, string format, bool outputIsExistingDirectory)
    {
        if (output is null)
        {
            return null;
        }

        var looksLikeDirectory = outputIsExistingDirectory
            || output.EndsWith(Path.DirectorySeparatorChar)
            || output.EndsWith(Path.AltDirectorySeparatorChar);

        return looksLikeDirectory ? Path.Combine(output, DefaultFileName(format)) : output;
    }

    private static string DefaultFileName(string format) => format switch
    {
        "json" => "validation-report.json",
        "sarif" => "validation-report.sarif",
        "html" => "validation-report.html",
        _ => "validation-report.txt"
    };
}
