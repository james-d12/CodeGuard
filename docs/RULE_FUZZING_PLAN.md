# Combinatorial / property-based fuzz testing for rule.yaml

## Context

CodeGuard's rule engine accepts declarative YAML rules built from a large, extensible
vocabulary — 21 selector kinds, 46 assertion kinds, 11 analyzer kinds, 3 conditions,
each with typed parameters (`String`, `Glob`, `Regex`, `Bool`, `Int`, `StringList`,
`Enum`, and recursive `Selector`/`AssertionList` types used by quantifier assertions).
Today this vocabulary is only exercised by 125 hand-written example rules plus their
hand-written `tests:` blocks — real coverage, but bounded by what a human thought to
write. Nothing currently generates *combinations* the engine's own authors didn't
anticipate, and nothing fuzzes adversarial parameter values (pathological globs,
catastrophic-backtracking regexes, empty lists, extreme ints). The goal is a test suite
that generates random-but-structurally-valid rule documents from the engine's own
capability catalog, runs them through the real pipeline, and asserts nothing crashes
that shouldn't.

This is greenfield: no property-based testing library, no random-generation
infrastructure, and no fuzzing of any kind exists in the repo today (confirmed by
repo-wide grep). Decisions made up front: **CsCheck** as the generation library, **full
scope in v1** (structural fuzzing + adversarial glob/regex values + `TestSetupBuilder`
setup-JSON fuzzing + embedded-`tests:` consistency, all together, not phased), and
**small iteration count in the existing CI job, with a larger nightly/manual soak**
layered on top later.

The payoff: because the generator walks `CapabilityCatalog.Create()` rather than a
hardcoded list, it self-updates whenever a new selector/assertion/analyzer is
registered — the fuzz suite's coverage grows automatically with the engine's own
vocabulary, with no separate maintenance step.

## Design

### Where facts come from (verified directly, not just derived from docs)

- `CandidateKind` enum (`src/CodeGuard.Configuration/Capabilities/CapabilityDescriptor.cs`)
  has **16** values: `Repository, Project, Type, Method, Property, Constructor, Field,
  File, Directory, CallSite, Switch, ThrowSite, MutationSite, TryBlock,
  MethodBodyShape, Diagnostic`.
- `rule.schema.json` is deliberately loose — it enforces document *shape* (single-key
  assertion objects, `oneOf` of `{target+assertions}` vs `{analyzer}`, recursive
  `when` node) but never enumerates valid `kind` strings or parameter shapes. Kind and
  parameter validity is enforced one layer down, at parse time, by
  `SelectorParserRegistry`/`AssertionParserRegistry`/`AnalyzerParserRegistry`
  (`RuleDocumentParser.Parse` throws `RuleParsingException` with a `RuleErrorCodes`
  value on failure). **A generator must consult the catalog, not just the schema, to
  produce parseable rules.**
- `GlobMatcher` (`src/CodeGuard.Evaluation/GlobMatcher.cs`) is `internal`, visible only
  to `CodeGuard.Evaluation.Tests` via that project's `InternalsVisibleTo`. Direct,
  isolated fuzzing of `GlobMatcher.IsMatch` must live in that existing test project,
  not the new one — the new project can only exercise glob patterns indirectly, as
  selector/assertion parameter values run through the full pipeline.
