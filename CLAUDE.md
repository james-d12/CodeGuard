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
dotnet test                     # 81 tests across 6 test projects, all should pass
dotnet test tests/RulesEngine.Evaluation.Tests   # run a single test project
dotnet test --filter "FullyQualifiedName~MustInheritFromAssertionTests"  # run a single test class/method

# CLI (AssemblyName=rules-engine), run against this repo's own rules/ under RuleEngine.sln:
dotnet run --project src/RulesEngine.Cli -- list-rules
dotnet run --project src/RulesEngine.Cli -- explain-rule DDD-ENTITY-001
dotnet run --project src/RulesEngine.Cli -- list-standards
dotnet run --project src/RulesEngine.Cli -- validate   # see Known limitation below — self-validation crashes
```

CI (`.github/workflows/ci.yml`) runs `dotnet restore && dotnet build --no-restore && dotnet test --no-build`
on `ubuntu-latest` for push/PR to `main`. `global.json` pins the SDK to `10.0.100` (`rollForward: latestFeature`).

## Architecture

### Dependency graph between core projects

```
RulesEngine.Analysis  (no dependencies — pure model + IAnalysisProvider abstraction)
  ^
  |-- RulesEngine.RuleModel  (selector/assertion/condition interfaces; depends on Analysis)
  |     ^
  |     |-- RulesEngine.Evaluation  (concrete selectors/assertions; depends on RuleModel + Analysis)
  |     |-- RulesEngine.Core        (RuleEvaluator; depends on RuleModel + Analysis, NOT Evaluation)
  |
  |-- RulesEngine.Analyzers.Roslyn   (depends on Analysis only; pure Roslyn, no MSBuild)
  |     ^
  |     |-- RulesEngine.Analyzers.MSBuild (depends on Analysis + Analyzers.Roslyn + Microsoft.CodeAnalysis.Workspaces.MSBuild)
  |
  |-- RulesEngine.Analyzers.Repository (depends on Analysis only; pure filesystem walk, no Roslyn/MSBuild)

RulesEngine.Reporting     depends on Core (transitively RuleModel, for Severity in SARIF level mapping)
                          + Sarif.Sdk package (SarifViolationReporter); System.Text.Json only for Json reporter
RulesEngine.Configuration depends on Analysis + RuleModel + Evaluation (needs concrete selector/assertion
                          classes to construct from YAML — there is no intermediate DTO layer)
RulesEngine.Cli           depends on everything (Core, RuleModel, Analysis, Evaluation, Reporting,
                          Configuration, Analyzers.MSBuild, Analyzers.Repository)
