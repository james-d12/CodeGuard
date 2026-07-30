# RuleEngine Implementation Status

This file is a handoff summary for picking up work on this repository in a new session/agent.
It was written after implementing PR1–PR7 of the approved implementation plan. **Read this
before making changes** — it captures context, decisions, and gotchas that aren't obvious from
the code alone.

## Essential reading (in this order)

1. `RuleEngine/PRIMITIVES.md` — the original design/requirements doc for this whole project.
2. The approved implementation plan: `/home/james/.claude/plans/reading-the-ruleengine-primitives-md-pla-imperative-quail.md`
   (11 sections: primitive vocabulary, starter rule set, analysis model, Roslyn/MSBuild
   integration, rule schema, repository discovery, project structure, CLI, results model, test
   strategy, and an 8-PR incremental plan). This file (`IMPLEMENTATION_STATUS.md`) tracks
   progress *against that plan* — the plan is still the source of truth for intent and design
   rationale; this file is the "what's actually been built and what's left" status report.
3. This file, for what's been built and what's left.

## Where things stand

**All 8 PRs of the plan are done**, including the optional PR8 fast-follow. All tests pass across
6 test projects. The solution builds with 0 errors and 0 warnings (Buildalyzer, which used to pull
in a transitive `System.Security.Cryptography.Xml` dependency triggering 14 `NU1903` advisory
warnings, has since been removed — see the updated gotcha #2 and #6 below).

**PR8 — CI workflow.** `.github/workflows/ci.yml` runs `dotnet restore` / `build` / `test` on
`ubuntu-latest` for pushes/PRs to `main` plus manual `workflow_dispatch`. A `global.json` pinning
the SDK to `10.0.100` (`rollForward: latestFeature`) was added alongside it so CI resolves the
same SDK feature band this was built and tested against — `actions/setup-dotnet@v4` reads it via
`global-json-file: global.json`. This workflow has **not been exercised on actual GitHub Actions**
(no remote configured) — only structurally validated (YAML parses, steps mirror the exact commands
verified manually throughout this project). Verify it end-to-end the first time this repo is
pushed to a GitHub remote.

The v1 plan is now fully implemented. The only outstanding, deliberately separate work is the
`RuleEngine/REFACTORING.md` architectural-evolution proposal — see below.

There is also a separate, much larger **architectural evolution** proposal in
`RuleEngine/REFACTORING.md` (Selector/Predicate/Assertion/Diagnostic separation, analysis
sessions with caching, rule versioning/lifecycle, a custom-analyzer escape hatch, rule fixture
testing, etc.). The user explicitly deferred that in favor of finishing PR7 first — it has **not**
been started. Read it before proposing any further architectural changes, but treat it as a
separate initiative from the PR1–PR8 plan, not something to blend into it opportunistically.

## Verifying the current state

```bash
cd /home/james/Dev/RuleEngine
dotnet build          # should succeed, 0 errors (14 pre-existing NU1903 advisory warnings, see above)
dotnet test           # should show 81 passed across 6 test projects, 0 failed
dotnet run --project RuleEngine/RulesEngine.Cli -- list-rules       # works against this repo's own rules/
dotnet run --project RuleEngine/RulesEngine.Cli -- explain-rule DDD-ENTITY-001
dotnet run --project RuleEngine/RulesEngine.Cli -- list-standards
dotnet run --project RuleEngine/RulesEngine.Cli -- validate   # see "Known limitation" below — self-validation still crashes
```

## Architecture overview

```
RuleEngine.sln
Directory.Build.props          # net10.0, Nullable enable, ImplicitUsings enable, LangVersion latest
global.json                    # pins SDK to 10.0.100 (rollForward: latestFeature) — read by CI (PR8)
.github/workflows/ci.yml       # dotnet restore/build/test on push/PR to main + workflow_dispatch (PR8)
.rulesengine/config.yml        # repository discovery config for THIS repo (PR6)

rules/                         # the 11 illustrative starter rules (YAML), all tagged illustrative: true
  ddd/                         # 7 files — entity, aggregate, event, command-handler rules
  architecture/                # 3 files — layering/package rules
  csharp/                      # 1 file — namespace convention rule
  schema/rule.schema.json      # JSON Schema (2020-12) for rule YAML files

RuleEngine/
  PRIMITIVES.md                 # original design doc — do not edit
  REFACTORING.md                # separate, much larger architectural-evolution proposal — not started, see above
  RulesEngine.Cli/               # System.CommandLine-based CLI (net10.0 exe, AssemblyName=rules-engine)
    Program.cs                  #   MSBuildLocator bootstrap + composes RootCommand from Commands/
    Commands/                   #   ValidateCommand, ListRulesCommand, ExplainRuleCommand, ListStandardsCommand
    Support/                    #   CliRepositoryContext (shared --path/--config resolution), CommonOptions
  RulesEngine.Core/              # RuleEvaluator, ValidationResult/Violation (Core.Evaluation, Core.Results)
  RulesEngine.RuleModel/         # RuleDefinition, Severity, EnforcementClassification;
                                 #   ITargetSelector/IAssertion/IConditionNode interfaces;
                                 #   AndCondition/OrCondition/NotCondition
  RulesEngine.Analysis/          # Provider-agnostic analysis model (RepositoryModel, ProjectModel,
                                 #   TypeModel, etc. in AnalysisModel/) + IAnalysisProvider,
                                 #   AnalysisModelBuilderContext, AnalysisModelBuilder (Providers/)
  RulesEngine.Evaluation/        # Concrete executable selectors/assertions (see table below) + GlobMatcher
  RulesEngine.Configuration/     # YAML rule loading/parsing/validation + repository discovery (see below)
  RulesEngine.Reporting/         # IViolationReporter + Console/Json/Sarif reporters (Console/, Json/, Sarif/)
  RulesEngine.Analyzers.Roslyn/  # RoslynTypeExtractor: CSharpCompilation -> IReadOnlyList<TypeModel>
  RulesEngine.Analyzers.MSBuild/ # MsBuildAnalysisProvider: MSBuildWorkspace + Microsoft.Build.Evaluation -> ProjectModel (+ Types via Roslyn)
  RulesEngine.Analyzers.Repository/ # RepositoryFileProvider: walks the filesystem -> FileModel (no Roslyn/MSBuild)

tests/
  RulesEngine.Core.Tests/            (9 tests)  — RuleEvaluator, Console/Json/Sarif violation reporters
  RulesEngine.Evaluation.Tests/      (41 tests) — every selector/assertion + And/Or/Not composition
  RulesEngine.Configuration.Tests/   (15 tests) — RuleFileLoader (incl. source-tracking), RepositoryDiscovery,
                                                    RulesEngineConfigLoader (incl. explicit --config path)
  RulesEngine.Analyzers.Roslyn.Tests/(12 tests) — RoslynTypeExtractor against in-memory source snippets
  RulesEngine.Analyzers.Repository.Tests/ (1 test) — RepositoryFileProvider walk + directory exclusions
  RulesEngine.IntegrationTests/      (3 tests)  — full pipeline against a real fixture solution, incl.
                                                    JSON/SARIF reporter output shape
    Fixtures/SimpleDomainSolution/   — real 3-project .sln (Contoso.Domain/Application/Infrastructure)
                                        used ONLY by MsBuildAnalysisProvider at test-time, not built by the main solution
```

### Dependency direction between the core projects

```
RulesEngine.Analysis  (no dependencies — pure model + provider abstraction)
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

RulesEngine.Reporting     depends on Core (transitively RuleModel, for Severity in the SARIF level mapping)
                          + Sarif.Sdk package (SarifViolationReporter; System.Text.Json only for Json one)
RulesEngine.Configuration depends on Analysis + RuleModel + Evaluation (needs concrete selector/assertion classes to construct from YAML)
RulesEngine.Cli           depends on everything (Core, RuleModel, Analysis, Evaluation, Reporting, Configuration,
                          Analyzers.MSBuild, Analyzers.Repository)
```

## Selectors and assertions implemented (v1 scope)

Every selector/assertion has both a concrete `RulesEngine.Evaluation` class and a
`RulesEngine.Configuration.Parsing` YAML parser, registered in `DefaultParsers.cs`. **Extend
both together** — a new assertion isn't usable from YAML until its parser is registered there.

| YAML `kind` | Evaluation class | Parser params |
|---|---|---|
| `class` (target) | `ClassInNamespaceSelector` | `namespace` |
| `type` (target) | `TypeSelector` | `namespace` (optional, default `*`) |
| `project` (target) | `ProjectSelector` | `name` |
| `inherits_from` (target) | `InheritsFromSelector` | `type` |
| `implements` (target) | `ImplementsSelector` | `interface` |
| `must_inherit_from` | `MustInheritFromAssertion` | `type` |
| `must_implement` | `MustImplementAssertion` | `interface` |
| `must_have_method` | `MustHaveMethodAssertion` | `name` |
| `must_have_property` | `MustHavePropertyAssertion` | `name` |
| `must_have_constructor` | `MustHaveConstructorAssertion` | `accessibility` (YAML array, e.g. `[private, protected]`) |
| `must_be_in_namespace` | `MustBeInNamespaceAssertion` | `pattern` |
| `must_be_in_project` | `MustBeInProjectAssertion` | `pattern` |
| `must_reference_package` | `MustReferencePackageAssertion` | `id` |
| `must_not_reference_package` | `MustNotReferencePackageAssertion` | `id` |
| `must_reference_project` | `MustReferenceProjectAssertion` | `name` |
| `must_not_reference_project` | `MustNotReferenceProjectAssertion` | `name` |
| `must_not_depend_on` | `MustNotDependOnAssertion` | `type` (scans base type/interfaces/method signatures) |

All pattern matching uses `RulesEngine.Evaluation.GlobMatcher` (only `*` wildcard supported, via
`Regex.Escape` + `.*` substitution). **Important:** patterns are matched with `GlobMatcher`, not
exact string equality — this matters for generic base types (see Gotcha #1 below).

`AndCondition`/`OrCondition`/`NotCondition` exist in `RulesEngine.RuleModel.Conditions` and are
unit-tested, but **there is no YAML parsing for `when`/`and`/`or`/`not` yet** — no starter rule
needs it, so it was deliberately deferred (not stubbed); PR7 didn't end up needing it either. If a
future rule needs it, you'll need to add a
`ConditionParserRegistry` in `RulesEngine.Configuration.Parsing` and wire `when:` parsing into
`RuleDocumentParser`, plus add `"when"` to `rules/schema/rule.schema.json`.

## CLI commands (PR7)

All four commands live in `RuleEngine/RulesEngine.Cli/Commands/`, share `--path`/`--config`
resolution via `Support/CliRepositoryContext.cs`, and are composed in `Program.cs`.

| Command | Options | Notes |
|---|---|---|
| `validate` | `--path`, `--config`, `--format console\|json\|sarif`, `--output <file>`, `--rule <id>` (repeatable), `--solution <path>` (repeatable), `--severity-threshold info\|warning\|error\|critical`, `--fail-on info\|warning\|error\|critical` | Providers run `[RepositoryFileProvider, MsBuildAnalysisProvider]` in that order. `SolutionFileLocator` (`Cli/Support/SolutionFileLocator.cs`) discovers `.sln` files recursively under `--path` (skipping `bin`/`obj`/`.git`/`.vs`/`.idea`/`node_modules`), analyzing **every** `.sln` found by default; `--solution` restricts to specific file(s) instead. `MsBuildAnalysisProvider` takes the resulting list and dedupes any project referenced by more than one solution (by project path) so it's only built/reported once, attributed to whichever solution is processed first. `--severity-threshold` filters the reported `ValidationResult` (recomputing `RulesPassed`/`RulesFailed`/`Status`); `--fail-on` (default `info`) independently decides the process exit code from what's left after that filter — both default to today's original behavior (any violation reported, any violation fails) when omitted. |
| `list-rules` | `--path`, `--config`, `--format table\|json`, `--tag` (repeatable, any-match), `--standard`, `--enabled-only` | Pure rule loading — no analysis model, no Roslyn/MSBuild, so this is cheap for an agent to call before generating code. |
| `explain-rule <ruleId>` | `--path`, `--config` | Uses `RuleFileLoader.LoadFromDirectoriesWithSource` to find the backing YAML file, then prints parsed metadata plus the **raw YAML source verbatim** (rather than trying to introspect configured selector/assertion parameters, which `ITargetSelector`/`IAssertion` don't expose beyond `Kind` — see decision #5 below). |
| `list-standards` | `--path`, `--config`, `--format table\|json` | Derives distinct `Standard` values purely from loaded rules' `standard` field — no separate standards-file format (as planned). |

`RulesEngineConfigLoader.LoadOrDefault` now has a two-argument overload
(`repoRoot, explicitConfigPath`) backing `--config`; the original one-argument overload still
exists and delegates to it with `null`.

### Post-v1 addition: `check-rules` and the `validate` rule-set pre-flight gate

Design doc: `docs/done/RULE_VALIDATION_PLAN.md`. Before this, a broken rule YAML file made
`RuleFileLoader.LoadFromFile` throw immediately (first error only, uncaught by any CLI command), so
`validate` against a repo with one bad rule file crashed with a raw .NET stack trace instead of a
clean report, and there was no way to check a folder of rule YAML in isolation.

- `RuleFileLoader` (`RulesEngine.Configuration/Loading/RuleFileLoader.cs`) gained a non-throwing core:
  `TryLoadFromFile` (schema-validate-then-parse, catching `RuleSchemaValidationException`/
  `RuleParsingException`/`RuleLoadException` into an error list instead of throwing) and
  `ValidateDirectories` (walks a rule-file set, collects **every** file's issues plus duplicate-ID
  conflicts into one `RuleSetValidationReport`, rather than stopping at the first one).
  `LoadFromFile`/`LoadFromDirectory(ies)(WithSource)` are unchanged in observable behavior (still
  throw on the first problem) but are now implemented on top of this non-throwing core, so there is
  a single parsing pass shared by every caller — no drift between "what `check-rules` approves" and
  "what `validate` actually loads."
- New `CliRepositoryContext.ValidateRules()` (`RulesEngine.Cli/Support/CliRepositoryContext.cs`)
  exposes `ValidateDirectories` for the configured rule paths, parallel to `LoadRules()`.
- New `check-rules` command (`RulesEngine.Cli/Commands/CheckRulesCommand.cs`): validates a rule set
  for structural correctness only (schema conformance, unknown selector/assertion/analyzer `kind`,
  `target`/`assertions` vs `analyzer` mutual exclusivity, duplicate rule IDs) with no analysis model
  and no MSBuild involved. Shares `--path`/`--config`/`--rules-source`/`--branch` resolution with
  every other command, so `--rules-source <folder>` points it directly at an ad-hoc rules folder.
  `--format console|json`; exit `0`/`1` on validity, no severity concept (these are authoring errors,
  not violations).
- `validate` (`ValidateCommand.cs`) now calls `context.ValidateRules()` unconditionally before
  building the `AnalysisModel`/touching `MsBuildAnalysisProvider`; on any issue it prints the same
  report (`RulesEngine.Cli/Support/RuleValidationReportWriter.cs`, shared with `check-rules`) and
  returns exit code `1` — no `--skip-rule-validation` escape hatch, since the check is cheap and a
  broken ruleset should never silently or crashily proceed past it.
- Tests: `RuleFileLoaderTests` covers `ValidateDirectories` aggregation directly;
  `RulesEngine.Cli.Tests` gained `CheckRulesCommandTests` and `ValidateCommandPreflightTests`
  (invoking the actual `Command` via `Build().Parse(...).InvokeAsync()` and redirecting
  `Console.Out`). Both new CLI test classes share a `[Collection(ConsoleOutputCollection.Name)]` —
  xUnit parallelizes different test classes by default, and two classes independently swapping the
  process-global `Console.Out` will race unless serialized into one collection.

## The 11 starter rules

All under `rules/`, all illustrative (`Contoso.*` namespace, `illustrative: true`), matching the
"fully v1-implementable" subset from the plan (rules 1–5, 9–12, 14–15):

| Rule ID | File | What it checks |
|---|---|---|
| DDD-ENTITY-001 | `ddd/ddd-entity-001.yml` | Domain entities inherit `Entity<*>` |
| DDD-ENTITY-002 | `ddd/ddd-entity-002.yml` | Domain entities have a private/protected ctor |
| DDD-AGGREGATE-001 | `ddd/ddd-aggregate-001.yml` | Types inheriting `Entity<*>` implement `IAggregateRoot` |
| DDD-AGGREGATE-002 | `ddd/ddd-aggregate-002.yml` | `IAggregateRoot` implementers have a `Create` method |
| DDD-EVENT-001 | `ddd/ddd-event-001.yml` | `IDomainEvent` implementers live in `*.Domain.Events` |
| DDD-EVENT-002 | `ddd/ddd-event-002.yml` | `IDomainEvent` implementers live in a `*.Domain` project |
| APP-COMMANDHANDLER-001 | `ddd/ddd-commandhandler-001.yml` | Handlers in `*.Application.Handlers` implement `ICommandHandler<*>` |
| ARCH-DEPENDENCY-001 | `architecture/architecture-dependency-001.yml` | `*.Domain` projects don't reference `*.Infrastructure` projects |
| ARCH-DEPENDENCY-002 | `architecture/architecture-dependency-002.yml` | `*.Domain` types don't reference `Contoso.Infrastructure.*` |
| ARCH-PACKAGE-001 | `architecture/architecture-package-001.yml` | `*.Domain` projects don't reference `Microsoft.EntityFrameworkCore` |
| CSHARP-NAMESPACE-001 | `csharp/csharp-namespace-001.yml` | Every type lives under `Contoso.*` |

## Key decisions and gotchas (read before touching Roslyn/Buildalyzer/YAML code)

1. **Glob patterns, not exact match, for base types.** Roslyn renders a closed generic base type
   as `Entity<int>`, not the open `Entity<TId>` placeholder used when *authoring* a rule. Rules
   must use `Entity<*>` (wildcard) to match any closed type argument. `MustInheritFromAssertion`
   and `InheritsFromSelector` both use `GlobMatcher.IsMatch`, not `==`, specifically for this
   reason. If you add another base-type/interface-matching primitive, use `GlobMatcher` too.

2. **Package version pins are load-bearing, not arbitrary.** `Buildalyzer`/`Buildalyzer.Workspaces`
   were removed in favor of `Microsoft.CodeAnalysis.Workspaces.MSBuild` (`MSBuildWorkspace`) plus a
   small supplementary `Microsoft.Build.Evaluation.Project` evaluation per project (for
   `PackageReferences`/raw MSBuild `Properties`, which `MSBuildWorkspace` doesn't expose) — see
   `MsBuildAnalysisProvider.cs`. This removed the need to pin `Microsoft.CodeAnalysis.CSharp(.Workspaces)`
   to 4.10.0 (previously required because `Buildalyzer.Workspaces` depended on
   `Microsoft.CodeAnalysis.Workspaces.Common` 4.10.0, and mixing Roslyn generations in one process
   throws `TypeLoadException`) — `RulesEngine.Analyzers.Roslyn.csproj` and
   `RulesEngine.Analyzers.MSBuild.csproj` now use **5.6.0** (latest), matched by
   `Microsoft.CodeAnalysis.Workspaces.MSBuild` 5.6.0. Keep all three in the same Roslyn generation
   if you bump one.
   - `RulesEngine.Analyzers.MSBuild.csproj` still has `Microsoft.Build`/`Microsoft.Build.Framework`
     `PackageReference`s with `ExcludeAssets="runtime" PrivateAssets="all"` at `17.11.48`, required
     by `Microsoft.Build.Locator`'s own build-time check (`MSBL001`) — much shorter list than
     Buildalyzer needed (~12 entries), since `Microsoft.CodeAnalysis.Workspaces.MSBuild`'s own
     dependency graph is far shallower. If `MSBL001` fires after a version bump, add/adjust exactly
     the package + version it names — don't guess in advance.

3. **JsonSchema.Net schemas must be parsed once per process.** It throws
   `JsonSchemaException: "Overwriting registered schemas is not permitted"` if you call
   `JsonSchema.FromText` twice with the same `$id` in one process (e.g. across multiple xUnit
   tests). `RuleSchemaValidator` uses a `static Lazy<JsonSchema>` for this reason — don't remove
   that caching.

4. **System.CommandLine is on the 3.0 preview API**, not the older 2.0 beta API you may know.
   Key differences: `command.SetAction(async (parseResult, ct) => ...)` (not `SetHandler`),
   `rootCommand.Subcommands.Add(subCommand)` (not `AddCommand`), `rootCommand.Parse(args).InvokeAsync()`
   (not `rootCommand.InvokeAsync(args)`). If IntelliSense/docs you find look different, you're
   probably looking at the 2.0 API — trust what compiles.

5. **`RuleDefinition` holds executable interface instances directly** (`ITargetSelector Target`,
   `IReadOnlyList<IAssertion> Assertions`, `IConditionNode? When`) rather than separate "Definition"
   DTOs that get resolved at evaluation time. This was a deliberate PR1 simplification versus the
   original plan sketch (which had `TargetSelectorDefinition`/`AssertionDefinition` DTOs) —
   `RulesEngine.Configuration.Parsing` builds these executable instances directly from YAML via
   `SelectorParserRegistry`/`AssertionParserRegistry`, there's no intermediate DTO layer. Keep
   this consistent if you extend the schema.

6. **Known limitation — CLI self-analysis (partially resolved).** `rules-engine validate` run
   against **this tool's own currently-running solution** (`RuleEngine.sln`) used to reliably crash
   with `System.InvalidOperationException: Sequence contains no elements` inside
   `MsBuildAnalysisProvider.ContributeAsync`, because Buildalyzer's default design-time build ran
   `Clean;Build`, and when Buildalyzer's "common output directory" happened to be this CLI's own
   `bin/` folder, an earlier project's `Clean` step deleted `Buildalyzer.Logger.dll`, which a later
   project's spawned MSBuild process then couldn't find.
   - **This specific crash is fixed** by the Buildalyzer→`MSBuildWorkspace` migration (see gotcha
     #2) — confirmed empirically by running `rules-engine validate` against `RuleEngine.sln` after
     the swap. `MSBuildWorkspace`'s design-time build runs in a separate out-of-process BuildHost
     and does no `Clean;Build` sequencing across projects, so this class of shared-output-directory
     collision no longer occurs.
   - **However, self-analysis still doesn't complete end-to-end.** It now gets much further — past
     type extraction — and crashes in `NoPureDelegationOverrideAnalyzer.Analyze`
     (`src/RulesEngine.Evaluation/Analyzers/NoPureDelegationOverrideAnalyzer.cs`):
     `model.Solutions.SelectMany(...).SelectMany(p => p.Types).ToDictionary(t => t.FullName)`
     assumes a type's `FullName` is unique across the *entire* repository, but every test project in
     this solution gets an SDK-generated `AutoGeneratedProgram` stub type with an identical name, so
     with 6+ test projects the dictionary throws `ArgumentException: An item with the same key has
     already been added`. This is a **pre-existing bug unrelated to MSBuild/Buildalyzer** — it was
     simply never reached before because the Buildalyzer crash always happened first. Not yet fixed;
     the fix direction is to key by something that includes the project (e.g. `(ProjectName,
     FullName)`) rather than `FullName` alone — any other analyzer/selector doing a bare
     `.ToDictionary(t => t.FullName)` across `model.Solutions.SelectMany(s => s.Projects)` likely has
     the same latent bug.
   - Still does **not** affect validating any other repository without this same-name collision —
     proven by `RulesEngine.IntegrationTests` (a real, separate 3-project solution analyzed
     correctly, real violations detected).

7. **`dotnet sln add` auto-adds transitively referenced projects** in this SDK version — you'll
   see "Project X added to the solution" for projects you didn't explicitly pass, when they're
   referenced by the one you did pass. Not a bug, just a newer CLI behavior worth knowing about
   so you don't duplicate `dotnet sln add` calls.

8. **The SARIF NuGet package is `Sarif.Sdk`, not `Microsoft.CodeAnalysis.Sarif`** — the latter is
   just the root C# namespace the package exposes; `dotnet add package Microsoft.CodeAnalysis.Sarif`
   fails with "no versions available". `SarifViolationReporter` builds a `SarifLog`/`Run`/`Result`
   graph and serializes via `SarifLog.Save(Stream)` into a `MemoryStream` (safe — it does not close
   the stream), then reads the bytes back out as a string to satisfy `IViolationReporter`'s
   `TextWriter`-based contract (there's no `Save(TextWriter)` overload). Also note: the SDK omits
   the `level` field entirely for `FailureLevel.Warning` results, since "warning" is the SARIF
   spec's implicit default — don't assert on its presence for warning-level violations, only for
   note/error.

9. **MSBuildLocator must be registered exactly once per process, via a single choke point.**
   `RulesEngine.IntegrationTests` has two test classes that each need MSBuild (via
   `MSBuildWorkspace` and `Microsoft.Build.Evaluation.Project`).
   Originally each had its own `static` constructor guarded by
   `if (!MSBuildLocator.IsRegistered) MSBuildLocator.RegisterDefaults();` — this is **not** safe
   with two classes in one assembly, because xUnit runs test classes in the same assembly in
   parallel by default, and the two independent static-constructor check-then-act races threw
   `InvalidOperationException: MSBuild assemblies were already loaded`. Fixed by moving the
   registration into a single `[ModuleInitializer]` method
   (`RulesEngine.IntegrationTests/MsBuildLocatorInitializer.cs`), which the runtime guarantees
   runs exactly once, before any type in the assembly is used. If you add a third test class that
   needs MSBuild, it gets this for free — don't add another per-class static constructor.

10. **System.CommandLine 3.0-preview quirks worth knowing** (confirmed by direct experimentation,
    since the preview API differs from both the 2.0 docs and from what an LLM might guess):
    - `Option<string[]>` supports repeated flags out of the box (`--rule A --rule B` → `["A","B"]`);
      it does *not* support space-separated multi-value syntax (`--rule A B` errors on the second
      token). No extra configuration needed for this — just declare `new Option<string[]>("--rule")`.
    - `option.AcceptOnlyFromAmong("a", "b", "c")` validates the value and produces a parse error
      (handled automatically by `Parse(...).InvokeAsync()` — invalid values never reach the command
      action) — this is how every enum-like option (`--format`, `--severity-threshold`, `--fail-on`)
      is validated, rather than hand-rolling an enum parser + custom error message.
    - `new Option<string>("--x") { DefaultValueFactory = _ => "default" }` is how you set a default
      value (not a constructor parameter).
    - `Option<bool>` is a plain presence flag by default (`--enabled-only` present → `true`, absent
      → `false`) — no `--enabled-only true`/`false` syntax needed.
    - Positional `Argument<T>` is required by default; a missing one produces a parse error before
      the command action ever runs.

## Things NOT done (explicitly deferred, per the plan)

- `Any`/`All`/`None` condition combinators (only `And`/`Or`/`Not`).
- `when`/`and`/`or`/`not` YAML parsing — `AndCondition`/`OrCondition`/`NotCondition` exist and are
  unit-tested, but there's no parser registry or schema support to author them in a rule YAML
  file yet (see "Selectors and assertions implemented" section above).
- Method-body assertions (`MustCall`, `MustAwait`, etc.), cross-entity assertions
  (`MustHaveCorresponding`), naming/cardinality assertions, package version-range constraints
  (`MustUsePackageVersionAtLeast`), property-setter assertions (`MustNotHaveSetter`).
- Non-C# analysis providers (YAML/JSON/Terraform/K8s/etc.) — architecture left open via
  `IAnalysisProvider`, nothing implemented.
- A dedicated standards-file format — `list-standards` derives standards purely from the
  `standard` field already present on loaded rules, as planned (no separate standards-file format).
- Everything in `RuleEngine/REFACTORING.md` (analysis sessions/caching, rule versioning and
  lifecycle states, the Selector/Predicate/Assertion/Diagnostic split, a custom-analyzer escape
  hatch, rule fixture testing) — a deliberately separate, larger initiative the user chose not to
  start yet. See "Where things stand" above.
- A fix for the remaining CLI self-analysis known limitation (gotcha #6) — the original
  Buildalyzer-crash cause is resolved, but `NoPureDelegationOverrideAnalyzer`'s
  `FullName`-uniqueness assumption still blocks full self-validation; documented but not solved.
