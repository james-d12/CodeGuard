# Rule Coverage Plan — extending the engine to support every deterministic rule in rules.generated.json

> Status: **planned, not started**. This is a design/implementation plan, not yet executed. Picking
> this up: start with Stage A in full (build + ship), then Stage B. See "Verification" at the end for
> gating between stages.

## Context

`rules.generated.json` contains ~90 candidate rules with `enforcement.classification` values of
`deterministic`, `partially_deterministic`, `ai_review`, `human_review`. Per the user's instruction,
non-deterministic rules (`partially_deterministic`/`ai_review`/`human_review` — the `hybrid.*` and
`prob.*` prefixed rules) are disregarded entirely.

Of the 135 `deterministic` rules, 5 are already converted and enforced (`rules/generated/*.yml`, done
in a prior session). Two research passes (an Analysis-model capability audit + a rule-by-rule gap
categorization) established that the remaining **130 deterministic rules** split into:

- **111 rules** genuinely about this repo's charter — "a deterministic analysis/validation engine...
  against .NET repositories" (`CLAUDE.md`) — that the engine can be extended to check.
- **19 rules that are explicitly out of scope** and will **not** be implemented:
  - **16 rules** whose `target.kind` isn't a .NET-repository artifact at all (`agent-output`,
    `markdown-table-row`, `terraform-resource`, `bruno-collection`, `skill-document`) — these describe
    AI-agent process/governance behaviour or file types this engine has never modeled. Building
    analysis providers for Terraform/Bruno/markdown-tables is possible in principle but none of those
    file types exist in this repo, and "agent output" isn't a static artifact a repository snapshot can
    contain.
  - **3 rules** (`skill.automapper-removal.zero-references-after`, `skill.tests.coverage-gate-80`, plus
    the build/test-result half of a couple of others) that require actually *running* `dotnet
    build`/`dotnet test`/coverlet, not statically analyzing source — orthogonal to what this engine does
    (it analyzes a repository snapshot, it doesn't execute the target repo's build).

This matches `docs/REFACTORING.md`'s own stated non-goal: the engine should stay a governance layer
over static repository facts, not become a test runner or a replacement for arbitrary tooling.

**Design alignment**: `docs/REFACTORING.md` (read in full for this plan, per `CLAUDE.md`'s instruction
to consult it before proposing architectural changes) already specifies almost exactly this evolution:
a *small, reusable, richly-parameterized* primitive vocabulary instead of one primitive per rule
statement (§2.1), a Roslyn-decoupled analysis-domain split (§8), and a **custom analyzer escape hatch**
for anything that "genuinely requires code" (§2.3, §7) rather than forcing every requirement into
declarative YAML. This plan follows that philosophy — it is a *scoped, concrete slice* of
`REFACTORING.md`'s vision (not the full refactor, which stays a separate, larger initiative), sized
to exactly what's needed to cover the 111 in-scope rules.

**A key design finding changes the shape of what "custom analyzer" work is needed.** The original
categorization found ~49 rules needing "method-body/call-site" analysis and assumed each would need its
own bespoke analyzer class. Investigating further: the overwhelming majority of these are actually a
single repeated shape — *"does/doesn't a call-site matching pattern X exist within scope Y"*
(`.Result`, `new HttpClient()`, `Console.WriteLine`, `Guid.NewGuid()`, `MapGet("...")`, etc.). That
shape is exactly the kind of thing `REFACTORING.md` §2.1 says should be **one generic, richly
parameterized primitive**, not 49 classes. So instead of 49 bespoke analyzers, this plan adds:

- **One new analysis-model fact type** (`CallSiteModel`) capturing invocations/object-creations/member
  accesses with their containing method, project, and literal arguments — populated by extending the
  existing Roslyn extraction pass.
- **One new selector kind** (`call_site`) with rich filters (invoked member, target type, containing
  project/namespace/method) to select facts matching a pattern.
- **Two new generic assertions** (`must_exist` / `must_not_exist`) that assert a selector's result
  set (any selector, not just `call_site`) is non-empty/empty within scope — this is the "forbidden/
  required pattern" primitive `REFACTORING.md` implies but the current engine lacks entirely.

