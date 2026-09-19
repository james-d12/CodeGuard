using CodeGuard.Configuration.Validation;
using CodeGuard.RuleFuzzing.Tests.Generation;
using CodeGuard.RuleFuzzing.Tests.Oracles;
using CsCheck;
using Xunit.Abstractions;

namespace CodeGuard.RuleFuzzing.Tests;

/// <summary>
/// A document that is catalog-valid except for one deliberate schema-shape break must be rejected by
/// <c>RuleSchemaValidator</c> with exactly <see cref="RuleSchemaValidationException"/> - never an
/// uncaught exception of any other type.
/// </summary>
public sealed class SchemaViolationFuzzTests(ITestOutputHelper output)
{
    private static readonly PipelineHarness Harness = new();

    [Fact]
    public void MalformedDocuments_AreRejectedByRuleSchemaValidator()
    {
        var gen = SchemaViolationGenerator.AnyViolation(Harness.Catalog);
        var iterations = RuleFuzzOptions.Iterations("RULEFUZZ_ITERATIONS_SCHEMA", 2000);

        gen.Sample(document =>
        {
            Assert.Throws<RuleSchemaValidationException>(() => Harness.ValidateSchema(document));
        }, writeLine: output.WriteLine, iter: iterations, threads: 1);
    }
}
