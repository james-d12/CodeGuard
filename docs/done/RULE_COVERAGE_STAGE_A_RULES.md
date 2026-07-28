# Stage A Rule Classification

> Produced during Stage A implementation (Phase A5a of `docs/RULE_COVERAGE_PLAN.md`), after
> Phases A1-A4 (model extensions, `when`/`and`/`or`/`not` parsing, all Stage A selectors and
> assertions) were built and verified (161 tests green). This is the concrete, rule-by-rule
> classification of all 135 `deterministic` entries in `rules.generated.json` against the
> primitive vocabulary that now actually exists in the engine — no artifact like this existed
> before this pass (confirmed by repo search).

## Summary

| Bucket | Count |
|---|---|
| Stage A (declarative, ships now) | 53 |
| Stage B (needs call-site analysis / custom analyzer / cardinality primitives not yet built) | 60 |
| Out of scope (non-.NET-artifact or execution-based) | 18 |
| Already done (existing `rules/generated/*.yml`, prior session) | 4 |
| **Total deterministic rules** | **135** |

**This deviates from `docs/RULE_COVERAGE_PLAN.md`'s rough estimate (~70 stage-a / ~41 stage-b / 19 out-of-scope / 4 done).** The real split is **53 / 60 / 18 / 4**. The main driver: several rules that *sound* purely structural (e.g. "repository classes derive from X and don't just delegate to the base class", "factory methods read from the right source", "exactly one DI registration exists") actually need a generic *cardinality* check — "does at least one thing matching pattern Y exist" — over an arbitrary selector result. The engine's only existing cardinality-style assertions (`must_have_method`/`must_have_property`/`must_have_constructor`) are type-scoped and name-pattern-only; the general-purpose `must_exist`/`must_not_exist` primitive is explicitly a Stage B deliverable (source plan §Phase B2), not built in this pass. So every rule needing that generic form — even when it has nothing to do with call-sites or method bodies — is classified stage-b here. Several such rules are **split** in the notes below: a genuinely stage-a sub-rule (usually an inheritance or attribute check) can ship today as its own YAML file, while the fuller rule waits on Stage B. The out-of-scope count (18, not 19) matches the plan's named 16 non-.NET-artifact rules exactly, plus only 2 confirmed execution-based rules (`skill.automapper-removal.zero-references-after`, `skill.tests.coverage-gate-80`) — the plan's own text hedged on a precise 3rd example ("plus the build/test-result half of a couple of others"), so 18 is the honest count rather than a forced 19.

## Stage A — declarative, ships now (53)