- `RuleEvaluator.Evaluate` (`src/CodeGuard.Core/Evaluation/RuleEvaluator.cs`) wraps each
  rule's evaluation in `try/catch (Exception ex) when (ex is not
  OperationCanceledException)`, converting any uncaught exception into one
  `RuleEvaluationError` added to `ValidationResult.EvaluationErrors` (status becomes
  `PartiallyEvaluated`). **This is the primary crash oracle**: a well-formed generated
  rule run against a valid model must produce zero `RuleEvaluationError`s.
  `EvaluateRule` (no per-rule wrapper) is used by `rules test`/single-rule paths and
  does not catch — raw exceptions propagate there instead.
- Mismatched selector/assertion `CandidateKind` pairing (`Produces` vs `AppliesTo`) is
  not prevented anywhere upstream — it's legal to construct. Each assertion instead
  defensively type-checks its candidate and returns `AssertionOutcome.Failure(...)`
  (not a throw) on mismatch, so a mismatched pairing manifests as "every candidate
  violates," not a crash. `RuleSetAnalyzer.FindUnreachableAssertions`
  (`src/CodeGuard.Configuration/Analysis/RuleSetAnalyzer.cs`) already flags exactly
  this at the top level via static `Produces`/`AppliesTo` comparison, but — per its own
  doc comment — does **not** recurse into nested selectors inside quantifier
  assertions (`must_all_match`/`must_any_match`/`must_none_match`). That's a real,
  documented blind spot worth encoding as an explicit, monitored invariant rather than
  leaving it a silent gap.
- `TestSetupBuilder.Build(JsonObject)` (`src/CodeGuard.Configuration/Testing/TestSetupBuilder.cs`)
  is production code (used by `rules test`) that builds a synthetic `RepositoryModel`
  from JSON, covering all 16 `CandidateKind`s without Roslyn/disk — this is the natural
  way to get virtual models for the fuzz harness, and can itself be fuzzed with
  adversarial setup JSON.
- `RuleTestRunner.Run(RuleDefinition)` (`src/CodeGuard.Configuration/Testing/RuleTestRunner.cs`)
  runs a rule's own embedded `tests:` block — reusable as-is for the embedded-tests
  consistency check.
- Existing wiring pattern to copy: `RuleFileLoaderTests.CreateLoader()`
  (`tests/CodeGuard.Configuration.Tests/RuleFileLoaderTests.cs`) shows how to build a
  `RuleFileLoader` from `DefaultParsers.CreateSelectorRegistry()/CreateAssertionRegistry()/
  CreateConditionRegistry()`, `DefaultAnalyzers.CreateRegistry()`, and
  `RuleSchemaValidator.CreateDefault()`.
- Test stack: xUnit 2.9.3, `Microsoft.NET.Test.Sdk` 18.9.0, net10.0, matching every
  other test project in the solution.

### New project: `tests/CodeGuard.RuleFuzzing.Tests`

Added to `CodeGuard.sln`, referencing `CodeGuard.Configuration` and `CodeGuard.Core`
(everything else flows in transitively), plus a new `CsCheck` PackageReference.

```
tests/CodeGuard.RuleFuzzing.Tests/
  Generation/
    RuleFuzzOptions.cs            # depth/probability/iteration-count knobs
    GenerationMode.cs             # PairingMode {Compatible, Incompatible, Mixed}; ValueMode {Benign, Adversarial}
    AdversarialCorpus.cs          # fixed lists: catastrophic regexes, glob edge cases, extreme ints
    ParameterValueGenerator.cs    # one CsCheck Gen<T> per ParameterType
    SelectorGenerator.cs          # recursive selector-node builder (bounded depth)
    AssertionGenerator.cs         # recursive assertion-node builder; applies pairing mode
    AnalyzerRuleGenerator.cs      # analyzer-shaped rule bodies
    RuleDocumentGenerator.cs      # top-level assembly: id/name/target/when/assertions or analyzer
    EmbeddedTestGenerator.cs      # generates a matching setup: + rule + expect: pass/fail together
    SchemaViolationGenerator.cs   # narrow: deliberate schema-shape breaks, for the schema oracle only
  Models/
    RepositoryModelFixtures.cs    # one virtual RepositoryModel per CandidateKind (via TestSetupBuilder) + one "kitchen sink"
  Oracles/
    PipelineHarness.cs            # registries + RuleSchemaValidator + CapabilityCatalog + RuleEvaluator, built once
    KnownParameterConstraints.cs  # allow-listed InvalidParameter cases the catalog doesn't encode (e.g. must_have_count needs ≥1 of min/max/exactly)
  RuleGenerationFuzzTests.cs           # schema-validity + parseability + zero-RuleEvaluationError oracles (compatible mode)
  UnreachableRuleCrossCheckFuzzTests.cs # static UnreachableRules vs dynamic always-fails agreement (incompatible mode), including the documented nested-mismatch gap
  TestSetupBuilderFuzzTests.cs         # adversarial setup: JSON against TestSetupBuilder.Build directly
  EmbeddedTestConsistencyFuzzTests.cs  # RuleTestRunner.Run vs independent RuleEvaluator.Evaluate agreement on jointly-generated setup+rule+expect
  SchemaViolationFuzzTests.cs          # RuleSchemaValidator-only oracle
