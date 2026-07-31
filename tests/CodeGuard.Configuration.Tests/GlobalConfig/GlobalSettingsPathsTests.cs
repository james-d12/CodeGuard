using System.Runtime.InteropServices;
using CodeGuard.Configuration.GlobalConfig;

namespace CodeGuard.Configuration.Tests.GlobalConfig;

public class GlobalSettingsPathsTests
{
    [Fact]
    public void ResolveRoot_UsesAppDataOnWindows()
    {
        var root = GlobalSettingsPaths.ResolveRoot(
            OSPlatform.Windows,
            name => name == "APPDATA" ? @"C:\Users\jamie\AppData\Roaming" : null,
            homeDirectory: @"C:\Users\jamie");

        Assert.Equal(Path.Combine(@"C:\Users\jamie\AppData\Roaming", "CodeGuard"), root);
    }

    [Fact]
    public void ResolveRoot_FallsBackToHomeAppDataOnWindows_WhenAppDataUnset()
    {
        var root = GlobalSettingsPaths.ResolveRoot(OSPlatform.Windows, _ => null, homeDirectory: @"C:\Users\jamie");

        Assert.Equal(Path.Combine(@"C:\Users\jamie", "AppData", "Roaming", "CodeGuard"), root);
    }

    [Fact]
    public void ResolveRoot_UsesNativeLibraryPathOnMacOs()
    {
        var root = GlobalSettingsPaths.ResolveRoot(OSPlatform.OSX, _ => null, homeDirectory: "/Users/jamie");

        Assert.Equal(Path.Combine("/Users/jamie", "Library", "Application Support", "CodeGuard"), root);
    }

    [Fact]
    public void ResolveRoot_UsesXdgConfigHomeOnLinux_WhenSet()
    {
        var root = GlobalSettingsPaths.ResolveRoot(
            OSPlatform.Linux,
            name => name == "XDG_CONFIG_HOME" ? "/home/jamie/.xdgconfig" : null,
            homeDirectory: "/home/jamie");

        Assert.Equal(Path.Combine("/home/jamie/.xdgconfig", "codeguard"), root);
    }

    [Fact]
    public void ResolveRoot_FallsBackToDotConfigOnLinux_WhenXdgConfigHomeUnset()
    {
        var root = GlobalSettingsPaths.ResolveRoot(OSPlatform.Linux, _ => null, homeDirectory: "/home/jamie");

        Assert.Equal(Path.Combine("/home/jamie", ".config", "codeguard"), root);
    }

    [Fact]
    public void RulesCacheDirectory_IsDeterministic_ForTheSameUrl()
    {
        var first = GlobalSettingsPaths.RulesCacheDirectory("/root", "https://github.com/org/rules-repo.git");
        var second = GlobalSettingsPaths.RulesCacheDirectory("/root", "https://github.com/org/rules-repo.git");

        Assert.Equal(first, second);
    }

    [Fact]
    public void RulesCacheDirectory_Differs_ForDifferentUrls()
    {
        var first = GlobalSettingsPaths.RulesCacheDirectory("/root", "https://github.com/org/rules-repo.git");
        var second = GlobalSettingsPaths.RulesCacheDirectory("/root", "https://github.com/other-org/rules-repo.git");

        Assert.NotEqual(first, second);
    }
}
