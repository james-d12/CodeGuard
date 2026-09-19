using System.Text.Json.Nodes;
using CodeGuard.Configuration.Parsing;
using CodeGuard.Configuration.Testing;
using CodeGuard.RuleFuzzing.Tests.Generation;
using CsCheck;
using Xunit.Abstractions;

namespace CodeGuard.RuleFuzzing.Tests;

/// <summary>
/// Fuzzes <see cref="TestSetupBuilder.Build"/> directly with adversarial <c>setup:</c> JSON - the same
/// production code path `codeguard rules test` uses to build a virtual <c>RepositoryModel</c>, so a
/// crash here is a crash any rule author's embedded test case could trigger by hand. The main risk this
/// targets is type confusion: every field reader (<c>GetRequiredString</c>/<c>GetOptionalInt</c>/
/// <c>.AsObject()</c>/<c>.AsArray()</c>) assumes a specific JSON value kind and throws a raw
/// <see cref="InvalidOperationException"/>/<see cref="FormatException"/> from <c>System.Text.Json</c>
/// when handed the wrong one - unlike a missing required field, which cleanly throws
/// <see cref="RuleParsingException"/>.
/// </summary>
public sealed class TestSetupBuilderFuzzTests(ITestOutputHelper output)
{
    private static readonly string[] KnownSetupKeys =
    [
        "projects", "types", "files", "callSites", "switches", "throwSites", "mutationSites",
        "tryBlocks", "methodBodyShapes", "diagnostics", "directories"
    ];

    // Deliberately wrong-shaped values for whatever key they land under - a string where an array or
    // object is expected, a number where a string is expected, an array where a scalar is expected.
    private static readonly Gen<JsonNode?> MalformedValue = Gen.OneOf(
        Gen.Bool.Select(_ => (JsonNode?)"not-an-object-or-array"),
        Gen.Bool.Select(_ => (JsonNode?)12345),
        Gen.Bool.Select(_ => (JsonNode?)true),
        Gen.Bool.Select(_ => (JsonNode?)new JsonArray("unexpected-string-item")),
        Gen.Bool.Select(_ => (JsonNode?)new JsonArray(12345)),
        Gen.Bool.Select(_ => (JsonNode?)new JsonObject()),
        Gen.Bool.Select(_ => (JsonNode?)new JsonArray()));

    private static readonly Gen<JsonObject> AdversarialSetup =
        Gen.OneOfConst(KnownSetupKeys).Array[1, 4]
            .SelectMany(keys => MalformedValue.Array[keys.Length, keys.Length]
                .Select(values =>
                {
                    var setup = new JsonObject();
                    for (var i = 0; i < keys.Length; i++)
                    {
                        setup[keys[i]] = values[i];
                    }

                    return setup;
                }));

    [Fact]
    public void Build_OnAdversarialSetup_EitherSucceedsOrFailsCleanly()
    {
        var iterations = RuleFuzzOptions.Iterations("RULEFUZZ_ITERATIONS_SETUP", 500);

        AdversarialSetup.Sample(setup =>
        {
            try
            {
                TestSetupBuilder.Build(setup);
            }
            catch (RuleParsingException)
            {
                // Expected: TestSetupBuilder validates unknown keys and required fields this way.
            }
        }, writeLine: output.WriteLine, iter: iterations, threads: 1);
    }
}