```

Keep this dependency direction intact — e.g. `RulesEngine.Core` must never depend on `Evaluation`,
and `Analysis` must never depend on Roslyn/MSBuild.

### Pipeline

`validate` composes: `RepositoryFileProvider` + `MsBuildAnalysisProvider` (in that order) build an
`AnalysisModel` (repository/project/type data, provider-agnostic) → `RuleEvaluator`
(`RulesEngine.Core`) runs each `RuleDefinition`'s `ITargetSelector` against the model, then each
`IAssertion` against matched targets → violations go through `IViolationReporter`
(`RulesEngine.Reporting`: Console/Json/Sarif).

`RuleDefinition` holds **executable interface instances directly** (`ITargetSelector Target`,
`IReadOnlyList<IAssertion> Assertions`, `IConditionNode? When`), not separate "Definition" DTOs
resolved later — `RulesEngine.Configuration.Parsing` builds these directly from YAML via
`SelectorParserRegistry`/`AssertionParserRegistry`. Keep this consistent if you extend the schema.

### Adding a new selector/assertion

Every selector/assertion needs **both** a concrete class in `RulesEngine.Evaluation` and a YAML
parser registered in `RulesEngine.Configuration/Parsing/DefaultParsers.cs` — it isn't usable from a
rule file until both exist. See the table in `docs/IMPLEMENTATION_STATUS.md` ("Selectors and
assertions implemented") for the current `kind` → class → parser-params mapping.

All pattern matching (namespaces, base types, project names) goes through
`RulesEngine.Evaluation.GlobMatcher` (`*` wildcard only), **not** exact string equality — this
matters because Roslyn renders a closed generic base type as `Entity<int>`, not the open
`Entity<TId>` placeholder used when authoring a rule, so rules must use `Entity<*>`.

There is currently no YAML parsing for `when`/`and`/`or`/`not` — `AndCondition`/`OrCondition`/
`NotCondition` exist and are unit-tested in `RulesEngine.RuleModel.Conditions` but nothing wires
them into `RuleDocumentParser` or `rules/schema/rule.schema.json` yet. Adding that requires a new
`ConditionParserRegistry`.

### Rules directory

`rules/` holds this repo's own starter rule set (YAML, all tagged `illustrative: true`,
`Contoso.*` namespaces), organized by standard (`ddd/`, `architecture/`, `csharp/`).
`rules/schema/rule.schema.json` is the JSON Schema (2020-12) rules are validated against.
`.rulesengine/config.yml` configures repository discovery (where rules/skills/agents/source/tests
live) — discovery is deliberately configurable per-repo, missing paths are skipped silently.

**Never add `rules/` content to a packable project.** `RulesEngine.Cli` is published publicly to
nuget.org as a `dotnet tool` (see `Directory.Build.props`/`RulesEngine.Cli.csproj` for
`PackAsTool`), and some of this repo's rule content is derived from real company conventions —
only `rules/schema/rule.schema.json` (already embedded as a resource in
`RulesEngine.Configuration`) may travel with the packaged tool. Do not add `rules/` as
`<Content>`/`<None>`/`<EmbeddedResource>` to `RulesEngine.Cli` or any other packable project;
`scripts/verify-nupkg-contents.sh` enforces this in CI before publishing.

### Known limitation — CLI self-analysis

`MsBuildAnalysisProvider` used to crash reliably when analyzing this tool's own solution
(`RuleEngine.sln`) because of a Buildalyzer-specific bug (see history below) — that crash is now
**fixed** (confirmed by running `rules-engine validate` against `RuleEngine.sln` after the
Buildalyzer→`MSBuildWorkspace` migration). However, `validate` against `RuleEngine.sln` still
doesn't complete end-to-end: it now reaches much further into the pipeline and crashes in
`NoPureDelegationOverrideAnalyzer.Analyze` (`ToDictionary(t => t.FullName)` assumes a type's
`FullName` is unique across the whole repository, but every test project here gets an SDK-generated
`AutoGeneratedProgram` stub type with an identical name, so multiple test projects collide on the
same dictionary key). This is a pre-existing bug in the analyzer's global-uniqueness assumption,
unrelated to MSBuild/Buildalyzer — it was simply never reached before because the old crash always
happened first. Not yet fixed; needs the analyzer to key by `(ProjectName, FullName)` or similar
rather than `FullName` alone. Still does not affect validating any other repo without this
naming collision.

### Package version pins

`RulesEngine.Analyzers.Roslyn.csproj` / `RulesEngine.Analyzers.MSBuild.csproj` use
`Microsoft.CodeAnalysis.CSharp(.Workspaces)` **5.6.0** (latest), matched by
`Microsoft.CodeAnalysis.Workspaces.MSBuild` 5.6.0 in `RulesEngine.Analyzers.MSBuild.csproj` — keep
these in the same Roslyn generation to avoid `TypeLoadException`s. `RulesEngine.Analyzers.MSBuild.csproj`
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
- MSBuildLocator must be registered exactly once per process. `RulesEngine.IntegrationTests` does
  this via a single `[ModuleInitializer]` (`MsBuildLocatorInitializer.cs`) rather than per-class
  static constructors, because xUnit runs test classes in one assembly in parallel by default and
  independent check-then-act registrations race.
- The SARIF NuGet package is `Sarif.Sdk`, not `Microsoft.CodeAnalysis.Sarif` (that's just the C#
  namespace it exposes).
