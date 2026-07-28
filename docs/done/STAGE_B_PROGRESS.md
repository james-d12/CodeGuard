# Stage B Progress — handoff notes (Stage B fully complete: Waves 0-4 all shipped)

> Status as of this update: **Stage B is done.** Waves 0-3 (all primitives + all 10 custom
> analyzers, including a post-hoc genericity pass renaming/regeneralizing 8 of 11 analyzers after
> user review — 246 tests passing) are complete and verified. **Wave 4 (rule authoring) is now also
> complete**: 51 new `rules/generated/*.yml` files (50 rules, one split into 2 files) cover 50 of
> the 60 original Stage B rule IDs; 3 stay unenforced (IDE-analyzer diagnostics the compiler's
> `GetDiagnostics()` can't surface) and 7 are honest gaps (need engine facts not modeled — see
> `docs/RULE_COVERAGE_STAGE_A_RULES.md`'s "Stage B — final classification" table for the full
> per-rule mapping and reasoning). **The class/kind names used earlier in this document's Batch
> 1/2/3 sections are stale** — the "Post-Wave-3 genericity pass" section below has the final
> name/kind/param mapping; treat the Batch sections as historical design-rationale notes, not the
> current API. Read this file first, then `docs/RULE_COVERAGE_PLAN.md` (original
> design) and `docs/RULE_COVERAGE_STAGE_A_RULES.md` (rule-by-rule classification, including the
> Stage B table this work is closing out) for full context. The approved execution plan (waves,
> file lists, risk resolutions) was written to
> `/home/james/.claude/plans/vectorized-noodling-sunbeam.md` — that plan file may or may not still
> exist depending on session cleanup; this document is the durable record, don't rely on the plan
> file being there. **Nothing in this doc has been committed to git yet** — check `git status`
> before assuming any of it is checked in.

## What's done and verified (build + tests green, 197 tests passing as of the last full run)

**Wave 0 — `must_exist`/`must_not_exist`** (generic cardinality assertions, shipped standalone
ahead of call-site work per user decision):
- `src/RulesEngine.Evaluation/Assertions/{MustExistAssertion,MustNotExistAssertion,SelectorTemplateResolver}.cs`
- `src/RulesEngine.Configuration/Parsing/{MustExistAssertionParser,MustNotExistAssertionParser}.cs`
- Design: assertion holds a raw `JsonObject` selector template + a `Func<JsonObject, ITargetSelector>`
  factory (closed over `SelectorParserRegistry` at parse time, so `RulesEngine.Evaluation` never
  references Configuration). `SelectorTemplateResolver` substitutes `${PropertyName}` string leaves
  via reflection over the outer candidate (e.g. `${FullName}` on a `TypeModel`).
- `DefaultParsers.CreateAssertionRegistry` now takes a `SelectorParserRegistry` parameter (both
  registries must be built selector-first now) — every call site updated.
- Added a `name` (glob) filter to `MethodSelector`/`MethodSelectorParser` (prerequisite for
  `skill.eventhandler.unit-test-status-coverage`, which needs no placeholder scoping at all).

**Wave 1 — `CallSiteModel` + syntax extraction + `call_site` + `must_match_argument`**:
- `src/RulesEngine.Analysis/AnalysisModel/CallSiteModel.cs` — `CallSiteKind` enum
  (Invocation/ObjectCreation/MemberAccess), `CallSiteArgument(Index, LiteralValue, IsLiteral)`,
  `CallSiteModel(...)` with trailing optional `EnclosingComparisonOperator`/`EnclosingComparisonValue`
  (added specifically to reclassify `BlockingCountPatternAnalyzer`'s `.Count() > 0` half as
  declarative — see below).
