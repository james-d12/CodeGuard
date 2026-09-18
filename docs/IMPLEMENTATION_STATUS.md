# CodeGuard Implementation Status

This file is a handoff summary for picking up work on this repository in a new session/agent.
It was written after implementing PR1–PR7 of the approved implementation plan. **Read this
before making changes** — it captures context, decisions, and gotchas that aren't obvious from
the code alone.

## Essential reading (in this order)

1. `CodeGuard/PRIMITIVES.md` — the original design/requirements doc for this whole project.
2. The approved implementation plan: `/home/james/.claude/plans/reading-the-codeguard-primitives-md-pla-imperative-quail.md`
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
`CodeGuard/REFACTORING.md` architectural-evolution proposal — see below.

There is also a separate, much larger **architectural evolution** proposal in
`CodeGuard/REFACTORING.md` (Selector/Predicate/Assertion/Diagnostic separation, analysis
sessions with caching, rule versioning/lifecycle, a custom-analyzer escape hatch, rule fixture
testing, etc.). The user explicitly deferred that in favor of finishing PR7 first — it has **not**
been started. Read it before proposing any further architectural changes, but treat it as a
separate initiative from the PR1–PR8 plan, not something to blend into it opportunistically.

## Verifying the current state

```bash
cd /home/james/Dev/CodeGuard
dotnet build          # should succeed, 0 errors (14 pre-existing NU1903 advisory warnings, see above)
dotnet test           # should show 81 passed across 6 test projects, 0 failed
dotnet run --project CodeGuard/CodeGuard.Cli -- list-rules       # works against this repo's own rules/
dotnet run --project CodeGuard/CodeGuard.Cli -- explain-rule DDD-ENTITY-001
dotnet run --project CodeGuard/CodeGuard.Cli -- validate   # see "Known limitation" below — self-validation still crashes
```

## Architecture overview

```
CodeGuard.sln
Directory.Build.props          # net10.0, Nullable enable, ImplicitUsings enable, LangVersion latest
global.json                    # pins SDK to 10.0.100 (rollForward: latestFeature) — read by CI (PR8)
.github/workflows/ci.yml       # dotnet restore/build/test on push/PR to main + workflow_dispatch (PR8)
.codeguard/config.yml        # repository discovery config for THIS repo (PR6)

rules/                         # the 11 illustrative starter rules (YAML), all tagged illustrative: true
  ddd/                         # 7 files — entity, aggregate, event, command-handler rules
  architecture/                # 3 files — layering/package rules
  csharp/                      # 1 file — namespace convention rule
  schema/rule.schema.json      # JSON Schema (2020-12) for rule YAML files

CodeGuard/
  PRIMITIVES.md                 # original design doc — do not edit
  REFACTORING.md                # separate, much larger architectural-evolution proposal — not started, see above
  CodeGuard.Cli/               # System.CommandLine-based CLI (net10.0 exe, AssemblyName=codeguard)
    Program.cs                  #   MSBuildLocator bootstrap + composes RootCommand from Commands/
    Commands/                   #   ValidateCommand, ListRulesCommand, ExplainRuleCommand
    Support/                    #   CliRepositoryContext (shared --path/--config resolution), CommonOptions
  CodeGuard.Core/              # RuleEvaluator, ValidationResult/Violation (Core.Evaluation, Core.Results)
  CodeGuard.RuleModel/         # RuleDefinition, Severity, EnforcementClassification;
                                 #   ITargetSelector/IAssertion/IConditionNode interfaces;
                                 #   AndCondition/OrCondition/NotCondition
  CodeGuard.Analysis/          # Provider-agnostic analysis model (RepositoryModel, ProjectModel,
                                 #   TypeModel, etc. in AnalysisModel/) + IAnalysisProvider,
                                 #   AnalysisModelBuilderContext, AnalysisModelBuilder (Providers/)
  CodeGuard.Evaluation/        # Concrete executable selectors/assertions (see table below) + GlobMatcher
  CodeGuard.Configuration/     # YAML rule loading/parsing/validation + repository discovery (see below)
  CodeGuard.Reporting/         # IViolationReporter + Console/Json/Sarif reporters (Console/, Json/, Sarif/)
  CodeGuard.Analyzers.Roslyn/  # RoslynTypeExtractor: CSharpCompilation -> IReadOnlyList<TypeModel>
  CodeGuard.Analyzers.MSBuild/ # MsBuildAnalysisProvider: MSBuildWorkspace + Microsoft.Build.Evaluation -> ProjectModel (+ Types via Roslyn)
  CodeGuard.Analyzers.Repository/ # RepositoryFileProvider: walks the filesystem -> FileModel (no Roslyn/MSBuild)

tests/
  CodeGuard.Core.Tests/            (9 tests)  — RuleEvaluator, Console/Json/Sarif violation reporters
  CodeGuard.Evaluation.Tests/      (41 tests) — every selector/assertion + And/Or/Not composition
  CodeGuard.Configuration.Tests/   (15 tests) — RuleFileLoader (incl. source-tracking), RepositoryDiscovery,
                                                    CodeGuardConfigLoader (incl. explicit --config path)
  CodeGuard.Analyzers.Roslyn.Tests/(12 tests) — RoslynTypeExtractor against in-memory source snippets
  CodeGuard.Analyzers.Repository.Tests/ (1 test) — RepositoryFileProvider walk + directory exclusions
  CodeGuard.IntegrationTests/      (3 tests)  — full pipeline against a real fixture solution, incl.
                                                    JSON/SARIF reporter output shape
    Fixtures/SimpleDomainSolution/   — real 3-project .sln (Contoso.Domain/Application/Infrastructure)
                                        used ONLY by MsBuildAnalysisProvider at test-time, not built by the main solution
```

### Dependency direction between the core projects

```
CodeGuard.Analysis  (no dependencies — pure model + provider abstraction)
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

CodeGuard.Reporting     depends on Core (transitively RuleModel, for Severity in the SARIF level mapping)
                          + Sarif.Sdk package (SarifViolationReporter; System.Text.Json only for Json one)
CodeGuard.Configuration depends on Analysis + RuleModel + Evaluation (needs concrete selector/assertion classes to construct from YAML)
CodeGuard.Cli           depends on everything (Core, RuleModel, Analysis, Evaluation, Reporting, Configuration,
                          Analyzers.MSBuild, Analyzers.Repository)
```

## Selectors and assertions implemented (v1 scope)

Every selector/assertion has both a concrete `CodeGuard.Evaluation` class and a
`CodeGuard.Configuration.Parsing` YAML parser, registered in `DefaultParsers.cs`. **Extend
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