| Rule ID | Selector Kind | Notes |
|---|---|---|
| `coding.eventhandler.event-limiting-folder` | `repository` | must_have_file with a path glob under the EventLimiting/ folder (approximates per-project directory check) |
| `coding.format.editorconfig-present` | `repository/file` | existence via must_have_file; per-setting checks via file selector + must_match_content |
| `coding.format.indentation` | `file` | must_not_match_content for tab characters |
| `coding.format.line-length` | `file` | must_not_match_content with inline-multiline regex (?m)^.{121,}$ |
| `coding.naming.acronyms` | `class/method/property/field` | expressible via must_match_name with a negative-lookahead regex |
| `coding.naming.async-suffix` | `method` | is_async filter + must_match_name covers the `async` keyword case; return-type-based case (non-async Task-returning methods) needs a small return-type filter not yet in MethodSelector |
| `coding.naming.capitalization-table` | `class/method/property/field` | partial: covers declared symbols (types/methods/properties/fields) via must_match_name; locals/parameters not modeled |
| `coding.packages.mandatory` | `project` | must_reference_package per required package |
| `coding.test.naming-convention` | `method` | must_match_name against test method identifiers |
| `coding.type.enum-json-converter` | `type` | needs a small new `enum` selector (identical shape to existing `record` selector, filtering TypeKind.Enum) — trivial declarative gap, not method-body work |
| `coding.type.nullable-enabled` | `project` | must_have_msbuild_property Nullable=enable |
| `golden.api.minimal-apis-only` | `class` | must_not_have_attribute(ApiController) + must_not_inherit_from(ControllerBase) |
| `golden.arch.entity-event-records` | `record/property` | must_match_name for *EntityData/*EventData + must_have_modifier(required) on mandatory properties |
| `golden.arch.event-naming` | `record` | must_match_name against event type names |
| `golden.arch.event-structure` | `record/property` | must_have_property(Audit)/(Version) on base record; must_have_modifier(sealed) on concrete records via inherits_from selector |
| `golden.arch.layer-dependency-matrix` | `project` | expressible today via existing must_reference_project/must_not_reference_project pair, per source plan |
| `golden.cicd.no-dockerfile-in-product-repo` | `repository` | must_not_have_file("Dockerfile") |
| `golden.cicd.pipeline-filenames` | `repository` | must_have_file x2 |
| `golden.config.no-secrets-in-source` | `file` | must_not_match_content with a credential-shaped regex, broadly scoped |
| `golden.csproj.treat-warnings-as-errors` | `project` | must_have_msbuild_property TreatWarningsAsErrors=true |
| `golden.di.registration-naming` | `method` | approximated via method selector (is_static, accessibility public) + must_match_name("^Add[A-Za-z]+$"); extension-method-of-IServiceCollection precision not verified |
| `golden.domain.no-infra-deps` | `file` | must_not_match_content per forbidden using-directive/type reference, scoped to *.Domain project files |
| `golden.http.framework-http-only` | `class` | approximated via must_not_inherit_from(HttpClient) + must_not_have_method("SendRequest"); "reimplementing a wrapper" is approximated structurally, not behaviorally verified |
| `golden.launch.launchsettings-required` | `file` | must_have_json_field per required launchSettings.json field |
| `golden.launch.port-ranges` | `file` | approximated via must_match_content regex on the raw JSON text for the allowed port range (must_have_json_field only supports exact equality, not numeric ranges) |
| `golden.persistence.dbup-script-naming` | `file` | needs a small addition: extend must_match_name (or add a filename-regex assertion) to work against FileModel.RelativePath, since GlobMatcher's `*`-only glob cannot express the numeric-prefix pattern precisely |
| `golden.persistence.no-mixed-stores` | `project` | must_reference_package/must_not_reference_package pairs per project pattern |
| `golden.repo.layout` | `repository` | must_have_file / must_have_directory per required path |
| `golden.repo.requirements-folder` | `repository` | must_have_directory per required subfolder |
| `golden.repo.root-files` | `repository` | must_have_file per required root file |
| `golden.repo.slnx-preferred` | `repository` | when: must_have_file(*.slnx) + assertion must_not_have_file(*.sln) |
| `golden.repo.solution-folder-naming` | `file` | must_not_match_content regex against the .slnx XML text |
| `golden.schemas.audit-model-fields` | `record/property` | must_have_property per required member (exact-set / no-extra-members not verifiable without a member-count assertion) |
| `golden.schemas.no-business-validation-attrs` | `property` | must_not_have_attribute per forbidden validation attribute |
| `golden.schemas.pure-contracts` | `project/record/property` | must_not_reference_package("*") for zero-package check; record/property modifier+attribute checks for the rest |
| `golden.stack.mandatory-framework-libs` | `project` | must_reference_package per required PP.Framework.* package |
| `golden.stack.no-efcore-for-operational-data` | `project` | must_not_reference_package(EFCore*) |
| `golden.test.naming-convention` | `method` | must_match_name against test method identifiers |
| `golden.test.no-forbidden-packages` | `project` | must_not_reference_package per forbidden package |
| `skill.apphost.internal-http-only` | `file` | must_not_match_content for https:// URL literals in AppHost source (approximated as text pattern, not call-site argument extraction) |
| `skill.apphost.pinned-cosmos-emulator-tag` | `file` | must_match_content against the literal image tag string in AppHost source |
| `skill.application.handler-shape` | `class` | must_have_modifier(sealed)+must_have_modifier(partial) + must_have_method("HandleAsync") |
| `skill.application.no-efcore-dbcontext` | `class/project` | must_not_inherit_from(DbContext) + must_not_reference_package(EFCore*) |
| `skill.application.schema-attribute-rules` | `property` | must_have_modifier/must_not_have_modifier(required) + must_have_attribute([Required]) per record kind |
| `skill.domain.command-contract-shape` | `record/property/file` | record/property/naming checks are stage-a; "no throw statements" approximated via must_not_match_content text-scan for `throw` (not precise syntax analysis) |
| `skill.domain.entity-data-shape` | `record/property` | record + required/init property checks |
| `skill.eventhandler.telemetry-folder` | `repository` | must_have_file per required Telemetry/ file |
| `skill.persistence.document-file-split` | `class` | must_match_filename against *EntityDocument/*EventDocument type selectors |
| `skill.reporting.requirements-json-rules` | `file` | must_have_json_field / must_not_have_json_field(equals) per required/forbidden field |
| `skill.reporting.schemas-no-refs` | `project` | must_not_reference_package("*") + must_not_reference_project("*") |
| `skill.service-requirements.base-app-json-scope` | `file` | must_have_json_field / must_not_have_json_field(equals) per required/forbidden field |
| `template.appsettings-no-secrets` | `file` | must_not_have_json_field(ConnectionStrings) + must_not_match_content for secret-shaped values |
| `template.csproj-required-props` | `project` | must_have_msbuild_property per required property |


## Stage B — needs call-site analysis, a named custom analyzer, or a cardinality primitive not yet built (60)

| Rule ID | Notes |
|---|---|
| `coding.api.versioned-routes` | route string is a call-site argument to Map* methods — needs call_site + must_match_argument |
| `coding.api.xml-docs-present` | named custom analyzer target (RoslynDiagnosticPassthroughAnalyzer / CS1591) |
| `coding.async.no-blocking-calls` | call-site detection (.Result/.Wait()/async void) |
| `coding.config.strongly-typed-options` | call-site/fluent-chain detection (AddOptions<T>()....ValidateOnStart()) |
| `coding.di.constructor-injection-only` | ctor param count / readonly field halves are stage-a, but "no calls to IServiceProvider.GetService" needs call-site detection |
| `coding.errors.custom-exception-shape` | naming/inheritance half is stage-a today, but verifying the 4 required ctor overloads by parameter shape needs cardinality (must_exist), a Stage B primitive |
| `coding.format.braces-required` | named custom analyzer target (RoslynDiagnosticPassthroughAnalyzer / IDE0011) |
| `coding.format.using-directives` | named custom analyzer target (RoslynDiagnosticPassthroughAnalyzer / IDE0005) — unused-using detection needs semantic analysis |
| `coding.http.httpclientfactory` | call-site detection (new HttpClient()) |
| `coding.lang.file-scoped-namespaces` | named custom analyzer target (RoslynDiagnosticPassthroughAnalyzer / IDE0161) |
| `coding.perf.no-blocking-count` | call-site detection (.Count()>0 / .Result / .Wait()) |
| `coding.telemetry.no-obsolete-instrumentation` | call-site detection (AddFrameworkInstrumentation) |
| `coding.test.doubles-folder` | folder existence is stage-a, but the "when hand-crafted doubles are used" condition needs code-usage detection to know when the rule applies |
| `coding.type.member-ordering` | named custom analyzer target (MemberOrderingAnalyzer) — needs ordered-sequence comparison across member declarations |
| `golden.api.versioned-path` | route string is a call-site argument; ProblemDetails-on-error also needs response-path analysis |
| `golden.config.hierarchy-order` | call-site ordering of configuration-builder chain calls |
| `golden.eventhandler.route-constants` | named custom analyzer target (EndpointRouteConstantAnalyzer) — needs "argument is not a literal" detection |
| `golden.observability.health-endpoints` | call-site detection (MapHealthChecks(...)) |
| `golden.observability.no-console-writeline` | call-site detection (Console.WriteLine/Write) |
| `golden.persistence.repository-base-class` | split: "derives from DomainEntityRepository<>" clause is stage-a (must_inherit_from) and can ship as its own rule now; the delegation-detection clause needs a custom analyzer (NoPureDelegationOverrideAnalyzer) |
| `skill.apphost.no-hardcoded-secrets` | secret-literal half is a content-scan (stage-a-ish), but "secret params via AddParameter(secret:true)" needs call-site/argument detection |
| `skill.application.di-pattern` | call-site/registration-chain detection + repo-wide "exactly once" cardinality |
| `skill.application.loggermessage-partials` | call-site detection (logger.LogInformation/LogWarning/etc.) |
| `skill.application.no-idgen-in-handler` | call-site detection (GuidGenerator.GenerateDeterministicId) |
| `skill.application.no-manual-validation-filter` | IEndpointFilter-implements half is stage-a, but "calls Validator.TryValidateObject" needs call-site detection |
| `skill.application.no-raw-httpclient` | call-site detection (new HttpClient()) — fluent-chain config (.WithRetryPolicy etc.) also needs call-site |
| `skill.application.single-catch-block` | named custom analyzer target (SingleCatchBlockAnalyzer) |
| `skill.application.startup-bootstrap` | call-site detection (Program.cs specific bootstrap calls) |
| `skill.application.timeprovider-required` | call-site detection (DateTime.UtcNow) |
| `skill.application.validation-net10-addvalidation` | call-site detection (AddValidation() call, custom filter absence) |
| `skill.client-config.typename-consistency` | named custom analyzer target (TypenameConsistencyAnalyzer) — cross-domain C#-constant vs YAML-field comparison |
| `skill.compliance.standards-compliance-checks` | composite meta-rule referencing many sub-checks, several of which are call-site-based |
| `skill.domain.deterministic-entity-id` | call-site detection (Guid.NewGuid() inside Create methods) |
| `skill.domain.event-mapping-exhaustive` | named custom analyzer target (SwitchExhaustiveAuditVersionAnalyzer) |
| `skill.domain.immutable-mutation` | named custom analyzer target (ImmutableMutationAnalyzer) |
| `skill.domain.no-business-exceptions` | named custom analyzer target (NoBusinessExceptionsAnalyzer) |
| `skill.domain.value-object-shape` | sealed/record/init half is stage-a, but "static factory returns tuple rather than throwing" needs method-body/return-statement analysis |
| `skill.eventhandler.bootstrap-required` | call-site detection (Program.cs specific bootstrap calls) |
| `skill.eventhandler.error-contract` | control-flow/response-path analysis per failure category |
| `skill.eventhandler.http-client-resilience` | call-site detection (raw HttpClient usage vs IHttpRequestSender) |
| `skill.eventhandler.no-raw-httpclient-outbound` | call-site detection (new HttpClient()) |
| `skill.eventhandler.unit-test-status-coverage` | source plan reclassifies this as declarative, but only once must_exist (a Stage B primitive) exists — not expressible with Stage A primitives alone |
| `skill.instrumentation.no-raw-otel-sdk` | call-site detection (new Meter(, new ActivitySource(, .AddOpenTelemetry()...) |
| `skill.observability.no-client-key-metric-tag` | call-site argument detection (metric tag name) |
| `skill.persistence.document-attributes` | attribute check ([JsonIgnore]) is stage-a, but default-value (Ttl=-1) and override-validity checks need facts not modeled (property initializers, base virtual-member cross-check) |
| `skill.persistence.factory-attributes` | attribute check is stage-a, but "factory methods read from X not Y" needs method-body data-flow analysis |
| `skill.persistence.partition-key-builder-class` | named custom analyzer target (PartitionKeyBuilderCardinalityAnalyzer) |
| `skill.persistence.registration-singleton-timeouts` | call-site detection (DI registration chain with specific timeout values) |
| `skill.persistence.repository-shape` | sealed/inherits-from half is stage-a, but "registered singleton" and "no VersionedEntityData<T> construction" need call-site detection |
| `skill.persistence.same-partition-key-shape` | cross-class call-site argument comparison (partition-key builder invocation shape) |
| `skill.persistence.sql-dbnull-binding` | call-site detection (SqlParameter.Value assignment vs DBNull.Value) |
| `skill.reporting.datetime-surrogates` | surrogate value format is a runtime-data invariant, not a static code-shape fact; helper-method staticness alone is stage-a but insufficient to cover the rule |
| `skill.reporting.dbup-bootstrap` | named custom analyzer target (DbUpBootstrapAnalyzer) |
| `skill.reporting.event-handler-processor-call` | call-site detection (ProcessAsync call vs custom wrapper construction) |
| `skill.reporting.eventid-reservation-blocks` | named custom analyzer target (EventIdReservationBlockAnalyzer) |
| `skill.reporting.handler-shape` | sealed/partial/HandleAsync half is stage-a, but "delegates to ReportingSearchExecutor.ExecuteAsync" needs call-site/body detection |
| `skill.reporting.pagination-shape` | property-presence half is stage-a, but "TotalRecords populated from persistence-layer value not computed" needs data-flow/provenance analysis |
| `skill.reporting.persistence-processor-base` | split: must_inherit_from(ReportingPersistenceProcessorBase<*>) clause is stage-a and could ship as its own rule now; "not implementing own try/catch/retry" needs body inspection |
| `skill.reporting.sql-repository-conventions` | call-site detection (LogDebug call, IsDBNullAsync vs sync variant) |
| `skill.utils.fpe-cancellation-and-batching` | call-site/parameter-flow analysis (CancellationToken propagation, loop detection) |


## Stage B — final classification after Wave 4 (rule authoring complete)

> Added after Wave 4 of `docs/STAGE_B_PROGRESS.md` authored `rules/generated/*.yml` for this bucket.
> Of the 60 rules above, **50 shipped** as rule files (2 as a split pair, so 51 files), **3 stay
> unenforced** (the `RoslynDiagnosticPassthroughAnalyzer`-targeted IDE0005/IDE0011/IDE0161 rules —
> the compiler's `GetDiagnostics()` never produces IDE-analyzer diagnostics, only compiler ones, so
> a rule referencing those IDs would silently always pass; see "Known gaps" in
> `docs/STAGE_B_PROGRESS.md`), and **7 are genuine gaps** where no combination of existing
> selectors/assertions/analyzers could express the check honestly (each would need a new engine
> fact not modeled — loop detection, named-argument capture, cross-class call-site comparison,
> assignment-RHS-value tracking, or ordered-call-sequence comparison). All analyzer `kind`/param
> names below are the **final, post-genericity-pass** names — see `docs/STAGE_B_PROGRESS.md`'s
> "Post-Wave-3 genericity pass" table if a name here doesn't match an older mental model.

| Rule ID | Rule file | Notes |
|---|---|---|
| `coding.api.versioned-routes` | `coding-api-versioned-routes-001.yml` | `call_site` (target) + `must_match_argument` |
| `coding.api.xml-docs-present` | `coding-api-xml-docs-present-001.yml` | `analyzer: roslyn-diagnostic-passthrough` (CS1591) |
| `coding.async.no-blocking-calls` | `coding-async-no-blocking-calls-001.yml` | `must_not_exist` x2 (`Result`, `Wait`) |
| `coding.config.strongly-typed-options` | `coding-config-strongly-typed-options-001.yml` | `must_exist` (`ValidateOnStart`), presence-only approximation |
| `coding.di.constructor-injection-only` | `coding-di-constructor-injection-only-001.yml` | `must_not_exist` x2 (`GetService`, `GetRequiredService`) |
| `coding.errors.custom-exception-shape` | `coding-errors-custom-exception-shape-001.yml` | `when: must_match_name` + `must_inherit_from` + `must_exist` x3 constructor shapes via `${FullName}` placeholder |
| `coding.format.braces-required` | **not shipped** | uncoverable — see note above |
| `coding.format.using-directives` | **not shipped** | uncoverable — see note above |
| `coding.http.httpclientfactory` | `coding-http-httpclientfactory-001.yml` | `must_not_exist` (`new HttpClient()`) |
| `coding.lang.file-scoped-namespaces` | **not shipped** | uncoverable — see note above |
| `coding.perf.no-blocking-count` | `coding-perf-no-blocking-count-001.yml` | `must_not_exist` x3 (`Count`+`enclosing_comparison: ">"`, `Result`, `Wait`) |
| `coding.telemetry.no-obsolete-instrumentation` | `coding-telemetry-no-obsolete-instrumentation-001.yml` | `must_not_exist` (`AddFrameworkInstrumentation`) |
| `coding.test.doubles-folder` | **gap** | needs per-project directory check + conditional-usage detection; `must_have_directory` only checks one fixed repo-root path |
| `coding.type.member-ordering` | `coding-type-member-ordering-001.yml` | `analyzer: member-ordering` (default order) |
| `golden.api.versioned-path` | `golden-api-versioned-path-001.yml` | `call_site` (target) + `must_match_argument` |
| `golden.config.hierarchy-order` | **gap** | needs ordered-call-sequence comparison (no "call A's Line < call B's Line" assertion exists) |
| `golden.eventhandler.route-constants` | `golden-eventhandler-route-constants-001.yml` | `call_site` (`Map*`, `argument_index:0`, `argument_is_literal:true`) + `must_not_exist` |
| `golden.observability.health-endpoints` | `golden-observability-health-endpoints-001.yml` | `must_exist` (`MapHealthChecks`), presence-only |
| `golden.observability.no-console-writeline` | `golden-observability-no-console-writeline-001.yml` | `must_not_exist` x2 (`Console.WriteLine`, `Console.Write`) |
| `golden.persistence.repository-base-class` | `golden-persistence-repository-base-class-001.yml` + `-002.yml` | split: `-001` = `must_inherit_from`; `-002` = `analyzer: no-pure-delegation-override` |
| `skill.apphost.no-hardcoded-secrets` | `skill-apphost-no-hardcoded-secrets-001.yml` | `must_not_match_content` (file scan, not call-site) |
| `skill.application.di-pattern` | `skill-application-di-pattern-001.yml` | `must_exist` (`ValidateOnStart`), doesn't cover TimeProvider cardinality |
| `skill.application.loggermessage-partials` | `skill-application-loggermessage-partials-001.yml` | `must_not_exist` x3 (`LogInformation`/`LogWarning`/`LogError`) |
| `skill.application.no-idgen-in-handler` | `skill-application-no-idgen-in-handler-001.yml` | `must_not_exist` (`GenerateDeterministicId`) |
| `skill.application.no-manual-validation-filter` | `skill-application-no-manual-validation-filter-001.yml` | `implements` (target) + `must_not_exist` via `containing_type: "${FullName}"` |
| `skill.application.no-raw-httpclient` | `skill-application-no-raw-httpclient-001.yml` | `must_not_exist` (`new HttpClient()`) |
| `skill.application.single-catch-block` | `skill-application-single-catch-block-001.yml` | `analyzer: catch-clause-count` |
| `skill.application.startup-bootstrap` | `skill-application-startup-bootstrap-001.yml` | `must_not_exist` + `must_exist` (WebApplication vs SppWebApplication) |
| `skill.application.timeprovider-required` | `skill-application-timeprovider-required-001.yml` | `must_not_exist` (`DateTime.UtcNow`) |
| `skill.application.validation-net10-addvalidation` | `skill-application-validation-net10-addvalidation-001.yml` | `must_exist` (`AddValidation`) |
| `skill.client-config.typename-consistency` | `skill-client-config-typename-consistency-001.yml` | `analyzer: const-yaml-value-consistency` |
| `skill.compliance.standards-compliance-checks` | **not shipped** | composite meta-rule already covered by the union of other individual rules — not a standalone check |
| `skill.domain.deterministic-entity-id` | `skill-domain-deterministic-entity-id-001.yml` | `must_not_exist` (`Guid.NewGuid()`) |
| `skill.domain.event-mapping-exhaustive` | `skill-domain-event-mapping-exhaustive-001.yml` | `analyzer: exhaustive-switch` |
| `skill.domain.immutable-mutation` | `skill-domain-immutable-mutation-001.yml` | `analyzer: immutable-mutation` |
| `skill.domain.no-business-exceptions` | `skill-domain-no-business-exceptions-001.yml` | `analyzer: no-exceptions` (`allow_guard_clause: true`) |
| `skill.domain.value-object-shape` | `skill-domain-value-object-shape-001.yml` | `must_have_modifier: sealed` only; tuple-factory/no-ID clauses not covered |
| `skill.eventhandler.bootstrap-required` | `skill-eventhandler-bootstrap-required-001.yml` | `must_not_exist` + `must_exist` (WebApplication vs SppWebApplication) |
| `skill.eventhandler.error-contract` | **gap** | needs response-path/control-flow analysis, not modeled |
| `skill.eventhandler.http-client-resilience` | `skill-eventhandler-http-client-resilience-001.yml` | `must_not_exist` (`new HttpClient()`) |
| `skill.eventhandler.no-raw-httpclient-outbound` | `skill-eventhandler-no-raw-httpclient-outbound-001.yml` | `must_not_exist` (`new HttpClient()`), narrower project scope than the resilience rule |
| `skill.eventhandler.unit-test-status-coverage` | `skill-eventhandler-unit-test-status-coverage-001.yml` | `must_exist` x9, one per non-success status code, via `method` selector's `name` filter |
| `skill.instrumentation.no-raw-otel-sdk` | `skill-instrumentation-no-raw-otel-sdk-001.yml` | `must_not_exist` x3 (`Meter`, `ActivitySource`, `WithTracing`) |
| `skill.observability.no-client-key-metric-tag` | `skill-observability-no-client-key-metric-tag-001.yml` | `must_not_match_content` (file scan — named-argument values aren't captured by `CallSiteModel`) |
| `skill.persistence.document-attributes` | `skill-persistence-document-attributes-001.yml` | `when: must_match_name` + `must_have_attribute` (JsonIgnore) |
| `skill.persistence.factory-attributes` | `skill-persistence-factory-attributes-001.yml` | `must_have_attribute` (ExcludeFromCodeCoverage) |
| `skill.persistence.partition-key-builder-class` | `skill-persistence-partition-key-builder-class-001.yml` | `analyzer: companion-type-cardinality` |
| `skill.persistence.registration-singleton-timeouts` | `skill-persistence-registration-singleton-timeouts-001.yml` | `must_exist` x2 (`WithRequestTimeout`, `WithThrottlingRetryOptions`), presence-only |
| `skill.persistence.repository-shape` | `skill-persistence-repository-shape-001.yml` | `must_have_modifier: sealed` + `must_inherit_from` |
| `skill.persistence.same-partition-key-shape` | **gap** | needs cross-class call-site argument comparison, not modeled |
| `skill.persistence.sql-dbnull-binding` | **gap** | needs assignment-RHS-value tracking; `MutationSiteModel` doesn't capture the assigned value |
| `skill.reporting.datetime-surrogates` | `skill-reporting-datetime-surrogates-001.yml` | `must_have_modifier: static` only; value-format/null-return clauses not covered |
| `skill.reporting.dbup-bootstrap` | `skill-reporting-dbup-bootstrap-001.yml` | `analyzer: project-convention` |
| `skill.reporting.event-handler-processor-call` | `skill-reporting-event-handler-processor-call-001.yml` | `must_not_exist` (`*PersistenceProcessorInput` construction) |
| `skill.reporting.eventid-reservation-blocks` | `skill-reporting-eventid-reservation-blocks-001.yml` | `analyzer: duplicate-attribute-argument` |
| `skill.reporting.handler-shape` | `skill-reporting-handler-shape-001.yml` | `must_have_modifier` x2 (sealed, partial) + `must_have_method` (HandleAsync) |
| `skill.reporting.pagination-shape` | `skill-reporting-pagination-shape-001.yml` | `when: must_match_name` + `must_have_property` x3 (Page, PageSize, TotalRecords) |
| `skill.reporting.persistence-processor-base` | `skill-reporting-persistence-processor-base-001.yml` | `must_inherit_from` |
| `skill.reporting.sql-repository-conventions` | `skill-reporting-sql-repository-conventions-001.yml` | `must_exist` (LogDebug) + `must_not_exist` (IsDBNull sync) |
| `skill.utils.fpe-cancellation-and-batching` | **gap** | needs parameter-type-aware assertion (no "has a CancellationToken parameter" check exists) + loop detection |


## Out of scope (18)

| Rule ID | Notes |
|---|---|
| `agent.automapper.never-skip-safety-net` | target.kind=agent-output — named out-of-scope |
| `agent.event-catalogue-reviewer.no-code-modification` | target.kind=agent-output — named out-of-scope |
| `agent.local-secrets.no-secret-printing` | target.kind=agent-output — named out-of-scope |
| `ai-consumption.finding-report-schema` | target.kind=agent-output — named out-of-scope |
| `ai-consumption.freshness-thresholds` | target.kind=agent-output — named out-of-scope |
| `ai-consumption.no-findings-on-approved-deviations` | target.kind=agent-output — named out-of-scope |
| `ai-consumption.no-protected-doc-edits` | target.kind=agent-output — named out-of-scope |
| `ai-consumption.severity-vocabulary` | target.kind=agent-output — named out-of-scope |
| `deviations.record-schema` | target.kind=markdown-table-row — named out-of-scope |
| `deviations.status-vocabulary` | target.kind=markdown-table-row — named out-of-scope |
| `golden.naming.region-suffix` | target.kind=terraform-resource — named out-of-scope (16 non-.NET-artifact rules) |
| `skill.automapper-removal.zero-references-after` | execution-based (requires running dotnet build + grep) — named out-of-scope |
| `skill.fetch.confluence-basic-auth` | target.kind=agent-output — named out-of-scope |
| `skill.fetch.event-catalogue-no-destructive-git` | target.kind=agent-output — named out-of-scope |
| `skill.fetch.no-pat-logging` | target.kind=agent-output — named out-of-scope |
| `skill.meta.skill-description-length` | target.kind=skill-document — named out-of-scope |
| `skill.tests.bruno-folder-conventions` | target.kind=bruno-collection — named out-of-scope |
| `skill.tests.coverage-gate-80` | execution-based (requires coverlet coverage report) — named out-of-scope |


## Already done — existing `rules/generated/*.yml` (4)

| Rule ID | Notes |
|---|---|
| `coding.packages.forbidden` | rules/generated/csharp-packages-forbidden-001.yml |
| `coding.test.no-mocking-frameworks` | rules/generated/csharp-test-no-mocking-frameworks-001.yml |
| `golden.persistence.dbup-only` | rules/generated/architecture-persistence-dbup-only-001.yml |
| `golden.test.coverlet-required` | rules/generated/csharp-test-coverlet-required-001.yml |