This alone covers the large majority of the "syntax analysis" bucket declaratively. What's left after
that reduction is a genuinely small set — **13 rules** — that need real code (control-flow analysis,
cross-file/cross-artifact value comparison, or wrapping the .NET SDK's own built-in Roslyn/IDE
analyzers rather than reimplementing them, per `REFACTORING.md` §2.3's explicit non-goal "the engine
should not attempt to replace Roslyn analyzers"). Those become the actual custom-analyzer extension
point's first (and, for now, only) consumers.

**User decisions this plan implements:**
1. Full implementation — every one of the 111 in-scope rules gets working enforcement (declarative
   rule file or custom analyzer), not just the framework.
2. New selectors/assertions are built AND all fitting rules are converted into working `rules/*.yml`
   files (not left as capability-only).
3. **Execution order**: build and ship the ~70 purely-declarative rules first (Stage A below — no
   dependency on the new call-site/custom-analyzer machinery). The ~41 rules that depend on the new
   `call_site` selector and/or a bespoke custom analyzer (the original "49 syntax-analysis" bucket,
   reduced per the finding above) are built last, as Stage B, once Stage A is done and shipped.

## Design principles (from docs/REFACTORING.md, applied)

- Prefer one generic primitive with rich parameters over many narrow ones (§2.1).
- Represent negation through explicit `must_not_*` counterparts, consistent with this codebase's
  existing convention (`must_reference_package`/`must_not_reference_package`, etc.) rather than
  introducing a new negation mechanism.
- Keep Roslyn/MSBuild concerns inside their existing analyzer projects; `RulesEngine.RuleModel` and
  `RulesEngine.Analysis` stay plain-POCO, dependency-free (per `CLAUDE.md`'s architecture invariants).
- Reuse the compiler's own analyzers instead of reimplementing style/formatting checks (§2.3, §9).
- Custom analyzers operate over the same pure `RepositoryModel` as declarative rules (extended with the
  new `CallSiteModel`/`FieldModel` facts) — not raw Roslyn `Compilation` objects — so the existing
  "Analysis has zero Roslyn dependency" boundary holds even for custom analyzer code. The one exception
  is the Roslyn-diagnostic-passthrough analyzer (below), which necessarily needs a live `Compilation`
  and is scoped to live in `RulesEngine.Analyzers.Roslyn` for that reason.

---

# Stage A — Declarative primitives (build and ship first, ~70 rules)

No dependency on call-site analysis or the custom-analyzer extension point — pure extensions to the
existing symbol-level model and selector/assertion pattern.

## Phase A1 — Analysis-model extensions (`src/RulesEngine.Analysis`, `src/RulesEngine.Analyzers.Roslyn`)

Extend the existing model records (additive, non-breaking) so the data the new primitives need actually
exists:

- `PropertyModel`: add `IsRequired`, `IsInit`, `IsStatic`, `Attributes: IReadOnlyList<AttributeModel>`.
- `MethodModel` / `ConstructorModel`: add `Attributes: IReadOnlyList<AttributeModel>`.
- `ParameterModel`: add `Attributes`, `HasDefaultValue`.
- `AttributeModel`: add `NamedArguments: IReadOnlyDictionary<string,string>` (currently positional-only).
- New `FieldModel` (`Name`, `Type`, `Accessibility`, modifiers incl. `readonly`/`static`/`const`) +
  `TypeModel.Fields`, populated the same way `Properties`/`Methods` already are from `IFieldSymbol`.
- `MethodModel`/`PropertyModel`/`ConstructorModel`/`FieldModel` get a `DeclaringType`/`ProjectName`
  back-reference (small additive fields, same pattern `TypeModel.ProjectName` already uses) so the new
  member-level selectors below can filter/report scope.

All additions are populated during the existing symbol-level extraction pass; no new
`IAnalysisProvider` is needed, and none of this touches method bodies/syntax (that's Stage B).

## Phase A2 — Wire `when`/`and`/`or`/`not` into the YAML parser

`AndCondition`/`OrCondition`/`NotCondition`/`IConditionNode` already exist and are unit-tested
(`src/RulesEngine.RuleModel/Conditions/`) but nothing parses them from YAML (`CLAUDE.md` gotcha,
confirmed still true). Add a `ConditionParserRegistry` (mirrors `SelectorParserRegistry`) in
`RulesEngine.Configuration/Parsing/`, wire it into `RuleDocumentParser` for a `when:` block, and add
`when`/`and`/`or`/`not` to `rules/schema/rule.schema.json`. Several Stage A rules are conditional
("if a class implements X, then...") and need this.

## Phase A3 — New selector kinds (`src/RulesEngine.Evaluation/Selectors`, registered in `DefaultParsers.cs`)

| kind | candidates | filters |
|---|---|---|
| `method` | `MethodModel` (flattened, carrying declaring type/project) | `namespace`, `project`, `declaring_type`, `accessibility`, `is_async`, `is_static` |
| `property` | `PropertyModel` | same shape |
| `constructor` | `ConstructorModel` | `declaring_type`, `parameter_types` |
| `field` | `FieldModel` | `declaring_type`, `is_readonly`, `is_static` |
| `record` | `TypeModel` filtered to `Kind == Record` | `namespace` (mirrors existing `class` selector) |
| `file` | `FileModel` | `path` (glob), `extension` |
| `repository` | singleton `RepositoryModel` itself | none — anchor for repo-root-level assertions |

(`call_site` is added in Stage B, once `CallSiteModel` exists.)

## Phase A4 — New assertion kinds (`src/RulesEngine.Evaluation/Assertions`)

Grouped by what they unlock:

- **MSBuild property**: `must_have_msbuild_property {name, value}` — reads `ProjectModel.Properties`
  (already populated by Buildalyzer, just never read by an assertion today).
- **Naming**: `must_match_name {regex}` — works against any candidate with a `.Name` (Type/Method/
  Property/Field/Project); extends the existing glob-only matching with true regex for the ~8 naming
  rules that need character classes/anchors GlobMatcher can't express.
- **Modifiers**: `must_have_modifier` / `must_not_have_modifier {modifier}` — one generic pair
  (`sealed|abstract|static|partial|readonly|required|init|record`) replacing what would otherwise be
  8+ bespoke booleans, per `REFACTORING.md` §2.1's exact example.
- **Attributes**: `must_have_attribute` / `must_not_have_attribute {type, argument?}`.
- **Members** (new negatives, symmetric with existing positives): `must_not_have_method`,
  `must_not_have_property`, `must_not_inherit_from`, `must_not_implement`.
- **Structure**: `must_have_parameter_count {max?, min?}` (constructor/method candidates).
- **Filesystem**: `must_have_file` / `must_not_have_file {path}`, `must_have_directory {path}` — all
  read `RepositoryModel.Files`, no new model needed (confirmed the plumbing already exists but nothing
  consumes it).
- **File content**: `must_match_content` / `must_not_match_content {pattern}` — reads the selected
  `FileModel.Path` from disk at evaluation time (no eager content-loading into the model, keeps
  `RepositoryModel` cheap).
- **JSON config**: `must_have_json_field` / `must_not_have_json_field {path, equals?}` — parses the
  selected file as JSON (`System.Text.Json`) and evaluates a dotted field path.
- **Cross-check**: `must_match_filename` — asserts a `TypeModel`'s name matches its own
  `Path.GetFileNameWithoutExtension(FilePath)`.

## Phase A5 — Author `rules/*.yml` for the ~70 Stage A rules

Following the existing `rules/generated/` convention (illustrative, PayPoint-sourced `standard:`
references) started previously: one YAML file per rule, covering buckets (a) MSBuild properties,
(b) file/folder existence, (c) file-content scans, (d) JSON config fields, (f) modifiers, (g)
attributes, (h) naming, plus the declarative subset of cross-file rules (e.g.
`golden.arch.layer-dependency-matrix`, already expressible with the *existing* `must_reference_project`/
`must_not_reference_project` pair — no new primitive needed, just the rule file). Same non-testing
stance as the prior session (this subfolder isn't wired into `dotnet test`).

**Ship/verify Stage A in full (see Verification) before starting Stage B.**

---

# Stage B — Call-site extension point + custom analyzers (build last, ~41 rules)

This is the "small extension point" for the rules that need call-site/method-body analysis — the
original ~49-rule "syntax analysis" bucket. Only start this once Stage A is merged and verified.

## Phase B1 — `CallSiteModel` + Roslyn syntax-body extraction

New `CallSiteModel` (`Kind`: Invocation/ObjectCreation/MemberAccess, `InvokedMember`, `TargetTypeName`,
`ContainingMethod`, `ProjectName`, `ArgumentLiterals`, `FilePath`, `Line`) + `RepositoryModel.CallSites`.
Populated by extending `RoslynTypeExtractor`'s existing per-method walk to also inspect the method's
syntax body via the semantic model already available at that point (it currently only reads symbols,
never bodies) — additive to the existing extraction pass, not a new provider.

## Phase B2 — `call_site` selector + cardinality assertions

- Selector `call_site`: candidates `CallSiteModel`, filters `kind`, `invoked_member`, `target_type`,
  `containing_project`, `containing_method`.
- **Cardinality (the big one)**: `must_exist` / `must_not_exist` — generic, takes no params beyond the
  selector already scoping it; asserts the candidate selector's result set (evaluated within the
  current target's scope) is non-empty/empty. This is what turns most "forbidden call-site" /
  "required call-site" rules into pure declarative rules once `call_site` exists — e.g. `.Result`/
  `.Wait()`, `new HttpClient()`, `Console.WriteLine`, `Guid.NewGuid()`, `AddFrameworkInstrumentation`,
  `new Meter(`/`new ActivitySource(`, service-locator calls, are all one `call_site` selector +
  `must_not_exist`, no new code per rule.
- **Call-site argument**: `must_match_argument {index, pattern}` — for route-string style checks where
  the call-site must exist *and* its literal argument must match a pattern (e.g. `/api/v[0-9]+/`),
  covering `MapGet`/`MapPost`/`MapHealthChecks` route + health-endpoint rules.

## Phase B3 — Custom analyzer extension point

- `ICustomAnalyzer` in `RulesEngine.RuleModel` (same placement rationale as `ITargetSelector`/
  `IAssertion` — Core depends on the interface, not the implementation): `string Name { get; }`,
  `IEnumerable<AnalyzerViolation> Analyze(RepositoryModel model)`.
- `RuleDefinition` gains an optional `AnalyzerName: string?`, mutually exclusive with `Target`/
  `Assertions` (schema: `oneOf [{required:[target,assertions]}, {required:[analyzer]}]`).
- `CustomAnalyzerRegistry` in `RulesEngine.Configuration/Parsing/`, resolved the same way selector/
  assertion parsers are.
- `RuleEvaluator` (`RulesEngine.Core`) branches: if `AnalyzerName` is set, resolve and invoke the
  analyzer directly instead of the selector→assertion pipeline, mapping its violations into the same
  `Violation` shape used everywhere else (unified diagnostics, per `REFACTORING.md` §3.5).

## Phase B4 — The 13 rules that get a real custom analyzer

Everything else in the original "syntax analysis" bucket is covered declaratively via Phase B2
(`call_site` + `must_exist`/`must_not_exist` + Stage A's naming/modifier/attribute primitives). These
13 genuinely need code — control flow, cross-artifact comparison, or wrapping the compiler's own
analyzers:

| analyzer | covers | why it can't be declarative |
|---|---|---|
| `RoslynDiagnosticPassthroughAnalyzer` | `coding.format.using-directives`, `coding.format.braces-required`, `coding.lang.file-scoped-namespaces`, `coding.api.xml-docs-present` | These are exactly what IDE0005/IDE0011/IDE0161/CS1591 already check — runs `CompilationWithAnalyzers` with the .NET SDK's built-in analyzers and republishes selected diagnostic IDs as violations, instead of reimplementing them (`REFACTORING.md` §2.3/§9 explicit non-goal) |
| `SwitchExhaustiveAuditVersionAnalyzer` | `skill.domain.event-mapping-exhaustive` | needs per-case-arm construction analysis |
| `NoBusinessExceptionsAnalyzer` | `skill.domain.no-business-exceptions` | needs to distinguish the one sanctioned `throw` from all others by structural position |
| `ImmutableMutationAnalyzer` | `skill.domain.immutable-mutation` | needs assignment-expression detection, not just call-sites |
| `SingleCatchBlockAnalyzer` | `skill.application.single-catch-block` | needs catch-block counting/typing within a method body |
| `MemberOrderingAnalyzer` | `coding.type.member-ordering` | needs ordered-sequence comparison across member declarations |
| `NoPureDelegationOverrideAnalyzer` | `golden.persistence.repository-base-class` (delegation-detection clause only; the "derives from `DomainEntityRepository<>`" clause is declarative via `must_inherit_from`) | needs method-body-is-single-base-call detection |
| `BlockingCountPatternAnalyzer` | `coding.perf.no-blocking-count` (`.Count() > 0` half only; `.Result`/`.Wait()` half is declarative via `call_site`) | needs the surrounding binary-comparison expression, not just the call site |
| `EndpointRouteConstantAnalyzer` | `golden.eventhandler.route-constants` | needs "argument is NOT a literal" detection (inverse of what `CallSiteModel` captures) |
| `PartitionKeyBuilderCardinalityAnalyzer` | `skill.persistence.partition-key-builder-class` | needs grouping classes by inferred aggregate-name prefix, not a flat count |
| `EventIdReservationBlockAnalyzer` | `skill.reporting.eventid-reservation-blocks` | needs repo-wide duplicate-value detection across extracted attribute arguments |
| `TypenameConsistencyAnalyzer` | `skill.client-config.typename-consistency` | compares a C# constant value against a YAML file field — cross-domain |
| `DbUpBootstrapAnalyzer` | `skill.reporting.dbup-bootstrap` | composite check spanning bootstrap call-site + csproj `<Content>` build-action (not currently modeled, and narrow enough not to warrant a permanent model addition) + filename pattern |

`skill.eventhandler.unit-test-status-coverage` folds into `must_exist` per-status-code once the
`method` selector can filter by name-contains-token — reclassified from custom analyzer to declarative
during design (test method names already carry the status code as a token).

## Phase B5 — Author `rules/*.yml` for the remaining ~41 Stage B rules

Same convention as Phase A5: ~28 using `call_site` + `must_exist`/`must_not_exist`/`must_match_argument`
declaratively, 13 using `analyzer:` referencing the Phase B4 classes. Combined with Stage A's ~70, this
brings the total to all 111 in-scope rules enforced.

## Out of scope — explicitly not implemented (19 rules)

- **16 non-.NET-repository-artifact rules** (`ai-consumption.*`, `deviations.*`,
  `skill.tests.bruno-folder-conventions`, `skill.meta.skill-description-length`, `agent.*`,
  `golden.naming.region-suffix`) — no analysis provider exists or is proposed for agent-transcripts,
  Terraform, Bruno collections, or markdown deviation tables; none of those artifacts exist in this repo.
- **3 execution-based rules** (`skill.automapper-removal.zero-references-after`,
  `skill.tests.coverage-gate-80`) — require running `dotnet build`/`test`/coverlet, not static analysis.

These stay unconverted in `rules.generated.json`, same as before — flagged here so the "full
implementation" scope is honest about what "all" excludes and why.

## Verification

New model/selector/assertion/analyzer classes get unit tests following the existing per-class test
pattern (e.g. `MustInheritFromAssertionTests` → new `MustHaveModifierAssertionTests`,
`MethodSelectorTests`, etc.) in the matching test project (`RulesEngine.Evaluation.Tests`,
`RulesEngine.Configuration.Tests`, `RulesEngine.Core.Tests` for the Stage B analyzer-branch behavior).

**After Stage A:**
- `dotnet build` stays 0 errors; `dotnet test` stays green (new tests added, none of the existing 81
  should change behavior since all additions are additive).
- `dotnet run --project src/RulesEngine.Cli -- list-rules` shows the ~70 new Stage A rules loading
  without error, alongside the existing 16.
- Spot-check a few Stage A rules with `explain-rule`, and run `validate` against a small scratch fixture
  repo (not this repo itself, per the documented self-analysis limitation) exercising a passing and
  failing case for a sample of each new Stage A primitive.

**After Stage B** (only start once Stage A is verified and shipped):
- Same checks, extended to the ~41 Stage B rules — `list-rules` shows all 111 rules total; spot-check
  a `call_site`-based rule and a custom-analyzer-backed rule against the same scratch fixture repo.
