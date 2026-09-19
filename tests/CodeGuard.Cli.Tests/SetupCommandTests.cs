using CodeGuard.Cli.Commands;
using CodeGuard.Configuration.GlobalConfig;

namespace CodeGuard.Cli.Tests;

/// <summary>Covers `codeguard setup` end-to-end via its System.CommandLine `Command`.</summary>
[Collection(ConsoleOutputCollection.Name)]
public sealed class SetupCommandTests
{
    [Fact]
    public async Task Run_DirectorySource_SucceedsAndPersistsSettings()
    {
        using var globalSettings = new IsolatedGlobalSettingsScope();
        var rulesDir = Directory.CreateTempSubdirectory("codeguard-setup-rules-").FullName;
        try
        {
            var (exitCode, output) = await RunSetup(["--source", rulesDir, "--type", "directory"]);

            Assert.Equal(0, exitCode);
            Assert.Contains("Configured rules source", output);

            var settings = GlobalSettingsStore.Load(GlobalSettingsPaths.SettingsFilePath(GlobalSettingsPaths.ResolveRoot()));
            Assert.NotNull(settings);
            Assert.Equal(RuleSourceKind.Directory, settings!.Kind);
        }
        finally
        {
            Directory.Delete(rulesDir, recursive: true);
        }
    }

    [Fact]
    public async Task Run_MissingDirectory_FailsCleanly()
    {
        using var globalSettings = new IsolatedGlobalSettingsScope();
        var missingDir = Path.Combine(Path.GetTempPath(), "codeguard-setup-missing-" + Guid.NewGuid());

        var (exitCode, _, errorOutput) = await RunSetupCapturingError(["--source", missingDir, "--type", "directory"]);

        Assert.Equal(1, exitCode);
        Assert.Contains("was not found", errorOutput);
    }

    [Fact]
    public async Task Run_TypeManaged_CreatesManagedDirectoryAndSucceeds()
    {
        using var globalSettings = new IsolatedGlobalSettingsScope();

        var (exitCode, output) = await RunSetup(["--type", "managed"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Configured rules source", output);

        var settings = GlobalSettingsStore.Load(GlobalSettingsPaths.SettingsFilePath(GlobalSettingsPaths.ResolveRoot()));
        Assert.NotNull(settings);
        Assert.Equal(RuleSourceKind.Directory, settings!.Kind);
        Assert.True(Directory.Exists(settings.Location));
    }

    [Fact]
    public async Task Run_SourceCombinedWithTypeManaged_IsRejected()
    {
        using var globalSettings = new IsolatedGlobalSettingsScope();
        var rulesDir = Directory.CreateTempSubdirectory("codeguard-setup-conflict-").FullName;
        try
        {
            var (exitCode, _, errorOutput) = await RunSetupCapturingError(["--source", rulesDir, "--type", "managed"]);

            Assert.Equal(1, exitCode);
            Assert.Contains("--source cannot be combined with --type managed", errorOutput);
        }
        finally
        {
            Directory.Delete(rulesDir, recursive: true);
        }
    }

    [Fact]
    public async Task Run_EmptySource_IsRejected()
    {
        using var globalSettings = new IsolatedGlobalSettingsScope();

        var (exitCode, _, errorOutput) = await RunSetupCapturingError(["--source", "   ", "--type", "directory"]);

        Assert.Equal(1, exitCode);
        Assert.Contains("A rules source is required", errorOutput);
    }

    [Fact]
    public async Task Run_BareInteractive_NothingConfigured_StartFreshCreatesManagedDirectory()
    {
        using var globalSettings = new IsolatedGlobalSettingsScope();

        var (exitCode, output) = await RunSetupInteractive([], "3\n");

        Assert.Equal(0, exitCode);
        Assert.Contains("Configured rules source", output);

        var settings = GlobalSettingsStore.Load(GlobalSettingsPaths.SettingsFilePath(GlobalSettingsPaths.ResolveRoot()));
        Assert.NotNull(settings);
        Assert.Equal(RuleSourceKind.Directory, settings!.Kind);
        Assert.True(Directory.Exists(settings.Location));
    }

    private static async Task<(int ExitCode, string Output)> RunSetup(IReadOnlyList<string> args)
    {
        var originalOut = Console.Out;
        var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            var exitCode = await SetupCommand.Build().Parse(args.ToArray()).InvokeAsync();
            return (exitCode, writer.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static async Task<(int ExitCode, string Output, string ErrorOutput)> RunSetupCapturingError(IReadOnlyList<string> args)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        var outWriter = new StringWriter();
        var errorWriter = new StringWriter();
        Console.SetOut(outWriter);
        Console.SetError(errorWriter);
        try
        {
            var exitCode = await SetupCommand.Build().Parse(args.ToArray()).InvokeAsync();
            return (exitCode, outWriter.ToString(), errorWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    private static async Task<(int ExitCode, string Output)> RunSetupInteractive(IReadOnlyList<string> args, string stdin)
    {
        var originalOut = Console.Out;
        var originalIn = Console.In;
        var writer = new StringWriter();
        Console.SetOut(writer);
        Console.SetIn(new StringReader(stdin));
        try
        {
            var exitCode = await SetupCommand.Build().Parse(args.ToArray()).InvokeAsync();
            return (exitCode, writer.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetIn(originalIn);
        }
    }
}
