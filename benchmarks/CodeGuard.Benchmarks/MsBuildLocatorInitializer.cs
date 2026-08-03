using System.Runtime.CompilerServices;
using Microsoft.Build.Locator;

namespace CodeGuard.Benchmarks;

/// <summary>Registers MSBuildLocator exactly once when this assembly loads - see the identical
/// rationale in CodeGuard.IntegrationTests.MsBuildLocatorInitializer.</summary>
internal static class MsBuildLocatorInitializer
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        if (!MSBuildLocator.IsRegistered)
        {
            MSBuildLocator.RegisterDefaults();
        }
    }
}
