using System.Runtime.CompilerServices;
using Microsoft.Build.Locator;

namespace RulesEngine.IntegrationTests;

/// <summary>
/// Registers MSBuildLocator exactly once when this assembly loads. A module initializer is used
/// instead of a per-test-class static constructor because xUnit runs test classes in the same
/// assembly in parallel by default - two independent static constructors each guarded by
/// `if (!MSBuildLocator.IsRegistered)` can race and throw "MSBuild assemblies were already loaded".
/// </summary>
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
