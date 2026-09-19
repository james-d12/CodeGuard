# CodeGuard.RuleFuzzing.Tests

Combinatorial/property-based fuzz testing for the rule engine. See `docs/RULE_FUZZING_PLAN.md` for the
full design. In short: `Generation/RuleDocumentGenerator` walks the engine's own
`CapabilityCatalog.Create()` (every registered selector/assertion/analyzer kind and its parameters) to
build random-but-structurally-valid rule documents, mixing benign and adversarial parameter values, and
runs them through the real pipeline (`RuleSchemaValidator` → `RuleDocumentParser` → `RuleSetAnalyzer` →
`RuleEvaluator`) looking for crashes or inconsistencies. Because it's catalog-driven, the fuzzer's
coverage grows automatically whenever a new selector/assertion/analyzer is registered - no separate
maintenance step.

## Running

```bash
dotnet test tests/CodeGuard.RuleFuzzing.Tests                    # default iteration counts (CI-fast)
dotnet test tests/CodeGuard.Evaluation.Tests --filter GlobMatcherFuzzTests
```

## Deeper local soaks

Every fuzz `[Fact]` reads its iteration count from an environment variable (default is low, for the
normal `dotnet test` run). Raise it locally for a deeper soak:

```bash
RULEFUZZ_ITERATIONS=20000 \
RULEFUZZ_ITERATIONS_ANALYZER=10000 \
RULEFUZZ_ITERATIONS_UNREACHABLE=5000 \
RULEFUZZ_ITERATIONS_NESTED_UNREACHABLE=5000 \
RULEFUZZ_ITERATIONS_SETUP=20000 \
RULEFUZZ_ITERATIONS_SCHEMA=5000 \
RULEFUZZ_ITERATIONS_EMBEDDED_TEST=5000 \
  dotnet test tests/CodeGuard.RuleFuzzing.Tests
```

## Reproducing a failure

CsCheck prints a seed on failure:

```
Set seed: "0000018ab..." or -e CsCheck_Seed=0000018ab... to reproduce (N shrinks, ... skipped, ... total).
```

Re-running the same `dotnet test` command with `CsCheck_Seed=<seed>` set in the environment replays the
exact same generation sequence, already shrunk to a minimal failing case. The failure message also
includes the full JSON of the generated rule document, so a fix can be verified without re-running the
fuzzer at all - paste it into a `[Fact]` as a permanent regression test in the relevant existing test
project (mirroring `RuleFileLoaderTests`' style) once the underlying bug is fixed.

## Project layout

- `Generation/` - the catalog-driven generator (`RuleDocumentGenerator`, `SelectorGenerator`,
  `AssertionGenerator`, `ParameterValueGenerator`, `AnalyzerRuleGenerator`, `SchemaViolationGenerator`),
  plus `AdversarialCorpus` (fixed edge-case values) and `JsonObjectGenExtensions` (the
  `Gen<JsonObject>`-builder helpers - see that file's doc comment on `NewObject()`/`FreshString()` for a
  non-obvious CsCheck gotcha: `Gen.Const(Func<T>)` is a *lazy constant*, evaluated once and reused, not
  a fresh value per generation - reusing a pre-built mutable `JsonNode` this way throws "the node
  already has a parent" the second time it's attached).
- `Models/RepositoryModelFixtures.cs` - one virtual `RepositoryModel` per `CandidateKind` (via the same
  `TestSetupBuilder` `codeguard rules test` uses) plus a "kitchen sink", built from raw setup JSON also
  exposed for reuse by `EmbeddedTestConsistencyFuzzTests`.
- `Oracles/PipelineHarness.cs` - the pipeline wired up once (mirrors `RuleFileLoaderTests.CreateLoader()`).
- `Oracles/KnownParameterConstraints.cs` - the small, explicit allow-list of *expected* rejections
  (cross-parameter constraints the catalog can't encode, e.g. `must_have_count` needing at least one of
  min/max/exactly; enum-parse failures using a different error code than other invalid-parameter
  failures; nested selector templates validating lazily at evaluation time rather than at parse time) -
  auditable documentation of exactly where the catalog's metadata alone isn't enough to guarantee
  parseability, distinct from an actual bug.
- Top-level `*FuzzTests.cs` - one file per oracle from the design doc.

All fuzz `Sample` calls run with `threads: 1`. The representative `RepositoryModel` fixtures are shared,
static objects reused across every generated case; running single-threaded keeps the idempotence and
consistency oracles honest about what they're actually testing (same rule, same model, called twice)
without a concurrent-access data race in shared fixture state as a confound.

## Bugs found and fixed while building this suite

- `MustHaveJsonFieldAssertion`/`MustNotHaveJsonFieldAssertion` crashed with an uncaught
  `JsonReaderException` when the target file's content wasn't valid JSON (a realistic case: the target
  selector doesn't have to be scoped to only `*.json` files). Fixed to treat unparseable content the
  same as "field not found".
- `TestSetupBuilder.Build` let a wrong JSON value kind for a known setup key (e.g. a number where an
  array was expected) escape as a raw `InvalidOperationException`/`FormatException` instead of a clean
  `RuleParsingException`. Fixed at the method's boundary rather than rewriting every individual field
  reader - see the doc comment on `Build`.
- `MemberOrderingAnalyzerParser` didn't reject a duplicate entry in `order`, letting
  `MemberOrderingAnalyzer`'s constructor throw a raw `ArgumentException` from
  `ToDictionary`. Fixed by validating for duplicates before construction.

## Known, deliberately-not-fixed findings

- Nested selectors inside `must_exist`/`must_not_exist`/`must_have_count`/the quantifier assertions are
  parsed lazily, at evaluation time (`SelectorTemplateResolver` + re-parse per candidate, to support
  `${Placeholder}` substitution) rather than eagerly at `RuleDocumentParser.Parse` time like a top-level
  target/assertion. A bad nested selector parameter therefore passes `codeguard rules validate` cleanly
  and only surfaces as a `RuleEvaluationError` the first time evaluation reaches it. Fixing this
  properly means detecting "no `${...}` placeholders present" and eagerly validating just that case - a
  real, separate design change, not something this suite attempts.
- Assertion-level regex parameters (`must_match_content`, `must_match_argument`,
  `must_match_namespace_pattern`) call `Regex.IsMatch(value, pattern)` with **no match timeout**, unlike
  `GlobMatcher` (100ms). A catastrophic-backtracking pattern here could hang evaluation indefinitely
  with no way to interrupt it, rather than throwing cleanly. Deliberately not exercised by this suite's
  live-evaluation fuzz loop (see `AdversarialCorpus`'s doc comment) since doing so risks hanging the test
  run itself rather than failing it. Worth a follow-up: give those call sites the same timeout treatment
  as `GlobMatcher`.
