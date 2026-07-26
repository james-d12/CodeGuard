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

