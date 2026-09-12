# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A deterministic analysis/validation engine for enforcing an organisation's engineering standards
(DDD, architecture layering, C# conventions, etc.) against .NET repositories, intended to sit
alongside AI coding agents as a machine-checkable guardrail: agents load applicable rules before
generating code, then this engine validates the result and reports structured violations for the
agent to remediate. Rules are authored as declarative YAML (target selector + assertions), not
C# code, so new organisational rules can be added without touching the engine. Design rationale
lives in `docs/PRIMITIVES.md` (original design doc — do not edit) and `docs/REFACTORING.md` (a
separate, much larger architectural-evolution proposal that has **not been started** — read it
before proposing further architectural changes, but treat it as a distinct initiative). Full build
history/decisions/gotchas from the initial 8-PR implementation are in `docs/IMPLEMENTATION_STATUS.md`
— read it before making non-trivial changes, it has context not reconstructable from code alone.

## Commands

```bash
dotnet build                    # 0 errors, 0 warnings expected
dotnet test                     # 748 tests across 7 test projects, all should pass
dotnet test tests/CodeGuard.Evaluation.Tests   # run a single test project
dotnet test --filter "FullyQualifiedName~MustInheritFromAssertionTests"  # run a single test class/method

# CLI (AssemblyName=codeguard). Commands are nested under a `rules` group, not flat.
# This repo's own rules live in examples/rules/, so most commands need --rules-source.
dotnet run --project src/CodeGuard.Cli -- rules list     --rules-source examples/rules
dotnet run --project src/CodeGuard.Cli -- rules explain  DDD-ENTITY-001 --rules-source examples/rules --format json
dotnet run --project src/CodeGuard.Cli -- rules validate --rules-source examples/rules
dotnet run --project src/CodeGuard.Cli -- rules test     --rules-source examples/rules  # embedded tests:, no repo/disk
dotnet run --project src/CodeGuard.Cli -- rules analyze  --rules-source examples/rules  # rule-set-level problems (missing tests, unreachable assertions, exact duplicates), no repo/disk
dotnet run --project src/CodeGuard.Cli -- rules discover --format json                  # engine's full selector/assertion/analyzer vocabulary, reads no rule files
dotnet run --project src/CodeGuard.Cli -- rules create   # interactive scaffolder, descriptor-driven (see rules discover)
dotnet run --project src/CodeGuard.Cli -- info
dotnet run --project src/CodeGuard.Cli -- validate       # self-validation completes end-to-end, see "Known limitation" below
```

CI (`.github/workflows/ci.yml`, single `build-and-test` job on `ubuntu-latest` for push/PR to
`main`) does more than build+test: `dotnet format --verify-no-changes`, a vulnerable-package check,
build+test wrapped in a SonarCloud scan (`dotnet-sonarscanner` — coverage is collected via
`coverlet`'s `XPlat Code Coverage` collector and reported to Sonar via
`sonar.cs.cobertura.reportsPaths`), `rules validate`/`rules test` against this repo's own
`examples/rules/`, a check that `scripts/sync-skill-references.sh` produces no diff (the skill's
reference tables are generated from the engine's capability descriptors — see "Rules directory"
below), `dotnet publish`, and an HTML/markdown coverage report attached to the job summary.
`global.json` pins the SDK to `10.0.100` (`rollForward: latestFeature`).

## Architecture

### Dependency graph between core projects

```
CodeGuard.Analysis  (no dependencies — pure model + IAnalysisProvider abstraction)
  ^
  |-- CodeGuard.RuleModel  (selector/assertion/condition interfaces; depends on Analysis)
  |     ^
  |     |-- CodeGuard.Evaluation  (concrete selectors/assertions; depends on RuleModel + Analysis)
  |     |-- CodeGuard.Core        (RuleEvaluator; depends on RuleModel + Analysis, NOT Evaluation)
  |
  |-- CodeGuard.Analyzers.Roslyn   (depends on Analysis only; pure Roslyn, no MSBuild)
  |     ^
  |     |-- CodeGuard.Analyzers.MSBuild (depends on Analysis + Analyzers.Roslyn + Microsoft.CodeAnalysis.Workspaces.MSBuild)
  |
  |-- CodeGuard.Analyzers.Repository (depends on Analysis only; pure filesystem walk, no Roslyn/MSBuild)

CodeGuard.Reporting     depends on Core (transitively RuleModel, for Severity in SARIF level mapping)
                          + Sarif.Sdk package (SarifViolationReporter); System.Text.Json only for Json reporter
CodeGuard.Configuration depends on Analysis + RuleModel + Evaluation (needs concrete selector/assertion
                          classes to construct from YAML — there is no intermediate DTO layer)
CodeGuard.Cli           depends on everything (Core, RuleModel, Analysis, Evaluation, Reporting,
                          Configuration, Analyzers.MSBuild, Analyzers.Repository)
```

Keep this dependency direction intact — e.g. `CodeGuard.Core` must never depend on `Evaluation`,
and `Analysis` must never depend on Roslyn/MSBuild.

### Pipeline

`validate` composes: `RepositoryFileProvider` + `MsBuildAnalysisProvider` (in that order) build an
`AnalysisModel` (repository/project/type data, provider-agnostic) → `RuleEvaluator`
(`CodeGuard.Core`) runs each `RuleDefinition`'s `ITargetSelector` against the model, then each
`IAssertion` against matched targets → violations go through `IViolationReporter`
(`CodeGuard.Reporting`: Console/Json/Sarif).

`RuleDefinition` holds **executable interface instances directly** (`ITargetSelector Target`,
`IReadOnlyList<IAssertion> Assertions`, `IConditionNode? When`), not separate "Definition" DTOs
resolved later — `CodeGuard.Configuration.Parsing` builds these directly from YAML via
`SelectorParserRegistry`/`AssertionParserRegistry`. Keep this consistent if you extend the schema.

### Adding a new selector/assertion

Every selector/assertion needs **both** a concrete class in `CodeGuard.Evaluation` and a YAML
parser registered in `CodeGuard.Configuration/Parsing/DefaultParsers.cs` — it isn't usable from a
rule file until both exist. See the table in `docs/IMPLEMENTATION_STATUS.md` ("Selectors and
assertions implemented") for the current `kind` → class → parser-params mapping.

All pattern matching (namespaces, base types, project names) goes through
`CodeGuard.Evaluation.GlobMatcher` (`*` wildcard only), **not** exact string equality — this
matters because Roslyn renders a closed generic base type as `Entity<int>`, not the open
`Entity<TId>` placeholder used when authoring a rule, so rules must use `Entity<*>`.

YAML parsing for `when`/`and`/`or`/`not` is implemented: `AndCondition`/`OrCondition`/`NotCondition`
(`CodeGuard.RuleModel.Conditions`, unit-tested) are wired up via `ConditionParserRegistry`
(`CodeGuard.Configuration.Parsing`), which `RuleDocumentParser` consults to populate
`RuleDefinition.When`, and `RuleEvaluator` filters candidates against it before running assertions.
`rule.schema.json` has a recursive `whenNode` `$def` for it. A bare assertion `kind` (e.g.
`must_inherit_from`) can be used directly as a `when:` leaf via `AssertionCondition`. `Any`/`All`/
`None` quantifiers over a *set* of candidates (as opposed to `And`/`Or`/`Not`, which combine
conditions for a single candidate) are also implemented, as assertion kinds rather than conditions:
`must_all_match`/`must_any_match`/`must_none_match` (`CodeGuard.Evaluation.Assertions`) run a nested
`assertions:` list against every match of a nested `selector:`.

### Rules directory

`examples/rules/` holds this repo's own rule set — 125 YAML files organized by area (`ddd/`,
`architecture/`, `csharp/`, `persistence/`, `reporting/`, …). There is **no** root `rules/`
directory, so CLI commands against this repo need `--rules-source examples/rules`.

The JSON Schema (2020-12) rules are validated against lives at
`src/CodeGuard.Configuration/Validation/Schemas/rule.schema.json` and is embedded as a resource.
`skills/codeguard-rule-generation/references/rule-schema.json` is a copy for the authoring skill;
`scripts/sync-skill-references.sh` keeps the two identical (don't hand-edit the skill's copy — CI
runs the script and diffs `skills/`, so a manual edit there just gets overwritten/flagged). `.codeguard/config.yml`
configures repository discovery (where rules/skills/agents/source/tests live) — discovery is
deliberately configurable per-repo, missing paths are skipped silently.

117 of the 125 rules carry an embedded `tests:` block run by `codeguard rules test` against a
virtual analysis model (no disk, no Roslyn/MSBuild) — see `docs/RULES_TEST_DESIGN.md`. The 8 without
are analyzer-backed rules, which the virtual setup path can't drive. CI runs `rules validate` and
`rules test` over `examples/rules` on every build, so a broken rule fails the build.

**Never add `examples/rules/` content to a packable project.** `CodeGuard.Cli` is published publicly
to nuget.org as a `dotnet tool` (see `Directory.Build.props`/`CodeGuard.Cli.csproj` for
`PackAsTool`), and some of this repo's rule content is derived from real company conventions — only
the embedded `rule.schema.json` may travel with the packaged tool. Do not add `examples/rules/` as
`<Content>`/`<None>`/`<EmbeddedResource>` to `CodeGuard.Cli` or any other packable project;
`scripts/verify-nupkg-contents.sh` enforces this in CI before publishing.

### Capability descriptors and the AI-assisted authoring tooling

Every selector/assertion/analyzer parser (`CodeGuard.Configuration.Parsing`) implements a
`CapabilityDescriptor Descriptor` property (kind, summary, parameters, plus — for selectors/
assertions — the `CandidateKind` produced/accepted). `CapabilityCatalog.Create()`
(`CodeGuard.Configuration.Capabilities`) aggregates all of them; a test
(`CapabilityCatalogTests`) fails the build if a parser and its descriptor drift apart. This
backs `rules discover` (prints the engine's actual vocabulary; `--format markdown` is what
`scripts/sync-skill-references.sh` consumes to regenerate the skill's reference tables above) and
`rules analyze` (rule-set-level checks: missing/one-sided tests, unreachable assertions — an
assertion whose `AppliesTo` doesn't include its target selector's `Produces` — and exact-duplicate
rules). Adding a new selector/assertion/analyzer means adding its `Descriptor` too, or
`CapabilityCatalogTests` fails. See `docs/HIGH_LEVEL_AI_ASSISTING.md` for the design rationale and
`docs/IMPLEMENTATION_STATUS.md` for full build detail on this and `metadata.source` (optional rule
provenance — `document`/`section`/`statement`, surfaced by `rules explain --format json`).

### Known limitation — CLI self-analysis (resolved)

`MsBuildAnalysisProvider` used to crash reliably when analyzing this tool's own solution because of
a Buildalyzer-specific bug — fixed by the Buildalyzer→`MSBuildWorkspace` migration. A second, unrelated
crash then surfaced further into the pipeline in `NoPureDelegationOverrideAnalyzer.Analyze`:
`ToDictionary(t => t.FullName)` assumed a type's `FullName` is unique across the whole repository,
but every test project gets an SDK-generated `AutoGeneratedProgram` stub type with an identical
name, so multiple test projects collided on the same dictionary key. **This is now fixed** — the
analyzer keys by `(ProjectName, FullName)` instead of `FullName` alone, and has a regression test
covering the multi-project collision.

A sibling analyzer, `ImmutableMutationAnalyzer`, had the exact same "FullName is globally unique"
assumption (via a `HashSet<string>` instead of `ToDictionary`, so it silently misattributed
violations across projects instead of crashing) — also fixed the same way, also regression-tested.

`codeguard validate` against this repo's own `CodeGuard.sln` now completes end-to-end with zero
evaluation errors (confirmed empirically, not just by inspection). One more thing had to be fixed to
get there: `SolutionFileLocator`'s directory-skip list didn't exclude `.claude` — a Claude Code git
worktree checkout can live at `.claude/worktrees/...`, so a plain `codeguard validate --path .` was
discovering that nested duplicate of this same repo as a second solution and re-tripping the
`(ProjectName, FullName)`-uniqueness assumption one level up, across (not within) solutions. `.claude`
is now in the skip list alongside `bin`/`obj`/`.git`/etc.

Residual caveat, not yet hit in practice: `(ProjectName, FullName)` is unique within one solution
and, empirically, across this repo's own solutions, but nothing guarantees it across an arbitrary
multi-solution repo where the same project name legitimately appears in two different `.sln` files
on disk (not a duplicate worktree — a real repo layout). Not a known failure, just an unproven edge
case worth keeping in mind if a similar collision resurfaces elsewhere.

### Package version pins

`CodeGuard.Analyzers.Roslyn.csproj` / `CodeGuard.Analyzers.MSBuild.csproj` use
`Microsoft.CodeAnalysis.CSharp(.Workspaces)` **5.6.0** (latest), matched by
`Microsoft.CodeAnalysis.Workspaces.MSBuild` 5.6.0 in `CodeGuard.Analyzers.MSBuild.csproj` — keep
these in the same Roslyn generation to avoid `TypeLoadException`s. `CodeGuard.Analyzers.MSBuild.csproj`
also has `Microsoft.Build`/`Microsoft.Build.Framework` PackageReferences at `17.11.48`
(`ExcludeAssets="runtime" PrivateAssets="all"`) required by `Microsoft.Build.Locator`'s own
build-time check (`MSBL001`) — if `MSBL001` fires after a package bump, add/adjust exactly the
package+version it names; don't guess in advance.

### Other gotchas worth knowing before touching this code

- `JsonSchema.FromText` throws if called twice with the same `$id` in one process — `RuleSchemaValidator`
  uses a `static Lazy<JsonSchema>`; don't remove that caching.
- `System.CommandLine` here is the **3.0 preview API**, not the 2.0 beta API most docs/LLM knowledge
  cover: `command.SetAction(async (parseResult, ct) => ...)`, `rootCommand.Subcommands.Add(...)`,
  `rootCommand.Parse(args).InvokeAsync()`. `Option<string[]>` supports repeated flags
  (`--rule A --rule B`) but not space-separated multi-value syntax.
- MSBuildLocator must be registered exactly once per process. `CodeGuard.IntegrationTests` does
  this via a single `[ModuleInitializer]` (`MsBuildLocatorInitializer.cs`) rather than per-class
  static constructors, because xUnit runs test classes in one assembly in parallel by default and
  independent check-then-act registrations race.
- The SARIF NuGet package is `Sarif.Sdk`, not `Microsoft.CodeAnalysis.Sarif` (that's just the C#
  namespace it exposes).
