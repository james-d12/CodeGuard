using CodeGuard.Cli.Support;

namespace CodeGuard.Cli.Tests;

public class RepositoryUrlNormalizerTests
{
    [Theory]
    [InlineData("https://github.com/acme/app.git", "https://github.com/acme/app")]
    [InlineData("https://github.com/acme/app/", "https://github.com/acme/app")]
    [InlineData("https://github.com/acme/app", "https://github.com/acme/app")]
    [InlineData("https://user:ghp_secret@github.com/acme/app.git", "https://github.com/acme/app")]
    [InlineData("https://x-access-token@github.com/acme/app", "https://github.com/acme/app")]
    [InlineData("https://user:p@ss@host.example.com/org/repo.git", "https://host.example.com/org/repo")]
    [InlineData("https://host.example.com/org/repo.git?token=abc#frag", "https://host.example.com/org/repo")]
    [InlineData("ssh://git@host.example.com:2222/org/repo.git", "ssh://host.example.com:2222/org/repo")]
    [InlineData("git@github.com:acme/app.git", "ssh://github.com/acme/app")]
    [InlineData("github.com:acme/app", "ssh://github.com/acme/app")]
    [InlineData("  https://github.com/acme/app.git  ", "https://github.com/acme/app")]
    [InlineData("/srv/git/repo.git", "/srv/git/repo")]
    [InlineData("C:\\repos\\app", "C:\\repos\\app")]
    public void Normalize_RemovesCredentialsAndDecoration(string raw, string expected)
    {
        Assert.Equal(expected, RepositoryUrlNormalizer.Normalize(raw));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Normalize_EmptyInput_ReturnsNull(string? raw)
    {
        Assert.Null(RepositoryUrlNormalizer.Normalize(raw));
    }
}
