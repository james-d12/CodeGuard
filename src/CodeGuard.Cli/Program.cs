using System.CommandLine;
using Microsoft.Build.Locator;
using Microsoft.Extensions.Logging;
using CodeGuard.Cli.Commands;
using CodeGuard.Cli.Support;

using var bootstrapLoggerFactory = CliLoggerFactory.Create(LogLevel.Information);
var bootstrapLogger = bootstrapLoggerFactory.CreateLogger("CodeGuard.Cli.Program");

if (!MSBuildLocator.IsRegistered)
{
    try
    {
        MSBuildLocator.RegisterDefaults();
    }
    catch (InvalidOperationException ex)
    {
        bootstrapLogger.LogCritical(ex, "MSBuildLocator.RegisterDefaults failed - no .NET SDK/MSBuild install found.");
        await Console.Error.WriteLineAsync(
            "codeguard could not find a .NET SDK/MSBuild install on this machine. " +
            "Install the .NET SDK from https://dotnet.microsoft.com/download and try again.");
        return 1;
    }
}

var rootCommand = new RootCommand("Deterministic engineering rules and analysis engine");

rootCommand.Subcommands.Add(ValidateCommand.Build());
rootCommand.Subcommands.Add(RulesCommand.Build());
rootCommand.Subcommands.Add(SetupCommand.Build());
rootCommand.Subcommands.Add(InfoCommand.Build());

try
{
    // System.CommandLine's own default exception handler (EnableDefaultExceptionHandler, true by
    // default) would otherwise catch exceptions from command actions itself, print its own raw
    // "Unhandled exception: " + stack trace, and return 1 without rethrowing - making this catch
    // block dead code. Disabling it here lets exceptions actually reach the friendlier handling
    // below (defense-in-depth for anything not already caught inside a command's own action).
    var invocationConfiguration = new InvocationConfiguration { EnableDefaultExceptionHandler = false };
    return await rootCommand.Parse(args).InvokeAsync(invocationConfiguration);
}
catch (Exception ex)
{
    bootstrapLogger.LogCritical(ex, "codeguard terminated unexpectedly: {ExceptionType}: {Message}", ex.GetType().Name, ex.Message);
    await Console.Error.WriteLineAsync($"codeguard: unexpected error: {ex.Message}");
    return 1;
}