- `src/RulesEngine.Analysis/AnalysisModel/SyntaxFacts.cs` — `SwitchModel`, `ThrowSiteModel`,
  `MutationSiteModel`, `TryBlockModel`, `MethodBodyShapeModel` (all 5 Wave-3a fact types, defined
  now so `RepositoryModel`'s breaking change only happened once — see Risk 6 in the plan).
- `RepositoryModel` now has **10** positional params:
  `(RootPath, Solutions, Files, CallSites, Switches, ThrowSites, MutationSites, TryBlocks, MethodBodyShapes, Diagnostics)`.
  Every direct construction across the test suite was mechanically updated (sed-based batch fix,
  twice — once for the first 6 new lists in Wave 1, once more for `Diagnostics` in Wave 2).
  `tests/RulesEngine.Evaluation.Tests/TestModels.cs` has both the original
  `Repository(params ProjectModel[])` (unchanged signature, back-fills the new lists with `[]`) and
  a new `RepositoryWithFacts(projects?, files?, callSites?, switches?, throwSites?, mutationSites?, tryBlocks?, methodBodyShapes?, diagnostics?)`
  helper (all optional named params) — **use this one for all Wave 3 analyzer tests**.
- `src/RulesEngine.Analyzers.Roslyn/{RoslynSyntaxFactExtractor,SyntaxFactWalker,SyntaxFactSink}.cs`
  — a new whole-syntax-tree walk (sibling to `RoslynTypeExtractor`'s existing symbol-only walk, not
  an extension of it — Program.cs top-level statements have no natural home in a per-`IMethodSymbol`
  walk). `SyntaxFactWalker` currently implements all the visitors needed for Waves 1 AND 3a in one
  pass (this was done in one go rather than deferred, since the walker infrastructure was already
  being built): `VisitInvocationExpression`, `VisitObjectCreationExpression`,
  `VisitMemberAccessExpression` (Wave 1), plus `VisitSwitchStatement`, `VisitSwitchExpression`,
  `VisitThrowStatement`, `VisitThrowExpression`, `VisitAssignmentExpression`, `VisitTryStatement`,
  `VisitMethodDeclaration` (Wave 3a — **already done, not just reserved**). All confirmed working via
  `tests/RulesEngine.Analyzers.Roslyn.Tests/RoslynSyntaxFactExtractorTests.cs` (29 tests, including a
  spike test proving `semanticModel.GetEnclosingSymbol` resolves correctly for Program.cs top-level
  statements — Risk 2 from the plan is resolved, confirmed working).
- `src/RulesEngine.Evaluation/Selectors/CallSiteSelector.cs` + parser — `kind: call_site`, filters
  `site_kind`/`invoked_member`/`target_type`/`project`/`containing_method`/`containing_type`, plus
  `argument_index`+`argument_is_literal` and `enclosing_comparison` (both added specifically for the
  two reclassification wins below).
- `src/RulesEngine.Evaluation/Assertions/MustMatchArgumentAssertion.cs` + parser — `{index, pattern}`.
- `RuleEvaluator.ExtractLocation` extended with a `CallSiteModel` case.
- **Two custom analyzers from the original Phase B4 table were reclassified as declarative** (per
  Risk 3 in the plan — attempted before writing bespoke classes, both succeeded):
  - `EndpointRouteConstantAnalyzer` (`golden.eventhandler.route-constants`) → now expressible as
    `call_site` (filtered to `Map*` methods, `argument_index: 0, argument_is_literal: true`) +
    `must_not_exist`, since a literal route argument is exactly the violation (should be a named
    constant instead).
  - `BlockingCountPatternAnalyzer`'s `.Count() > 0` half (`coding.perf.no-blocking-count`) → now
    expressible as `call_site` (filtered to `*.Count`, `enclosing_comparison: ">"`) + `must_not_exist`.
    (Its `.Result`/`.Wait()` half was already declarative from Stage A/earlier Wave 1 work — this
    rule is now **fully** declarative, not split.)
  - **Net effect: 11 bespoke analyzer classes remain, not 13** (`RoslynDiagnosticPassthroughAnalyzer`
    already done + 10 more to go, listed below).

**Wave 2 — `ICustomAnalyzer` extension point** (originally shipped with a shared-instance-by-name
design; **reworked mid-Wave-3 after user feedback that the first 2 Batch-1 analyzers hardcoded
org-specific namespace prefixes in C#, violating "rules are declarative YAML, engine stays generic"**
— see the note right after this list):
- `src/RulesEngine.RuleModel/Analyzers/{ICustomAnalyzer,AnalyzerViolation}.cs` — placed in
  `RuleModel` (not Evaluation/Configuration) so both `Core` and `Configuration` can reference it
  without breaking the dependency graph. (`CustomAnalyzerRegistry` from the original Wave 2 design
  was deleted in the rework below — superseded by `AnalyzerParserRegistry`.)
- `RuleDefinition.Target`/`Assertions` are `nullable`/optional (dropped `required`); added
  `Analyzer: ICustomAnalyzer?` (the **constructed, already-configured instance** — not a name to
  resolve later, exactly like `Target`/`Assertions`). **This is a plain `sealed class`, not a
  record — no `with` expressions work on it.**
- `RuleDocumentParser.Parse` takes an `AnalyzerParserRegistry` param; branches on
  `document["analyzer"]` presence (now an **object** node, not a string); throws if both `analyzer`
  and `target`/`assertions` are present, or if the analyzer `kind` isn't registered.
- `RuleEvaluator` takes **no analyzer-registry constructor param at all** — `EvaluateAnalyzerRule`
  just calls `rule.Analyzer!.Analyze(model)` directly, same shape as how `EvaluateSelectorRule`
  calls `rule.Target!.SelectCandidates(model)`.
- `rules/schema/rule.schema.json`: root `required` is now just `[id, name]`, with
  `"oneOf": [{"required":["target","assertions"]}, {"required":["analyzer"]}]`, plus a new
  `"analyzer"` property — **an object requiring `kind`** (mirrors the `target` property's shape,
  not a bare string). **Important discovered behavior**: if a document sets BOTH
  `analyzer` and `target`+`assertions`, the **schema** rejects it (both `oneOf` branches match →
  `RuleSchemaValidationException`) before `RuleDocumentParser`'s own mutual-exclusivity check would
  ever run — a test confirms this (`LoadFromFile_WithAnalyzerAndTarget_ThrowsSchemaValidationException`).
- `ExplainRuleCommand` prints `Analyzer: {name}` (now `rule.Analyzer.Name`) instead of
  `Target kind:`/`Assertions:` when `rule.Analyzer is not null`.
- `RoslynDiagnosticPassthroughAnalyzer` (`src/RulesEngine.Evaluation/Analyzers/`) — reads
  `RepositoryModel.Diagnostics` (populated by a new `RoslynDiagnosticExtractor.cs` in
  `RulesEngine.Analyzers.Roslyn`, wired into `MsBuildAnalysisProvider`). **Important
  scope-narrowing decision**: the plan's Risk 1 spike was run for real —
  `Microsoft.CodeAnalysis.CSharp.CodeStyle` (needed for IDE0005/IDE0011/IDE0161) resolves fine at
  the pinned 4.10.0 version, but its DLLs only exist under the NuGet package's `analyzers/` asset
  folder (compiler-consumed via MSBuild, not a normal `lib/` reference) — driving those analyzers
  would require fragile `Assembly.LoadFrom` + reflection to find `DiagnosticAnalyzer` types and run
  them via `CompilationWithAnalyzers` manually. **Decision made: don't do this.** Only **CS1591**
  (pure compiler diagnostic via `compilation.GetDiagnostics()`, zero extra dependencies) is wired
  up in practice, but the analyzer itself takes `diagnostic_ids` as a YAML list param (kind
  `roslyn-diagnostic-passthrough`) rather than a hardcoded list, so a rule author could opt in to
  other compiler diagnostics without touching the engine. **This means 3 of the 4 rules this
  analyzer was originally meant to cover (`coding.format.using-directives`/IDE0005,
  `coding.format.braces-required`/IDE0011, `coding.lang.file-scoped-namespaces`/IDE0161) are NOT
  coverable today** — only `coding.api.xml-docs-present` (CS1591) is. This should be recorded as a
  known gap when Wave 4 authors the rule files, not silently worked around.
- Also: added `ConstantValue: string?` to `FieldModel` (trailing optional param, non-breaking),
  populated via `field.HasConstantValue ? field.ConstantValue?.ToString() : null` in
  `RoslynTypeExtractor.ToFieldModel` — a small model gap found while scoping
  `TypenameConsistencyAnalyzer` (below), which genuinely needs a C# const's literal value and the
  model had no way to expose it before.

**Mid-Wave-3 rework — analyzers now take their params from YAML, not hardcoded C#:** after Batch 1
was first written, `NoBusinessExceptionsAnalyzer` and `SingleCatchBlockAnalyzer` hardcoded
`"Contoso.Domain"`/`"Contoso.Application"` namespace prefixes directly in the analyzer class. The
user flagged this as a real violation of this repo's core principle (CLAUDE.md: rules are
declarative YAML so new organisational conventions don't require touching the engine) — the
original Wave 2 design gave `ICustomAnalyzer` **zero** rule-specific input (`Analyze(RepositoryModel
model)` only), unlike selectors/assertions which are *constructed from* their YAML params, so any
analyzer needing scope had nowhere to get it from except hardcoding it. Fix chosen (of two options
presented — "per-analyzer config params" over "generic scope filter"): mirror the
selector/assertion pattern exactly. New files:
- `src/RulesEngine.Configuration/Parsing/IAnalyzerParser.cs` — `Kind` + `Parse(JsonObject) ->
  ICustomAnalyzer`, mirrors `ISelectorParser`.
- `src/RulesEngine.Configuration/Parsing/AnalyzerParserRegistry.cs` — dispatches on the analyzer
  node's `kind` property, mirrors `SelectorParserRegistry`.
- One parser class per analyzer kind (e.g. `NoBusinessExceptionsAnalyzerParser`,
  `SingleCatchBlockAnalyzerParser`) in `RulesEngine.Configuration.Parsing`, each constructing a
  fresh, rule-specific instance from that rule's YAML.
- `NoBusinessExceptionsAnalyzer`/`SingleCatchBlockAnalyzer` now take a `namespacePattern`
  constructor param (glob, matched via `GlobMatcher.IsMatch` per this repo's existing
  no-`StartsWith`-exact-matching convention), read from an optional `namespace` YAML key
  (`?? "*"` if the rule doesn't scope it). Example YAML now:
  `analyzer: { kind: no-business-exceptions, namespace: "Contoso.Domain.*" }`.
- **This same "take it from YAML, don't hardcode it in C#" rule applies to every analyzer still to
  be written in Batch 2/3 below** — several of their original design notes (written before this
  rework) named specific Contoso-only strings that must now become parser-read YAML params instead
  of C# literals. Each affected item below has been annotated accordingly.

**Post-Wave-3 genericity pass — the authoritative name/kind/param table.** After all 10 Wave 3
analyzers were parameterized (scope params, no hardcoded namespaces), the user did a second review
and pointed out several analyzers still carried business-specific framing beyond just a missing
scope param - either the class *name* baked in one example use case (e.g.
`PartitionKeyBuilderCardinalityAnalyzer` for what's really a generic "one companion type per
marker-interface type" check), or a piece of *behavior* was hardcoded that should have been a
parameter (e.g. "throw only allowed as guard clause" baked into `NoBusinessExceptionsAnalyzer`
instead of being a toggle). 8 of the 11 analyzers were renamed/regeneralized; every rename kept the
original example configuration reproducible via explicit param values (usually just the defaults).
`NoPureDelegationOverrideAnalyzer` and `RoslynDiagnosticPassthroughAnalyzer` were judged already
generic and left alone. Final state, `kind` → class → params:

| `kind` (YAML) | Class | Params | Notes |
|---|---|---|---|
| `roslyn-diagnostic-passthrough` | `RoslynDiagnosticPassthroughAnalyzer` | `diagnostic_ids` (list, required) | unchanged |
| `exhaustive-switch` | `ExhaustiveSwitchAnalyzer` (was `SwitchExhaustiveAuditVersionAnalyzer`) | `namespace` (default `"*"`) | renamed off "audit version" (an artifact of the rule id, not the logic); gained a scope param it never had |
| `no-exceptions` | `NoExceptionsAnalyzer` (was `NoBusinessExceptionsAnalyzer`) | `namespace` (default `"*"`), `allow_guard_clause` (bool, default `false`) | `false` = flag every throw in scope (generic "no exceptions here"); `true` = old guard-clause-only behavior. Original `skill.domain.no-business-exceptions` config: `namespace: Contoso.Domain.*, allow_guard_clause: true` |
| `immutable-mutation` | `ImmutableMutationAnalyzer` | `namespace` (default `"*"`) | unchanged name/logic; this was the only analyzer with **zero** scope param, now fixed |
| `catch-clause-count` | `CatchClauseCountAnalyzer` (was `SingleCatchBlockAnalyzer`) | `namespace` (default `"*"`), `min_catches`/`max_catches` (both default `1`) | generalizes "exactly one" into a configurable range |
| `member-ordering` | `MemberOrderingAnalyzer` | `order` (list of `fields`/`constructors`/`properties`/`methods`, default `[fields, constructors, properties, methods]`) | the rank convention is now itself a param, not an assumption; a group name omitted from a custom list sorts after every named group |
| `no-pure-delegation-override` | `NoPureDelegationOverrideAnalyzer` | `base_type_pattern` (default `"*"`) | unchanged — already generic |
| `companion-type-cardinality` | `CompanionTypeCardinalityAnalyzer` (was `PartitionKeyBuilderCardinalityAnalyzer`) | `marker_interface` (required; renamed from `aggregate_root_interface`), `companion_suffix` (**required**, no default; renamed from `builder_suffix`) | generic "every X implementing marker interface must have exactly one type named `{X.Name}{suffix}`"; original partition-key config: `marker_interface: Contoso.Domain.IAggregateRoot, companion_suffix: PartitionKeyBuilder` |
| `duplicate-attribute-argument` | `DuplicateAttributeArgumentAnalyzer` (was `EventIdReservationBlockAnalyzer`) | `attribute_name` (required), `argument_index` (default `0`) | generic repo-wide "no two members may share this attribute argument value"; original event-ID config: `attribute_name: EventIdAttribute` (index defaults to 0) |
| `const-yaml-value-consistency` | `ConstYamlValueConsistencyAnalyzer` (was `TypenameConsistencyAnalyzer`) | `const_type`, `const_name`, `yaml_file_pattern`, `yaml_field_path` (all required) | cosmetic rename only, no behavior change — was already fully generic |
| `project-convention` | `ProjectConventionAnalyzer` (was `DbUpBootstrapAnalyzer`) | `project_pattern` (required), `required_call_pattern` (default `"*DeployChanges*"`), `required_content_folder` (default `"Scripts"`) | generic "matching projects must have call-site X and ship folder Y as `<Content>`"; original DbUp config: `project_pattern: "*.Reporting*"` (the two defaults already match DbUp's shape, so no override needed) |

All renames were free — no committed `rules/*.yml` file referenced any `analyzer:` kind yet (Wave 4
hadn't started), so there was no migration cost. **When Wave 4 authors rule files, use the `kind`
names and param names in this table, not the ones in the Batch 1/2/3 sections below** (those
sections are kept for their design rationale, but their class names, `kind` strings, and some param
names are now stale — e.g. `builder_suffix` is `companion_suffix`, `bootstrap_call_pattern` is
`required_call_pattern`).

All of the above, plus all of Wave 3 and both generalization passes, is in the working tree
(uncommitted, not yet a git commit — check `git status`), builds clean (0 errors), and passes the
full test suite (246 tests as of the last run: `dotnet build && dotnet test`).

## Status: nothing left in progress — Stage B is fully shipped

**Wave 3 — the 10 remaining custom analyzers. All 10 are now done**, including tests,
`DefaultAnalyzers.cs` registration, and manual CLI verification. See the completion checklist after
Batch 3 below.

**Wave 4 — rule authoring. Also done.** 51 files in `rules/generated/*.yml` (50 rules, one split
into `-001`/`-002`), covering 50 of the original 60 Stage B rule IDs. The full per-rule
file/primitive mapping — including the 3 uncoverable IDE-diagnostic rules and the 7 genuine gaps —
now lives in `docs/RULE_COVERAGE_STAGE_A_RULES.md`'s "Stage B — final classification" section, not
duplicated here. `dotnet build && dotnet test` still green (246 tests, unchanged — rule YAML files
aren't part of the unit test suite, only exercised via `ValidateEndToEndTests` against the repo's
own `rules/` directory and via manual CLI `list-rules`/`explain-rule`/`validate` runs against a
scratch fixture copy, both of which passed cleanly with all 120 total rules loaded and 0 crashes).

The rest of this document (below) is Wave 3's design/handoff detail, preserved as-is for historical
context — nothing below needed further changes for Wave 4.

### The remaining analyzers, with concrete design notes (so the next session doesn't have to re-derive them)

All go in `src/RulesEngine.Evaluation/Analyzers/`, all `ICustomAnalyzer`, all registered in
`src/RulesEngine.Configuration/Parsing/DefaultAnalyzers.cs`. Tests go in
`tests/RulesEngine.Evaluation.Tests/Analyzers/<Name>Tests.cs` using
`TestModels.RepositoryWithFacts(...)` (see Wave 1 notes above) — no Roslyn needed for analyzer-logic
tests, only for the extraction-side tests already written in
`RoslynSyntaxFactExtractorTests.cs`.

**Batch 1 — done:**
1. ~~`SwitchExhaustiveAuditVersionAnalyzer`~~ — done, tested
   (`tests/RulesEngine.Evaluation.Tests/Analyzers/SwitchExhaustiveAuditVersionAnalyzerTests.cs`),
   registered (parser kind `"switch-exhaustive-audit-version"`, no params). Already generic — no
   rework needed.
2. ~~`NoBusinessExceptionsAnalyzer`~~ (`skill.domain.no-business-exceptions`) — done, tested,
   parser kind `"no-business-exceptions"`. Flags every `ThrowSiteModel` in `model.ThrowSites` where
   `GlobMatcher.IsMatch(ContainingType, namespacePattern)` (namespace pattern is a YAML `namespace`
   param, default `"*"`) AND `IsFirstStatementInMethod == false`.
3. ~~`ImmutableMutationAnalyzer`~~ (`skill.domain.immutable-mutation`) — done, tested, parser kind
   `"immutable-mutation"`, no params. Flags every `MutationSiteModel` whose `ContainingType` matches
   a `TypeModel.FullName` with `Kind == TypeKind.Record` (cross-referenced via
   `model.Solutions.SelectMany(s => s.Projects).SelectMany(p => p.Types)`) AND whose
   `ContainingMethod != ".ctor"`. Already generic (keyed on `TypeKind`, not a namespace) — no rework
   needed.
4. ~~`SingleCatchBlockAnalyzer`~~ (`skill.application.single-catch-block`) — done, tested, parser
   kind `"single-catch-block"`. Flags every `TryBlockModel` where
   `GlobMatcher.IsMatch(ContainingType, namespacePattern)` (YAML `namespace` param, default `"*"`)
   AND `CatchClauseCount != 1`.

**Batch 2 — done:**
5. ~~`MemberOrderingAnalyzer`~~ (`coding.type.member-ordering`) — done, tested
   (`tests/RulesEngine.Evaluation.Tests/Analyzers/MemberOrderingAnalyzerTests.cs`), parser kind
   `"member-ordering"`, no params (the Fields=0/Constructors=1/Properties=2/Methods=3 rank
   convention is an engine-level default, not org-specific, so it's a hardcoded constant in the
   class by design — this is the one Batch-2/3 analyzer the "take it from YAML" rule doesn't apply
   to). Reconstructs declaration order from existing `TypeModel.{Fields,Constructors,Properties,
   Methods}` (each member already carries a `Line`), sorts by `Line`, flags the first member whose
   rank is lower than the max rank seen so far.
6. ~~`NoPureDelegationOverrideAnalyzer`~~ (`golden.persistence.repository-base-class`, delegation
   clause only — the "derives from a repository base type" clause is already declarative via
   `must_inherit_from`, per the source plan). Done, tested, parser kind
   `"no-pure-delegation-override"`, takes a `base_type_pattern` YAML param (default `"*"`, glob via
   `GlobMatcher` — no longer hardcoded to `"DomainEntityRepository"`). Flags every
   `MethodBodyShapeModel` where `IsSingleBaseCallDelegation == true` AND the method's
   `ContainingType` resolves (via a `FullName`-keyed `TypeModel` lookup) to a type whose `BaseType`
   matches the pattern.
7. ~~`PartitionKeyBuilderCardinalityAnalyzer`~~ (`skill.persistence.partition-key-builder-class`) —
   done, tested, parser kind `"partition-key-builder-cardinality"`, takes a **required**
   `aggregate_root_interface` YAML param (glob via `GlobMatcher`, checked against `Interfaces` — no
   default, since there's no sane repo-agnostic fallback; parser throws `RuleParsingException` if
   omitted) and an optional `builder_suffix` param (default `"PartitionKeyBuilder"`). For every
   `TypeModel` implementing the configured interface, derives the aggregate name (`type.Name`) and
   checks there is exactly one `TypeModel` named `$"{aggregateName}{builderSuffix}"` (matched on
   `.Name`, not `.FullName` — namespace doesn't matter for this cardinality check) anywhere in the
   repo; flags if zero or more than one, attributed to the aggregate root's own location.

**Batch 3 — done:**
8. ~~`EventIdReservationBlockAnalyzer`~~ (`skill.reporting.eventid-reservation-blocks`) — done,
   tested, parser kind `"eventid-reservation-block"`, takes a **required** `attribute_name` YAML
   param (glob via `GlobMatcher` against `AttributeModel.TypeName` — no default, parser throws if
   omitted; no longer hardcoded to `"EventIdAttribute"`). Repo-wide duplicate-value detection: scans
   every method/property/field's `Attributes` for a match and takes the first
   `ConstructorArgumentLiterals` entry as the ID value; groups all (owning member, ID) pairs by ID
   across the **entire repository**; flags every member in a group where the ID appears more than
   once (this is the one analyzer needing a genuinely cross-cutting whole-repo pass, not a
   per-candidate check — each duplicate gets its own violation, attributed to its own owning
   member's location, not a single combined violation).
9. ~~`TypenameConsistencyAnalyzer`~~ (`skill.client-config.typename-consistency`) — done, tested,
   parser kind `"typename-consistency"`, takes 4 **required** YAML params (`const_type`,
   `const_name`, `yaml_file_pattern`, `yaml_field_path` — no sane repo-agnostic defaults for any of
   them, parser throws if any are omitted). `<PackageReference Include="YamlDotNet" Version="18.1.0" />`
   was added to `RulesEngine.Evaluation.csproj` (same version already pinned in
   `RulesEngine.Configuration.csproj`). Compares a C# `const` field's value (via
   `FieldModel.ConstantValue`, added earlier this session) against a dotted-path field in a matching
   YAML file, read via a small new `YamlFieldPath.Resolve` helper
   (`src/RulesEngine.Evaluation/Analyzers/YamlFieldPath.cs`) that walks `YamlDotNet.RepresentationModel`
   nodes directly — **deliberately not** reusing `RulesEngine.Configuration`'s
   `YamlDocumentReader`/full YAML-to-`JsonNode` conversion, since that class is `internal` to
   `Configuration` and this only needed scalar lookup by dotted path, not full JSON conversion.
10. ~~`DbUpBootstrapAnalyzer`~~ (`skill.reporting.dbup-bootstrap`) — done, tested, parser kind
    `"dbup-bootstrap"`, takes a **required** `project_pattern` YAML param plus two optional ones
    (`bootstrap_call_pattern` default `"*DeployChanges*"`, `scripts_folder_name` default
    `"Scripts"` — these two do have sane engine-level defaults, unlike `project_pattern` which is
    inherently repo-specific). Composite check per matching project: (a) at least one
    `CallSiteModel` with `InvokedMember` matching the call pattern, (b) the project's own `.csproj`
    (read directly via `ProjectModel.Path`, **not** via a `RepositoryModel.Files` lookup as
    originally sketched — `ProjectModel` already carries its own file path) parsed with
    `System.Xml.Linq` for a `<Content Include=...>` entry whose value contains the scripts folder
    name. Flags the project (one violation, not two) if either check fails, naming which one(s)
    failed in the message. This is intentionally an approximation of the full original description
    (composite bootstrap + build-action + filename pattern) — the filename-pattern half is already
    covered declaratively by Stage A's `golden-persistence-dbup-script-naming-001.yml`.

### Wave 3 completion checklist — all done

1. ~~Register all remaining analyzer parsers~~ — `src/RulesEngine.Configuration/Parsing/DefaultAnalyzers.cs`'s
   `CreateRegistry()` lists all 11 parsers using the **final** names from the genericity-pass table
   above (the original Wave 2 `roslyn-diagnostic-passthrough` plus all 10 Wave 3 analyzers).
2. ~~`dotnet build && dotnet test`~~ — 0 errors, **246 tests passing** (197 at the start of Wave 3;
   +41 from the 10 new analyzers' original tests, -1 from removing the `RuleEvaluator`-level
   "unknown analyzer name" test superseded by the analyzer-config rework, +9 from new test cases
   added during the genericity pass to cover the new params/behaviors).
3. ~~Final CLI verification~~ — done twice: once right after Wave 3 (before the genericity-pass
   renames, against a `WAVE3-VERIFY-001` scratch rule), and again after Wave 4 shipped all 51 rule
   files — `list-rules` loads all 120 rules repo-wide, `explain-rule` spot-checked one `call_site`
   rule, one `must_exist`/`must_not_exist` rule, and one `analyzer:` rule (all printed correctly),
   and `validate` against a scratch copy of `SimpleDomainSolution` with the full `rules/` directory
   evaluated all 120 rules without a single crash (94 passed / 26 failed on the fixture, which is
   expected — the fixture doesn't conform to most of the illustrative Contoso-specific conventions).
4. ~~Wave 4~~ — done: authored `rules/generated/*.yml` for 50 of the 60 Stage B rules (51 files,
   one rule split into `-001`/`-002`), and extended `docs/RULE_COVERAGE_STAGE_A_RULES.md` in place
   with the final Stage B classification table (rule ID → file → primitive), including the 2
   reclassifications from Wave 1 (`golden.eventhandler.route-constants`,
   `coding.perf.no-blocking-count`'s `.Count()` half) shipped as `call_site` rules, not bespoke
   analyzers. 3 rules stay unenforced (uncoverable IDE-diagnostic rules) and 7 are genuine gaps
   (each needs an engine fact not modeled) — both are named explicitly in that table rather than
   silently skipped. All `analyzer:`-kind rule files use the `kind`/param names from the
   "Post-Wave-3 genericity pass" table above, not the stale Batch 1/2/3 names.

## Known gaps / honest limitations to carry forward

- **IDE0005/IDE0011/IDE0161 are not coverable** (see Wave 2 notes above) — only CS1591 passthrough
  works. 3 of the original 4 `RoslynDiagnosticPassthroughAnalyzer` rule targets stay unenforced
  unless a future session solves the analyzer-loading problem properly (e.g. vendoring the specific
  analyzer types some other way, or accepting a `dotnet format analyzers --verify-no-changes`
  shell-out as a different kind of "custom analyzer").
- `MemberOrderingAnalyzer`'s **default** member order (`[fields, constructors, properties,
  methods]`) is an inherent convention choice (not derived from any spec) — it's now overridable
  via the `order` YAML param (see genericity-pass table above), but the default itself is still a
  judgment call worth knowing isn't derived from a real spec.
- `DuplicateAttributeArgumentAnalyzer`'s example attribute name (`EventIdAttribute`) and
  `ConstYamlValueConsistencyAnalyzer`'s specific const/YAML-field pairing (when Wave 4 configures
  them for the original `skill.reporting.eventid-reservation-blocks`/
  `skill.client-config.typename-consistency` rules) are both illustrative judgment calls consistent
  with how every other Stage A/B rule in this repo invents plausible `Contoso.*`-specific
  conventions — not a gap, just worth knowing they're invented, not derived from a real spec. Both
  analyzers are now fully generic (the attribute name/field pairing are rule YAML params, not
  hardcoded), so this is purely about what value Wave 4 chooses to put in the rule file, not a
  limitation of the analyzer.