All pattern matching uses `CodeGuard.Evaluation.GlobMatcher` (only `*` wildcard supported, via
`Regex.Escape` + `.*` substitution). **Important:** patterns are matched with `GlobMatcher`, not
exact string equality — this matters for generic base types (see Gotcha #1 below).

`AndCondition`/`OrCondition`/`NotCondition` exist in `CodeGuard.RuleModel.Conditions` and are
unit-tested, but **there is no YAML parsing for `when`/`and`/`or`/`not` yet** — no starter rule
needs it, so it was deliberately deferred (not stubbed); PR7 didn't end up needing it either. If a
future rule needs it, you'll need to add a
`ConditionParserRegistry` in `CodeGuard.Configuration.Parsing` and wire `when:` parsing into
`RuleDocumentParser`, plus add `"when"` to `rules/schema/rule.schema.json`.

## CLI commands (PR7)

All four commands live in `CodeGuard/CodeGuard.Cli/Commands/`, share `--path`/`--config`
resolution via `Support/CliRepositoryContext.cs`, and are composed in `Program.cs`. **Note:**
the table below documents the original PR7 command names; `list-rules`/`explain-rule`/`check-rules`
were later regrouped under a `rules` subcommand (`rules list`/`rules explain`/`rules validate`,
the last renamed from `rules check`) — see "Post-v1 addition: `rules` subcommand group + `rules
create`" below.

| Command | Options | Notes |
|---|---|---|
| `validate` | `--path`, `--config`, `--format console\|json\|sarif`, `--output <file>`, `--rule <id>` (repeatable), `--solution <path>` (repeatable), `--severity-threshold info\|warning\|error\|critical`, `--fail-on info\|warning\|error\|critical` | Providers run `[RepositoryFileProvider, MsBuildAnalysisProvider]` in that order. `SolutionFileLocator` (`Cli/Support/SolutionFileLocator.cs`) discovers `.sln`/`.slnx` files recursively under `--path` (skipping `bin`/`obj`/`.git`/`.vs`/`.idea`/`node_modules`); auto-discovery (no `--solution`) proceeds unattended only when exactly one is found — more than one throws `InvalidOperationException` listing every candidate, since that's ambiguous (a genuine multi-solution repo vs. `--path` pointed at a parent directory containing several unrelated repos look identical from here), and the user must re-run with `--solution <path>` (repeatable) to say which to analyze. `MsBuildAnalysisProvider` takes the resulting list and dedupes any project referenced by more than one solution (by project path) so it's only built/reported once, attributed to whichever solution is processed first. `--severity-threshold` filters the reported `ValidationResult` (recomputing `RulesPassed`/`RulesFailed`/`Status`); `--fail-on` (default `info`) independently decides the process exit code from what's left after that filter — both default to today's original behavior (any violation reported, any violation fails) when omitted. |
| `rules list` (was `list-rules`) | `--path`, `--config`, `--format table\|json`, `--tag` (repeatable, any-match), `--enabled-only` | Pure rule loading — no analysis model, no Roslyn/MSBuild, so this is cheap for an agent to call before generating code. |
| `rules explain <ruleId>` (was `explain-rule`) | `--path`, `--config` | Uses `RuleFileLoader.LoadFromDirectoriesWithSource` to find the backing YAML file, then prints parsed metadata plus the **raw YAML source verbatim** (rather than trying to introspect configured selector/assertion parameters, which `ITargetSelector`/`IAssertion` don't expose beyond `Kind` — see decision #5 below). |

`CodeGuardConfigLoader.LoadOrDefault` now has a two-argument overload
(`repoRoot, explicitConfigPath`) backing `--config`; the original one-argument overload still
exists and delegates to it with `null`.

### Post-v1 addition: `--format html` and directory-aware `--output`

`validate --format` also accepts `html` (`CodeGuard.Reporting.Html.HtmlViolationReporter`) — a
single self-contained file (inline CSS/JS, no external requests) with client-side severity/rule-id/
project/message filtering, meant to be opened in a browser or published as a CI artifact.

`--output` now accepts a directory as well as an exact file path
(`CodeGuard.Cli.Support.ReportOutputPathResolver`, tested in `ReportOutputPathResolverTests.cs`
following the same pure-function-of-its-inputs pattern as `ColorSupport`): if the value is an
existing directory, or ends in a path separator, a default filename derived from `--format` is
appended (`validation-report.html`/`.json`/`.sarif`/`.txt`). Either way, `ValidateCommand` now
creates the resolved path's parent directory if it doesn't exist yet, so `--output
./artifacts/report.html` no longer requires `./artifacts` to already exist.

### Post-v1 addition: `check-rules` and the `validate` rule-set pre-flight gate

Design doc: `docs/done/RULE_VALIDATION_PLAN.md`. Before this, a broken rule YAML file made
`RuleFileLoader.LoadFromFile` throw immediately (first error only, uncaught by any CLI command), so
`validate` against a repo with one bad rule file crashed with a raw .NET stack trace instead of a
clean report, and there was no way to check a folder of rule YAML in isolation.

- `RuleFileLoader` (`CodeGuard.Configuration/Loading/RuleFileLoader.cs`) gained a non-throwing core:
  `TryLoadFromFile` (schema-validate-then-parse, catching `RuleSchemaValidationException`/
  `RuleParsingException`/`RuleLoadException` into an error list instead of throwing) and
  `ValidateDirectories` (walks a rule-file set, collects **every** file's issues plus duplicate-ID
  conflicts into one `RuleSetValidationReport`, rather than stopping at the first one).
  `LoadFromFile`/`LoadFromDirectory(ies)(WithSource)` are unchanged in observable behavior (still
  throw on the first problem) but are now implemented on top of this non-throwing core, so there is
  a single parsing pass shared by every caller — no drift between "what `check-rules` approves" and
  "what `validate` actually loads."
- New `CliRepositoryContext.ValidateRules()` (`CodeGuard.Cli/Support/CliRepositoryContext.cs`)
  exposes `ValidateDirectories` for the configured rule paths, parallel to `LoadRules()`.
- New `check-rules` command (`CodeGuard.Cli/Commands/CheckRulesCommand.cs`): validates a rule set
  for structural correctness only (schema conformance, unknown selector/assertion/analyzer `kind`,
  `target`/`assertions` vs `analyzer` mutual exclusivity, duplicate rule IDs) with no analysis model
  and no MSBuild involved. Shares `--path`/`--config`/`--rules-source`/`--branch` resolution with
  every other command, so `--rules-source <folder>` points it directly at an ad-hoc rules folder.
  `--format console|json`; exit `0`/`1` on validity, no severity concept (these are authoring errors,
  not violations).
- `validate` (`ValidateCommand.cs`) now calls `context.ValidateRules()` unconditionally before
  building the `AnalysisModel`/touching `MsBuildAnalysisProvider`; on any issue it prints the same
  report (`CodeGuard.Cli/Support/RuleValidationReportWriter.cs`, shared with `check-rules`) and
  returns exit code `1` — no `--skip-rule-validation` escape hatch, since the check is cheap and a
  broken ruleset should never silently or crashily proceed past it.
- Tests: `RuleFileLoaderTests` covers `ValidateDirectories` aggregation directly;
  `CodeGuard.Cli.Tests` gained `CheckRulesCommandTests` and `ValidateCommandPreflightTests`
  (invoking the actual `Command` via `Build().Parse(...).InvokeAsync()` and redirecting
  `Console.Out`). Both new CLI test classes share a `[Collection(ConsoleOutputCollection.Name)]` —
  xUnit parallelizes different test classes by default, and two classes independently swapping the
  process-global `Console.Out` will race unless serialized into one collection.

### Post-v1 addition: `rules` subcommand group + `rules create`

The flat command names `check-rules`/`list-rules`/`explain-rule` were regrouped under a new
`rules` parent command (`Cli/Commands/RulesCommand.cs`, a `Command` whose own `.Subcommands` are
populated the same way `rootCommand`'s are in `Program.cs` — this is the first place in the repo
a `Command` nests another `Command` rather than being a direct child of `rootCommand`):
`check-rules` → `rules check`, `list-rules` → `rules list`, `explain-rule <id>` → `rules explain
<id>`. `validate` and `setup` stay top-level. This was a clean break (no hidden aliases for the
old flat names) since the tool is still pre-1.0.

The command classes moved from `Cli/Commands/*.cs` into `Cli/Commands/Rules/*.cs`
(`CheckRulesCommand` → `Rules.CheckCommand`, `ListRulesCommand` → `Rules.ListCommand`,
`ExplainRuleCommand` → `Rules.ExplainCommand`), namespace `CodeGuard.Cli.Commands.Rules`. Tests
mirrored the move (`CheckRulesCommandTests` → `tests/CodeGuard.Cli.Tests/Rules/CheckCommandTests.cs`).

A new `rules create` command (`Rules/CreateCommand.cs`) interactively scaffolds a rule YAML file.
Rather than hardcoding each target-selector/assertion kind's parameter shape into the CLI (there
are 21 selector kinds and 45 assertion kinds in `DefaultParsers`, each with different parameter
names), it drives a generic kind-picker + key/value parameter loop off
`SelectorParserRegistry.Kinds`/`AssertionParserRegistry.Kinds` (new one-line accessors added to
both registries, backed by the `_byKind` dictionary each already had) — so it automatically
supports new kinds with zero CLI changes when they're registered in `DefaultParsers`. It only
authors the `target`+`assertions` rule shape, not the `analyzer`-referencing shape (which points
at a specific pre-existing `ICustomAnalyzer` by name — a rarer, more advanced path). The assembled
document is serialized to YAML via a new `CodeGuard.Configuration.Writing.RuleYamlWriter`
(wrapping `YamlDotNet.Serialization.SerializerBuilder`), keeping the `YamlDotNet` dependency
confined to `CodeGuard.Configuration` rather than adding it to `CodeGuard.Cli` directly. Before
reporting success, it runs the same `CliRepositoryContext.ValidateRules()` (`RuleFileLoader
.ValidateDirectories`) that `rules validate`/`validate`'s pre-flight gate use, against the rules
directory including the newly written file — catching schema errors and duplicate-ID conflicts
before the user walks away thinking the rule is good.

### Post-v1 addition: `rules check` → `rules validate`

Further naming-convention pass on the `rules` subcommand group: `rules check` was renamed to
`rules validate`, matching `rules list`/`rules explain`/`rules create`'s single-verb style and using
the same word this repo already uses for the top-level `validate` command's own name (the two remain
functionally distinct — `rules validate` only checks rule YAML structural correctness, no analysis
model or MSBuild involved, while top-level `validate` runs the full analysis engine against a
target repo's source and includes the same rule-set check as an unconditional pre-flight step).
Command file `Cli/Commands/Rules/CheckCommand.cs` → `Rules/ValidateCommand.cs`, class
`Rules.CheckCommand` → `Rules.ValidateCommand`, test file/class
`Cli.Tests/Rules/CheckCommandTests.cs` → `Rules/ValidateCommandTests.cs`. Clean break, no alias for
the old `rules check` name — consistent with the earlier `check-rules` → `rules check` rename above,
since the tool is still pre-1.0.

### Post-v1 addition: expanded generic primitive vocabulary

The "Selectors and assertions implemented (v1 scope)" table above is a historical snapshot of the
original 8-PR plan and was already stale before this addition (it lists 5 selector kinds/12
assertion kinds; the registries in `DefaultParsers.cs` had grown to 14/32 by the time of the
Stage A/B rule-coverage work in `docs/done/`). Rather than rewrite that table in place, this
section documents on top of it, following this doc's existing "Post-v1 addition" convention —
**`DefaultParsers.cs` is the actual source of truth for the current kind list**, not this doc.

Motivation and full design rationale: broaden the *variety* of declarative primitives available
(per `docs/PRIMITIVES.md`'s original vocabulary) while staying generic/reusable rather than
one-off, per `docs/REFACTORING.md` §2.1. Four concrete additions:

- **Selectors over previously-unreachable syntax-fact data** (`SwitchSelector`/`switch`,
  `ThrowSiteSelector`/`throw_site`, `MutationSiteSelector`/`mutation_site`,
  `TryBlockSelector`/`try_block`, `MethodBodyShapeSelector`/`method_body_shape`,
  `DiagnosticSelector`/`diagnostic`, `DirectorySelector`/`directory`). `RepositoryModel.Switches`/
  `ThrowSites`/`MutationSites`/`TryBlocks`/`MethodBodyShapes`/`Diagnostics`/`Directories` were
  already populated by `RoslynSyntaxFactExtractor`/`RepositoryFileProvider` but only reachable
  from bespoke `analyzer`-kind classes — no declarative rule could select over them. Each new
  selector mirrors `CallSiteSelector`'s glob/range-filter style. This required extending
  `CodeGuard.Configuration.Testing.TestSetupBuilder` to accept `switches:`/`throwSites:`/
  `mutationSites:`/`tryBlocks:`/`methodBodyShapes:`/`diagnostics:` setup arrays (previously these
  six keys explicitly threw `RuleParsingException` — "not supported yet" — since no selector
  needed them; see `docs/RULES_TEST_DESIGN.md`'s "v1 setup scope").
- **`must_have_count`** (`MustHaveCountAssertion`): generalizes `must_exist`/`must_not_exist`
  (existence-only) to counting, via `min`/`max`/`exactly` params against a nested `selector:`
  template — same `SelectorTemplateResolver` plumbing. `must_exist`/`must_not_exist` are kept as
  the simpler, more readable form for pure existence checks (`min: 1` / `exactly: 0` are the
  equivalent `must_have_count` forms).
- **`must_depend_on`/`must_only_depend_on`** (`MustDependOnAssertion`/`MustOnlyDependOnAssertion`),
  plus a broadened `must_not_depend_on`: all three now share `DependencyTraversal`
  (`CodeGuard.Evaluation.Assertions`), which walks base type, interfaces, type-level attributes,
  and member (method/property/field/constructor) return/parameter/property/field types and their
  attributes — `must_not_depend_on` previously only checked base type/interfaces/method
  signatures, so a forbidden dependency reached only via a field or attribute silently passed.
  `must_only_depend_on` (the allow-list form) has **no implicit BCL/framework exemption** — Roslyn
  renders primitives via their C# keyword alias (`string`, `int`, `void`, confirmed against
  `RoslynTypeExtractorTests`), not a `System.*`-prefixed name, so a hardcoded "exclude System.*"
  default would silently fail to exempt them; allow-lists must name primitives/framework types
  explicitly (see the example rule for a starter list).
- **Small symmetric gap-fills**: `must_have_field`/`must_not_have_field` (mirrors
  `must_have_property`/`must_not_have_property`, closing the asymmetry against the existing
  `field` selector), `must_not_be_in_namespace` (complement to `must_be_in_namespace`),
  `must_match_namespace_pattern` (regex-on-`Namespace`, complement to `must_match_name`'s
  regex-on-`Name`), `must_use_package_version` (one generic `{package, constraint}` primitive —
  e.g. `constraint: ">=8.0.0"` — covering the at-least/at-most/exactly family from
  `docs/PRIMITIVES.md` §15 without three separate kinds).

All 16 new kinds ship with unit tests (`CodeGuard.Evaluation.Tests/{Selectors,Assertions}`) and at
least one `illustrative: true` example rule with embedded `tests:` cases under `examples/rules/`
(15 new rule files — `must_have_field`/`must_not_have_field` share one file as a natural pair),
verified via `codeguard rules validate`/`codeguard rules test`.

### Post-v1 addition: `must_all_match`/`must_any_match`/`must_none_match` quantifiers, `type` selector `name:` param

Closes the two remaining gaps found by re-auditing `docs/PRIMITIVES.md` against `DefaultParsers.cs`
(see gotcha #6 above for the self-analysis bug fixes done alongside this): `Any`/`All`/`None`
quantifier combinators (§20's `MustHaveAll`, §22) and cross-entity correspondence rules (§23's
`MustHaveCorresponding`). Two additions, kept generic per the same `docs/REFACTORING.md` §2.1
philosophy as the section above:

- **`must_all_match`/`must_any_match`/`must_none_match`** (`MustAllMatchAssertion`/
  `MustAnyMatchAssertion`/`MustNoneMatchAssertion`, `CodeGuard.Evaluation.Assertions`): run a nested
  `assertions:` list (implicit AND, same as a rule's top-level `assertions:`) against every match of
  a nested `selector:` (same `SelectorTemplateResolver` plumbing as `must_have_count`), then combine
  per-match results via universal (`all`), existential (`any`), or negative-existential (`none`)
  quantification. This is the piece of the `And`/`Or`/`Not` vocabulary those three don't cover -
  they combine conditions for a *single* candidate, these quantify over a *set*. `must_all_match`/
  `must_none_match` are vacuously true on an empty match set (standard for-all/none-exists
  semantics); `must_any_match` is false on an empty set (nothing to satisfy "exists").
  - Parsing these requires the very `AssertionParserRegistry` being constructed (any assertion kind
    can nest inside one of these) - a genuine self-reference, not just recursion. `DefaultParsers
    .CreateAssertionRegistry` handles this with a local function closing over a `registry` variable
    assigned immediately after the list literal completes; the delegate is only ever invoked later,
    during an actual rule-file parse, by which point `registry` is assigned. Regression-tested by
    `RuleFileLoaderTests.LoadFromFile_WithMustAllMatch_ParsesNestedSelectorAndNestedAssertions`
    (nests a `must_have_property` inside a `must_all_match`, i.e. two different assertion kinds).
- **`type` selector gained a `name:` glob param** (mirroring `method`'s existing `namePattern`) —
  combined with `must_have_count`, this makes `MustHaveCorresponding`/`MustHaveOneToOne`/
  `MustHaveOneToMany` fully expressible with **zero new assertion classes**: `must_have_count` +
  `type: { namespace: ..., name: "${Name}Handler" }` + `exactly: 1` (see
  `examples/rules/ddd/ddd-cqrs-001.yml`).
  - Building this example rule surfaced a real bug in `SelectorTemplateResolver`: it only resolved
    a template string that *was* exactly one placeholder (`^\$\{(\w+)\}$`, anchored), so
    `"${Name}Handler"` was left completely unresolved (literal, placeholder and all) rather than
    becoming `"PlaceOrderHandler"`. Fixed to a global `Regex.Replace` so a placeholder can appear
    anywhere within a larger string, with the same fallback-to-literal behavior as before for an
    unresolvable placeholder (property not found, or its value is null) - this is what actually
    makes suffix-based correspondence rules like `must_have_corresponding`'s use case work, not
    just the new `name:` param on its own. Confirmed against the entire existing `examples/rules/`
    tree (125 rule files, 235 embedded test cases) with no regressions.

Both additions ship with unit tests (`CodeGuard.Evaluation.Tests/{Assertions,Selectors}`,
`SelectorTemplateResolverTests`) and `illustrative: true` example rules with embedded `tests:`
(`examples/rules/ddd/ddd-aggregate-004.yml`, `ddd-cqrs-001.yml`), verified via `codeguard rules
validate`/`codeguard rules test`.

### Post-v1 addition: capability descriptors, `rules discover`, structured validation errors, `rules explain --format json`

Design doc: `docs/HIGH_LEVEL_AI_ASSISTING.md` (Phase 1 of §27). Implements the AI-assisting doc's
prerequisite for letting an agent discover CodeGuard's actual rule vocabulary instead of guessing at
it, plus the three authoring-primitive gaps that prerequisite gated.

- **Capability descriptors** (`CodeGuard.Configuration/Capabilities/CapabilityDescriptor.cs`):
  `ISelectorParser`/`IAssertionParser`/`IAnalyzerParser` each gained a `CapabilityDescriptor
  Descriptor` property (`Kind`, `Summary`, `Parameters`), where each `ParameterDescriptor` carries
  `Name`/`Type` (`ParameterType`: String/Glob/Regex/Bool/Int/StringList/Enum/Selector/AssertionList)/
  `Required`/`Summary`/`Default`/`AllowedValues`. Selectors additionally set `Produces` (the
  `CandidateKind` they yield); assertions set `AppliesTo` (the `CandidateKind[]` they're valid
  against) — metadata with no other use yet but designed for §14's future "unreachable rule" check
  (an assertion that can't apply to its target's candidate kind). All 77 parser classes (21
  selectors, 45 assertions, 11 analyzers) implement `Descriptor`; landed as three sequential commits
  (selectors, then assertions, then analyzers) since it's a mechanical but not-small change across
  every parser file.
  - `CodeGuard.Configuration/Capabilities/CapabilityCatalog.cs` aggregates every registry's
    descriptors (`SelectorParserRegistry`/`AssertionParserRegistry`/`AnalyzerParserRegistry` each
    gained a `Descriptors` property) plus a hardcoded list for `and`/`or`/`not` conditions (not a
    descriptor-bearing registry).
  - **Drift guard**: `CapabilityCatalogTests` asserts each registry's actual `.Kinds` exactly matches
    the catalog's descriptor kinds for that category, so a parser added without a matching
    descriptor (or vice versa) fails the build immediately rather than silently drifting the way
    the skill's reference docs had (see below).
- **`codeguard rules discover`** (`CodeGuard.Cli/Commands/Rules/DiscoverCommand.cs`,
  `CodeGuard.Cli/Support/CapabilityReportWriter.cs`): reads no rule files, just calls
  `CapabilityCatalog.Create()`. `--format console|json|markdown`; markdown additionally takes
  `--section selectors|assertions|analyzers`.
  - `scripts/sync-skill-references.sh` calls `rules discover --format markdown --section <x>` for
    each of the three sections and splices the result between generated-marker comments in
    `skills/codeguard-rule-generation/references/{selectors,assertions,analyzers}.md`, plus copies
    the schema to `references/rule-schema.json`. CI (`.github/workflows/ci.yml`) re-runs the script
    and diffs `skills/`, failing the build if a primitive is added without regenerating — the exact
    drift (18 primitives behind the engine at the time) that motivated this work in the first place.
    `references/examples.md` is **not** covered — it isn't derivable from descriptors and stays
    hand-maintained.
- **Structured validation errors** (`CodeGuard.Configuration/Validation/RuleValidationError.cs`):
  `RuleFileIssue.Errors` widened from `IReadOnlyList<string>` to `IReadOnlyList<RuleValidationError>`,
  a `record(Code, Path, Message)` with a stable code set (`RuleErrorCodes`: `SCHEMA_VIOLATION`,
  `UNKNOWN_SELECTOR_KIND`, `UNKNOWN_ASSERTION_KIND`, `UNKNOWN_ANALYZER_KIND`, `INVALID_PARAMETER`,
  `DUPLICATE_RULE_ID`, `UNREADABLE_RULE_FILE`, `PARSE_ERROR`) assigned at each throw site
  (`RuleSchemaValidator`, `RuleParsingException.ToValidationError()`, `RuleFileLoader`). Console
  output is unaffected (`ToString()` reproduces the old `"path: message"` prose); JSON output now
  serializes the three fields separately instead of one flattened string.
- **`codeguard rules explain --format json`** (`ExplainCommand.cs`): since assertion parameter
  values can't be recovered from the parsed model (`IAssertion` exposes only `Kind`), JSON output
  pairs rule metadata with the **source document converted YAML→JSON** via a new
  `RuleFileLoader.ReadDocument(filePath)`, which reuses the existing `YamlDocumentReader` rather than
  adding introspection to every assertion class.
- **`RuleTestRunner` relocated** `CodeGuard.Cli.Support` → `CodeGuard.Configuration.Testing`, so it
  can be driven by something other than the CLI (e.g. a future `rules analyze` or an MCP server, per
  `docs/HIGH_LEVEL_AI_ASSISTING.md`'s Phase 4). Pure move — added a `CodeGuard.Configuration →
  CodeGuard.Core` project reference (for `RuleEvaluator`, which `RuleTestRunner` drives), no
  behavior change. While re-validating this move, `CreateCommand` (`rules create`) was reworked to
  prompt for each selector/assertion kind's declared parameters by name (via the new descriptors)
  instead of a blind free-form key/value loop; this surfaced a real bug — none of `CreateCommand`'s
  prompt loops distinguished end-of-input (`Console.ReadLine()` returning `null`) from a blank line,
  so a script/pipe that ran out of input before every required prompt was answered spun forever
  until the process OOMed instead of failing cleanly. Fixed by routing every prompt through a
  `ReadLineOrThrow()` helper that fails the command with a clear message on EOF.

### Post-v1 addition: `codeguard rules analyze`

Design doc: `docs/HIGH_LEVEL_AI_ASSISTING.md` §14 (Phase 3 of §27). Landed sooner than that document's
original phase order expected, since §12's descriptor layer (above) already unlocked most of it
without needing Phase 2's rule `metadata` first.

- `CodeGuard.Configuration/Analysis/RuleSetAnalyzer.cs` (`RuleAnalysisReport`,
  `RuleSetAnalyzer.Analyze`) implements Tier 1 and Tier 2 from §14; Tier 3 (overlapping selectors,
  conflicting assertions) is explicitly out of scope, per that section.
  - Tier 1: invalid rules and duplicate ids reuse `RuleFileLoader.ValidateDirectories` directly
    (split back apart by `RuleErrorCodes.DuplicateRuleId`); missing-tests and one-sided-tests
    (a rule with only `pass` cases or only `fail` cases — distinct from `RuleTestRunner`'s existing,
    narrower vacuous-test guard) are new; disabled/`illustrative: true` rules are counted but
    deliberately excluded from `RuleAnalysisReport.HasFindings`, since a rule set legitimately
    containing them (like this repo's own `examples/rules/`, all illustrative) isn't itself a
    problem. Missing provenance is correctly not implemented - still blocked on §6/§19's
    unimplemented `metadata` field.
  - Tier 2 "unreachable rules": a top-level assertion whose `CapabilityDescriptor.AppliesTo` doesn't
    include the target selector's `Produces` kind. Doesn't recurse into `must_all_match`/
    `must_any_match`/`must_none_match`'s nested `assertions:` - those objects aren't recoverable from
    the parsed model (only `Kind` survives parsing), only from the raw document, and this check
    works from the parsed model since `Kind` is all it needs for the top level.
  - Tier 2 "exact-duplicate rules": needs the raw source document instead (`RuleFileLoader.ReadDocument`),
    for the reason above once parameter values matter - a `Canonicalize` helper recursively
    key-sorts each rule's `target`+`assertions` (or `analyzer`) before grouping, so two rules
    differing only in parameter order still compare equal. Found real, true-positive duplicates in
    `examples/rules/` on first run - e.g. `ARCH-DEPENDENCY-002`/`ARCH-DEPENDENCY-005` share an
    identical enforceable body despite different names/tests/descriptions (each demonstrates the
    same `must_not_depend_on` rule through a different code shape).
- `codeguard rules analyze` (`CodeGuard.Cli/Commands/Rules/AnalyzeCommand.cs`,
  `CodeGuard.Cli/Support/RuleAnalysisReportWriter.cs`): `--format console|json`. Exits non-zero
  when `RuleAnalysisReport.HasFindings` (invalid rules, duplicate ids, missing/one-sided tests,
  unreachable assertions, or exact duplicates) - disabled/illustrative counts don't affect the exit
  code, per above.

### Post-v1 addition: `metadata.source` (rule provenance)

Design doc: `docs/HIGH_LEVEL_AI_ASSISTING.md` §6/§19 (the provenance slice of Phase 2 of §27) - see
that doc for the design discussion this followed. Deliberately narrower than the doc's earlier
drafts, both cuts made explicitly rather than by omission:

- Singular `source:`, not a list - a rule maps to one motivating statement in the common case, and a
  list is a schema shape that's harder to loosen later if wrong.
- `document`/`section` are **free text**, never resolved against a real file (e.g. against
  `.codeguard/config.yml`'s `standards` discovery path) and never used by the engine for grouping or
  lookup. This is the direct fix for why `RuleDefinition.Standard` (above) had to be removed: that
  field tried to be a strict categorization key, and rigid categorization is exactly what broke under
  two authoring paths' incompatible conventions. Giving `document`/`section` no structure to be
  inconsistent about closes off that failure mode rather than re-risking it.
- `statement` is a **paraphrase**, not a verbatim quote - same register `description`/`remediation`
  already use. Chosen over verbatim quoting because this repo's own rule content is derived from real
  company conventions (see CLAUDE.md) and `examples/rules/` is committed to git even though never
  packaged; a paraphrase doesn't add more of that original text to version control than exists today.
- No `generation.method` (ai/human) or lifecycle `status` bundled in, despite the design doc grouping
  "provenance / lifecycle state / test metadata / deterministic capability metadata" under one phase -
  each is a separate convention to get right, and bundling risked the same "one field, several
  meanings" problem this was meant to avoid.
- No backfill of the 125 existing example rules - additive for rules written from here on, same
  policy as the pre-existing unused `Documentation` field.

Mechanically: `RuleMetadata`/`RuleSource` (`CodeGuard.RuleModel/Rules/RuleDefinition.cs`), parsed in
`RuleDocumentParser.ParseMetadata`, added to `rule.schema.json` as one more optional top-level key
(only `document` is required within `source`) - a purely additive schema change. Surfaced by
`rules explain --format json`'s `metadata` field and counted (not flagged as a failure - see
`rules analyze` above) by `rules analyze`'s new `RulesMissingProvenance`.

**Lifecycle state (`status: experimental|active|deprecated|retired`, `docs/REFACTORING.md` §12) was
considered as a follow-up and rejected, not deferred.** Scoped down to "purely informational, no
`rules analyze` check" - the same discipline `metadata.source` went through - it became clear the
field would have zero consumers: nothing would read it, filter by it, or surface it anywhere, unlike
`metadata.source`. `enabled`/`illustrative` already cover the on/off and real-vs-demonstrative axes;
a third axis with no consumer wasn't worth the schema surface. `version` + diagnostic-level version
stamping (the other half of that same REFACTORING.md section) was never evaluated on its own merits
either way - it's a materially bigger, cross-cutting change (touches the evaluator and every
`IViolationReporter`) that was out of scope for this decision regardless of the `status` outcome.
Don't re-propose lifecycle state without first identifying a concrete consumer.

### Post-v1 addition: `metadata.source.file`/`fingerprint` + `rules validate` drift warnings

Design doc: `docs/done/RULE_SOURCE_AND_LINKED_DOCUMENTATION.md` (the "stale-rule detection" /
"documentation-to-rule impact analysis" item `docs/HIGH_LEVEL_AI_ASSISTING.md` §26/§27 Phase 6 named
as future, undesigned work - this is that work). Extends the `metadata.source` shape above rather
than introducing a second, competing "source" concept:

- `RuleSource` gains two new optional fields, `file` (repo-relative path to the linked markdown doc)
  and `fingerprint` (`sha256:<64 hex>` of the resolved content). `document`/`section`/`statement` are
  **unchanged** - same names, same meaning, same free-text-never-resolved behavior when `file` is
  absent. Setting `file` is what opts a rule into checking; nothing else about `metadata.source`
  changes for rules that don't set it (all 125 example rules, today).
- `section`, when `file` is set, doubles as the exact-match heading `rules validate` looks up within
  that file to scope the fingerprint to just that section rather than the whole document - kept the
  fingerprint precise (editing an unrelated section of a large standards doc shouldn't warn every
  rule linked to that file) without adding a redundant field alongside `section` for the same text.
- `statement` stays display-only, even when `file` is set - it is **not** used to search the linked
  document verbatim. A "the heading was renamed but the same text still exists elsewhere in the
  file" (moved-section) feature would need a verbatim string to search for, and `statement` was
  deliberately kept a paraphrase (see above) specifically so this repo's own rule content doesn't get
  more of the original company-standards text copied into a git-committed file than exists today.
  Reusing it for verbatim search would silently break that guarantee, so moved-section detection was
  cut from this pass rather than adding a second, verbatim-text field to work around it.
- Checking is folded into `codeguard rules validate` rather than a new `rules check-sources` command
  (considered and rejected - a fourth structural-ish verb where `validate`/`test`/`analyze` already
  exist, for a check that's naturally a superset of what `validate` already does per rule). Every
  finding - file missing, section not found, section name ambiguous, content changed, fingerprint not
  yet captured - is a **warning only**; none affect `rules validate`'s exit code, which stays governed
  solely by the pre-existing schema/structural checks. This fits "no automatic decisions" better than
  a fail-the-build design would: a stale doc reference is a signal for a human to review, not a reason
  to block CI. `rules validate` is otherwise unaffected when no rule sets `file` (the section is
  omitted from output entirely), and this is the one place it reads files outside the configured
  rules directory - both call sites it shares `RuleValidationReportWriter` with (the top-level
  `validate` pre-flight gate, and `rules create`'s post-scaffold summary) are untouched, via new
  writer overloads rather than changes to the existing ones.
- `rules validate --update-fingerprints` (in scope for this pass, not deferred - without it there's
  no way to resolve a warning once a human has decided the documentation change doesn't require a
  rule change, short of hand-computing a sha256 hex string) recomputes and writes fingerprints for
  rules flagged as drifted or never-captured. Off by default - the only CodeGuard command that
  mutates rule files as a side effect of a check. Implemented as a surgical text splice
  (`RuleSourceFingerprintWriter`, `CodeGuard.Configuration/Sources/`) using YamlDotNet's
  `RepresentationModel` node tree only to locate character offsets in the original file text, never
  to round-trip the file through a serializer - every other YAML write in this repo (`RuleYamlWriter`,
  used by `rules create`) fully regenerates a file from a fresh in-memory model, which would drop
  comments/reorder keys if used to "resave" a hand-authored rule file. Rules with a broken link
  (missing file/section, or an ambiguous section) have nothing to fingerprint and are left for a
  human to fix regardless of the flag.
- Mechanically: `MarkdownSourceResolver` (pure string logic - ATX headings only in v1, exact
  case-sensitive match, ends a section at the next heading of the same or shallower level; Setext
  headings and fuzzy matching are out of scope) and `RuleSourceChecker` (the file-I/O layer, called
  only from `rules validate`) live alongside `RuleSourceFingerprintWriter` in the new
  `CodeGuard.Configuration/Sources/` namespace. `rule.schema.json`'s `metadata.source` gained `file`/
  `fingerprint` as two more optional properties plus `dependentRequired: { fingerprint: [file] }` -
  purely additive, no existing rule breaks.
- Three of the 125 example rules were given real `file`/`fingerprint` links as a deliberate,
  small-scale exception to the "no backfill" policy `metadata.source` itself followed - not a
  reversal of that policy, but a demonstration that the feature works end-to-end against real
  content (dogfooding, and free regression coverage since CI already runs `rules validate` over
  `examples/rules`): `DDD-ENTITY-001` → `examples/docs/ddd-standards.md` § Entities,
  `ARCH-DEPENDENCY-001` → `examples/docs/architecture-standards.md` § Domain Layer,
  `CODING-DI-CONSTRUCTOR-INJECTION-ONLY-001` → `examples/docs/csharp-conventions.md` § Dependency
  Injection. The other 122 rules are untouched - `metadata.source` (with or without `file`) remains
  fully optional.

### Post-v1 removal: `rules create`

A CLI-focus audit (product-owner pass over the whole command surface, per this repo's "keep it
focused, no fluff" goal) found `rules create` - the interactive, descriptor-driven YAML scaffolder
added in the "Post-v1 addition: `rules` subcommand group + `rules create`" section above - had real
implementation weight (prompt loop, `RuleYamlWriter`, EOF handling) but **zero automated
consumers**: the AI rule-generation skill (`skills/codeguard-rule-generation/`), this tool's actual
stated primary workflow per `CLAUDE.md`, writes rule YAML directly and verifies with
`validate`/`test`/`analyze` rather than shelling out to it, and neither `ci.yml` nor the Copilot
agent (`com.github.copilot/agents/codeguard-rule-generator.agent.md`) ever invoked it. Only
README/CLAUDE.md prose and its own tests referenced it. Removed: `Cli/Commands/Rules/CreateCommand.cs`,
`Configuration/Writing/RuleYamlWriter.cs` (no other consumer), and their tests. A human authoring a
rule by hand now uses `rules discover` (the engine's vocabulary) plus `rule.schema.json`, the same
path the AI skill already follows - same precedent as the earlier `list-standards` removal (see
"Things NOT done" below).

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
   throws `TypeLoadException`) — `CodeGuard.Analyzers.Roslyn.csproj` and
   `CodeGuard.Analyzers.MSBuild.csproj` now use **5.6.0** (latest), matched by
   `Microsoft.CodeAnalysis.Workspaces.MSBuild` 5.6.0. Keep all three in the same Roslyn generation
   if you bump one.
   - `CodeGuard.Analyzers.MSBuild.csproj` still has `Microsoft.Build`/`Microsoft.Build.Framework`
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
   `CodeGuard.Configuration.Parsing` builds these executable instances directly from YAML via
   `SelectorParserRegistry`/`AssertionParserRegistry`, there's no intermediate DTO layer. Keep
   this consistent if you extend the schema.

6. **Known limitation — CLI self-analysis (now resolved).** `codeguard validate` run
   against **this tool's own currently-running solution** (`CodeGuard.sln`) used to reliably crash
   with `System.InvalidOperationException: Sequence contains no elements` inside
   `MsBuildAnalysisProvider.ContributeAsync`, because Buildalyzer's default design-time build ran
   `Clean;Build`, and when Buildalyzer's "common output directory" happened to be this CLI's own
   `bin/` folder, an earlier project's `Clean` step deleted `Buildalyzer.Logger.dll`, which a later
   project's spawned MSBuild process then couldn't find.
   - **This specific crash is fixed** by the Buildalyzer→`MSBuildWorkspace` migration (see gotcha
     #2) — confirmed empirically by running `codeguard validate` against `CodeGuard.sln` after
     the swap. `MSBuildWorkspace`'s design-time build runs in a separate out-of-process BuildHost
     and does no `Clean;Build` sequencing across projects, so this class of shared-output-directory
     collision no longer occurs.
   - **A second crash then surfaced further into the pipeline, and is now also fixed.** Past type
     extraction, `NoPureDelegationOverrideAnalyzer.Analyze`
     (`src/CodeGuard.Evaluation/Analyzers/NoPureDelegationOverrideAnalyzer.cs`) did
     `model.Solutions.SelectMany(...).SelectMany(p => p.Types).ToDictionary(t => t.FullName)`,
     assuming a type's `FullName` is unique across the *entire* repository — but every test project
     in this solution gets an SDK-generated `AutoGeneratedProgram` stub type with an identical name,
     so with 6+ test projects the dictionary threw `ArgumentException: An item with the same key has
     already been added`. **Fixed**: the dictionary is now keyed by `(ProjectName, FullName)`
     instead of bare `FullName`, with a regression test (`NoPureDelegationOverrideAnalyzerTests
     .Analyze_DoesNotThrow_WhenTwoProjectsShareATypeFullName`) covering the multi-project collision
     that caused the crash.
   - **A sibling analyzer had the exact same assumption, found while fixing the above.**
     `ImmutableMutationAnalyzer` (`src/CodeGuard.Evaluation/Analyzers/ImmutableMutationAnalyzer.cs`)
     built its record-name set as a `HashSet<string>` keyed on bare `FullName` — this doesn't throw
     on a repo-wide collision (unlike `ToDictionary`), so instead of crashing it silently
     misattributed mutation-site violations to the wrong project when two projects had an
     identically-named type. **Fixed** the same way (`HashSet<(string ProjectName, string
     FullName)>`), with an analogous regression test
     (`ImmutableMutationAnalyzerTests.Analyze_AttributesCorrectly_WhenTwoProjectsShareATypeFullName`).
     A repo-wide search turned up no other analyzer with this same-class bug — see the other
     `Analyzers/*.cs` files, which either already key by the composite tuple, use containers that
     tolerate duplicates by design (`GroupBy`/`FirstOrDefault`), or intentionally traverse
     cross-project by design (documented in their own class comments).
   - **Self-analysis now completes end-to-end with zero evaluation errors** — confirmed empirically
     (`codeguard validate --path .` against this repo). One more fix was needed to get a *plain*
     `validate --path .` (no `--solution` override) to this state:
     `Cli/Support/SolutionFileLocator.cs`'s directory-skip list didn't exclude `.claude`, and a
     Claude Code git worktree checkout can live at `.claude/worktrees/...` — a full duplicate copy
     of this repo, complete with its own `CodeGuard.sln` and identically-named projects. Without
     skipping it, `validate` discovered *two* solutions and re-tripped the same
     `(ProjectName, FullName)`-uniqueness assumption one level up, across solutions rather than
     within one. `.claude` is now in the skip list alongside `bin`/`obj`/`.git`/etc.
   - **Residual caveat, not yet hit in practice:** `(ProjectName, FullName)` is unique within one
     solution and, empirically, across this repo's own solutions, but nothing *guarantees* it across
     an arbitrary multi-solution repo where the same project name legitimately appears in two
     different `.sln` files on disk (a real repo layout, not a duplicate worktree checkout). Not a
     known failure, just an unproven edge case worth keeping in mind if a similar collision
     resurfaces.
   - Still does **not** affect validating any other repository without a same-name collision —
     proven by `CodeGuard.IntegrationTests` (a real, separate 3-project solution analyzed
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
   `CodeGuard.IntegrationTests` has two test classes that each need MSBuild (via
   `MSBuildWorkspace` and `Microsoft.Build.Evaluation.Project`).
   Originally each had its own `static` constructor guarded by
   `if (!MSBuildLocator.IsRegistered) MSBuildLocator.RegisterDefaults();` — this is **not** safe
   with two classes in one assembly, because xUnit runs test classes in the same assembly in
   parallel by default, and the two independent static-constructor check-then-act races threw
   `InvalidOperationException: MSBuild assemblies were already loaded`. Fixed by moving the
   registration into a single `[ModuleInitializer]` method
   (`CodeGuard.IntegrationTests/MsBuildLocatorInitializer.cs`), which the runtime guarantees
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

11. **Pipeline parallelism (project analysis + rule evaluation) and a BenchmarkDotNet harness were
    added post-v1**, to speed up `validate`'s two most expensive stages. Read this before touching
    `MsBuildAnalysisProvider`, `RuleEvaluator`, or `benchmarks/CodeGuard.Benchmarks`:
    - **`rules/` is gitignored** (`.gitignore:11`) — this repo's real rule set is company-derived
      content that exists only on this machine, so nothing that needs to build/run/CI on another
      machine (benchmarks, new tests) may depend on it. `benchmarks/CodeGuard.Benchmarks/
      SyntheticRuleSetGenerator.cs` replicates the small portable fixture at
      `tests/CodeGuard.IntegrationTests/Fixtures/ExampleRules/` (11 rules) up to ~110 rules instead,
      writing the copies to a temp directory and loading them through the normal
      `RuleFileLoader.CreateDefault().LoadFromDirectory(...)` path.
    - **`MSBuildWorkspace` is not safe for concurrent use.** `MsBuildAnalysisProvider`'s outer loop
      over `solutionPaths` and its single shared `workspace` instance stay strictly sequential.
      Only the *inner* per-project loop is parallelized (`Parallel.ForEachAsync`), because once a
      `Solution` is loaded it's an immutable snapshot and Roslyn guarantees `Project`/`Compilation`/
      `SemanticModel` reads on it are safe to fan out across threads. Don't try to parallelize the
      outer solutions loop or open multiple `MSBuildWorkspace` instances concurrently without
      re-verifying this.
    - **Deterministic-fold-after-parallel-compute is the pattern used everywhere here** — both in
      `MsBuildAnalysisProvider.ContributeAsync` (per-project results written into an indexed
      `ProjectAnalysisResult?[]` array, then folded into `AnalysisModelBuilderContext`/
      `projectModels` sequentially in original group order) and in `RuleEvaluator.Evaluate`
      (per-rule results written into an indexed `RuleOutcome[]` array, then folded into
      `violations`/`evaluationErrors`/counters sequentially in original rule order). This is
      deliberate: it avoids `ConcurrentBag`/locking entirely (indexed array writes never contend)
      **and** keeps output ordering deterministic regardless of which unit of work finishes first,
      which matters for SARIF/JSON output stability. If you "simplify" either loop back to
      `ConcurrentBag<T>.Add`, you reintroduce nondeterministic output ordering even though nothing
      crashes — the `*ParallelismTests` files (see below) exist specifically to catch that.
    - **`EvaluateProjectMetadata`'s per-call `Microsoft.Build.Evaluation.ProjectCollection`** was
      flagged as a concurrency risk to verify empirically, not just reason about, since MSBuild's
      evaluation engine has a history of subtle global-state issues under concurrent evaluation.
      `tests/CodeGuard.IntegrationTests/MsBuildAnalysisProviderParallelismTests.cs` runs
      `MsBuildAnalysisProvider` under forced 8-way parallelism against the 3-project
      `SimpleDomainSolution` fixture, both once (comparing against `maxDegreeOfParallelism: 1`) and
      across 20 repeated runs (races often don't reproduce on a single run) — no issues found as of
      this writing. `tests/CodeGuard.Core.Tests/Evaluation/RuleEvaluatorParallelismTests.cs` does
      the equivalent for `RuleEvaluator` with a synthetic 50-rule set.
    - **`--max-parallelism <int>`** on `validate` (default: `Environment.ProcessorCount`) threads
      through to both `MsBuildAnalysisProvider`'s constructor and `RuleEvaluator.Evaluate`. Setting
      it to `1` forces fully sequential execution — a troubleshooting escape hatch if a concurrency
      bug ever surfaces in the field, without needing a code revert.
    - **`benchmarks/CodeGuard.Benchmarks`** (BenchmarkDotNet, run via `scripts/run-benchmarks.sh`,
      which always forces `-c Release` since Debug JIT output makes timing numbers meaningless -
      not wired into `dotnet test`/CI, see the script's own header comment for why) benchmarks
      `AnalysisModelBuilder.BuildAsync` against this repo's own tracked `CodeGuard.sln` (10 real
      projects — a stand-in for a realistic multi-project repo, since the 3-project
      `SimpleDomainSolution` fixture is too small to show a parallel speedup) and
      `RuleEvaluator.Evaluate` against the synthesized rule set above plus a synthetic
      `RepositoryModel` built directly in `SyntheticModelBuilder.cs` (no MSBuild involved, so the
      rule-evaluation benchmark measures rule-evaluation cost in isolation). `BuildAsync` only
      exercises MSBuild solution loading, which CLAUDE.md's "Known limitation" section already
      confirms is fixed for self-analysis — it never reaches the unrelated
      `NoPureDelegationOverrideAnalyzer` crash further down the `validate` pipeline.
    - `validate` also now logs stage durations (`Analysis model built in {ms} ms` /
      `Evaluation complete in {ms} ms`) at `Information` level via a plain `Stopwatch` in
      `ValidateCommand.cs` — no BenchmarkDotNet dependency needed for a user running `validate` on
      their own repo to see where time is going.

## Things NOT done (explicitly deferred, per the plan)

- `when`/`and`/`or`/`not` YAML parsing is **done** — `ConditionParserRegistry`
  (`CodeGuard.Configuration.Parsing`) wires `AndCondition`/`OrCondition`/`NotCondition` into
  `RuleDocumentParser`/`RuleDefinition.When`/`RuleEvaluator`, with schema support (`whenNode` in
  `rule.schema.json`) and tests (`ConditionCompositionTests`, `ConditionParserRegistryTests`) — this
  bullet was stale. The `Any`/`All`/`None` *quantifier* gap (over a set of candidates, as opposed to
  `And`/`Or`/`Not`'s single-candidate conditions) is also **done** — see `must_all_match`/
  `must_any_match`/`must_none_match` in the "Post-v1 addition" section above.
- Cross-entity correspondence (`MustHaveCorresponding`/`MustHaveOneToOne`/`MustHaveOneToMany`) is
  **done** — fully expressible via the existing `must_have_count` plus the `type` selector's new
  `name:` param, no new assertion kind needed (see the same "Post-v1 addition" section above).
  `MustHaveMatching`/`MustHaveRelated` (undifferentiated from `MustHaveCorresponding` in
  PRIMITIVES.md, no separate example given) remain unimplemented as named kinds pending a concrete
  use case.
- Method-body assertions (`MustCall`, `MustAwait`, etc.), naming-convention assertions
  (`MustUsePascalCase` etc. — already expressible via `must_match_name`'s regex, so low priority),
  generic relationship assertions (`MustBeRelatedTo`/`MustHaveParent`/ownership-graph concepts —
  no concrete use case yet), package version-range constraints (`MustUsePackageVersionAtLeast`),
  property-setter assertions (`MustNotHaveSetter`).
- Non-C# analysis providers (YAML/JSON/Terraform/K8s/etc.) — architecture left open via
  `IAnalysisProvider`, nothing implemented.
- A dedicated standards-file format was never built. The `RuleDefinition.Standard` field and the
  `list-standards`/`list-rules --standard` commands that surfaced it were later **removed**
  (`docs/done/RULE_VALIDATION_PLAN.md`-adjacent cleanup, post-Stage B): the field had two mutually
  incompatible value conventions in practice — short codes (`DDD-001`) on the 11 hand-authored
  rules vs. markdown-doc-path/anchor or `SKILL.md` values (from `rules.generated.json`, a different
  target repo) on the 97 generated rules — so `list-standards` produced ~60 mostly-singleton groups
  instead of a meaningful category list. `Documentation` (`IReadOnlyList<string>`) remains on
  `RuleDefinition` as the intended doc-reference field but is unpopulated by any current rule file.
- Everything in `CodeGuard/REFACTORING.md` (analysis sessions/caching, rule versioning and
  lifecycle states, the Selector/Predicate/Assertion/Diagnostic split, a custom-analyzer escape
  hatch, rule fixture testing) — a deliberately separate, larger initiative the user chose not to
  start yet. See "Where things stand" above.
- A fix for the remaining CLI self-analysis known limitation (gotcha #6) — the original
  Buildalyzer-crash cause is resolved, but `NoPureDelegationOverrideAnalyzer`'s
  `FullName`-uniqueness assumption still blocks full self-validation; documented but not solved.