README.md                              # how to replay a failing seed locally
```

A second, small addition to the **existing** `tests/CodeGuard.Evaluation.Tests`
project: `GlobMatcherFuzzTests.cs`, adding its own `CsCheck` PackageReference, feeding
`AdversarialCorpus`-style glob/regex patterns directly at the internal
`GlobMatcher.IsMatch`, since that type is only visible there.

### Generation strategy

`RuleDocumentGenerator` walks a `CapabilityCatalog.Create()` instance (never a
hardcoded kind list) to build a `JsonObject` shaped exactly like a parsed rule
document. For each parameter, `ParameterValueGenerator` produces values per
`ParameterType`, in two modes:

- **Benign**: realistic-looking values (short globs like `Contoso.*.Entities`, small
  non-negative ints, simple regexes, one of `AllowedValues` for enums).
- **Adversarial**: a fixed corpus mixed in probabilistically — bare `**`, `?` repeated
  50×, empty/very long strings, regex metacharacters that aren't glob metacharacters
  (to check `Regex.Escape` correctness), catastrophic-backtracking regex shapes
  (`(a+)+$`, `(a|a)*$`), invalid regex syntax, `0`/negative/`int.MaxValue`, empty
  arrays for `StringList`/`AssertionList` (a deliberate "should this be rejected"
  probe), near-miss enum values.

Selector/assertion trees recurse through `Selector`/`AssertionList`-typed parameters
(used by quantifiers) up to `RuleFuzzOptions.MaxNestingDepth` (default 3), after which
generation is structurally forced to leaf-safe kinds (no further `Selector`/
`AssertionList` parameters) so termination doesn't depend on probability alone.

`AssertionGenerator` implements the **pairing mode**: `Compatible` only picks
assertions whose `AppliesTo` contains the selector's `Produces` (or is empty);
`Incompatible` deliberately picks a mismatched one; `Mixed` (the default for the main
soak) draws `Incompatible` a configurable minority of the time. This applies
recursively inside nested quantifier assertions too.

### Oracles

1. **Schema-validity** — every non-schema-violation generated document must pass
   `RuleSchemaValidator.CreateDefault().Validate(...)` without throwing.
2. **Parseability** — `RuleDocumentParser.Parse(...)` must succeed, or throw
   `RuleParsingException` with a code in the small, explicit
   `KnownParameterConstraints` allow-list (documented cross-parameter constraints the
   catalog's `Required`/type metadata alone doesn't capture, e.g. `must_have_count`
   needing at least one of min/max/exactly). Anything else is a bug — either the
   generator produced something it shouldn't, or the engine crashed unexpectedly.
3. **Zero-crash evaluation (primary oracle)** — for compatible-pairing rules, run
   `RuleEvaluator.Evaluate` against every fixture in `RepositoryModelFixtures` and
   assert `EvaluationErrors.Count == 0`. Also assert re-running is idempotent (no
   hidden mutable state).
4. **Static/dynamic agreement** — for incompatible-pairing rules that parse
   successfully: top-level mismatches must appear in `RuleSetAnalyzer`'s
   `UnreachableRules`, and dynamically must produce a violation on every applicable
   candidate (or zero candidates, which is vacuously fine). For nested mismatches
   inside quantifiers, assert the dynamic "always fails" behavior *and* explicitly
   assert `RuleSetAnalyzer` does **not** flag it (documented current limitation) — so
   a future fix to that recursion gap is caught by this test needing an update, not by
   the gap silently persisting unnoticed.
5. **`TestSetupBuilder` robustness** — adversarial `setup:` JSON either builds a valid
   `RepositoryModel` or fails with an expected exception type; no unexpected crash.
6. **Embedded-test consistency** — for jointly-generated setup+rule+`expect:` cases,
   `RuleTestRunner.Run` and an independent `RuleEvaluator.Evaluate` against the same
   generated setup must agree on pass/fail.

### Library choice: CsCheck

`Gen<T>` combinators compose naturally with the recursive catalog walk, integrate as
plain `[Fact]` methods (no new attribute vocabulary, unlike FsCheck's `[Property]`),
carry no F#Core-style transitive dependency, and give shrinking for free — a failing
4-level-deep generated rule gets automatically minimized to a small counterexample
without hand-written `Shrink()` logic. On failure it prints a replayable seed; capture
that seed via `ITestOutputHelper` on every run so CI failures are always locally
reproducible, and dump the failing `JsonObject` itself so a maintainer can turn it into
a permanent regression test (mirroring `RuleFileLoaderTests`' existing hand-written
style) without re-running the fuzzer.

### CI integration

Default `dotnet test` (already in `ci.yml`): low iteration count (e.g. 200–500
generated documents per oracle test), which given this is all in-process with virtual
models should add low single-digit seconds — no separate CI step needed, it rides
along with the existing solution-wide `dotnet test` run. Layer a
`workflow_dispatch`/scheduled job on top later that raises iteration count via an env
var (e.g. `RULEFUZZ_ITERATIONS`) read by `RuleFuzzOptions`, reusing the same test
methods — no separate test code.

## Critical files

- `src/CodeGuard.Configuration/Capabilities/CapabilityCatalog.cs`,
  `CapabilityDescriptor.cs` — the vocabulary the generator walks.
- `src/CodeGuard.Configuration/Parsing/RuleDocumentParser.cs`,
  `DefaultParsers.cs` — parse-time validity, registry construction pattern.
- `src/CodeGuard.Configuration/Testing/TestSetupBuilder.cs`,
  `RuleTestRunner.cs` — virtual model construction and embedded-test execution.
- `src/CodeGuard.Core/Evaluation/RuleEvaluator.cs` — the crash-to-`RuleEvaluationError`
  boundary that is the primary oracle.
- `src/CodeGuard.Configuration/Analysis/RuleSetAnalyzer.cs` — `UnreachableRules` cross-check.
- `src/CodeGuard.Evaluation/GlobMatcher.cs` — `internal`, fuzz target lives in
  `CodeGuard.Evaluation.Tests` instead of the new project.
- `tests/CodeGuard.Configuration.Tests/RuleFileLoaderTests.cs` — wiring pattern to
  replicate in `PipelineHarness`.
- `.github/workflows/ci.yml` — where the new project rides along, and where a later
  nightly/manual soak job would be added.

## Verification

- `dotnet build` — 0 errors/warnings, including the new project.
- `dotnet test tests/CodeGuard.RuleFuzzing.Tests` and
  `dotnet test tests/CodeGuard.Evaluation.Tests --filter GlobMatcherFuzzTests` pass
  locally at the default (low) iteration count.
- Manually bump `RuleFuzzOptions`' iteration count (or the env var once wired) to a much
  higher value (e.g. 20,000+) for a local deep soak before considering v1 done —
  this is the actual point of the exercise, and default CI-time counts are too low to
  trust as the only signal that the generator/oracles work.
- Any failure found during the deep soak: confirm it reproduces via the captured seed,
  then either fix the underlying engine bug (and add the shrunk case as a permanent
  regression test in the relevant existing test project) or add a narrowly-scoped entry
  to `KnownParameterConstraints`/`AdversarialCorpus` if the "failure" is actually
  expected, documented behavior — never silently loosen an oracle without recording why.
- `dotnet test` (whole solution) still passes and existing CI timing is not
  meaningfully affected — check the new project's local wall-clock time before wiring
  into `ci.yml`.
