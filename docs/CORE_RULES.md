# Deterministic Software Rule Engine

## Core Scope, Responsibilities and Boundaries

## 1. Purpose

The Rule Engine is a deterministic policy and verification platform for software systems.

Its purpose is to allow an organisation to express its own engineering, architectural, structural, and governance policies as executable rules, and evaluate those rules consistently across repositories and systems.

The Rule Engine is **not intended to replace SonarQube, Roslyn analyzers, linters, security scanners, testing frameworks, or human engineering judgement**.

Its primary purpose is to answer:

> **"Does this software system conform to the engineering and architectural policies that our organisation has explicitly defined?"**

The engine should make organisational standards executable, repeatable, explainable, and enforceable.

---

# 2. Core Design Principle

The Rule Engine should maximise the amount of verification that can be performed **deterministically**.

It should never use AI to answer a question that can be answered reliably and cheaply through deterministic analysis.

Where a question is inherently ambiguous or requires judgement, the Rule Engine should not attempt to manufacture certainty. Instead, it should expose evidence that can be consumed by an AI reviewer or human engineer.

The overall model is:

```text
Software System
      │
      ▼
Analysis Providers
      │
      ▼
Normalised Analysis Model
      │
      ▼
Deterministic Rule Evaluation
      │
      ├── PASS
      ├── WARN
      ├── FAIL
      └── EVIDENCE / REVIEW REQUIRED
```

The Rule Engine should therefore be viewed as a **deterministic policy layer over a software analysis model**, rather than as another general-purpose static analysis tool.

---

# 3. Core Pillars

The Rule Engine should focus on six core pillars.

## Pillar 1 — Architectural Policy

The Rule Engine should enforce organisation-specific architectural boundaries and invariants.

Examples:

* Domain projects must not reference Infrastructure projects.
* API projects must not directly depend on Persistence.
* Application code may depend on Domain.
* Every Command must have a corresponding Handler.
* Every Domain Event must have at least one Handler.
* Specific architectural layers may only depend on approved layers.
* Certain components must not bypass defined application boundaries.

Example:

```yaml
id: architecture.domain-no-infrastructure
name: Domain must not depend on Infrastructure

target:
  type: project
  where:
    layer: Domain

assertions:
  - relationship:
      name: references
      target:
        layer: Infrastructure
      must_not_exist: true
```

The important distinction is that the Rule Engine should express the **architectural policy**, not the implementation mechanism used to discover it.

The engine should not need to know whether the dependency was discovered through a `.csproj`, Roslyn, or another language's project system.

---

## Pillar 2 — Organisational Standards and Conventions

The Rule Engine should enforce deterministic conventions that are specific to the organisation, team, or repository.

Examples:

* Required files must exist.
* Required directories must exist.
* Files must be located in approved locations.
* Required project structures must exist.
* Naming conventions must be followed.
* Required metadata must be present.
* Required components must exist.
* Components must follow defined relationships.

Examples:

```yaml
id: governance.service-owner-required
name: Every service must have an owner

target:
  type: service

assertions:
  - property:
      path: owner
      must_exist: true
```

Or:

```yaml
id: governance.public-api-openapi
name: Every public API must have an OpenAPI specification

target:
  type: api

assertions:
  - relationship:
      name: documented_by
      target:
        type: openapi_document
      must_exist: true
```

These are organisation-specific policies that are unlikely to be fully covered by generic static analysis tools.

---

## Pillar 3 — System and Repository Governance

The Rule Engine should verify that software repositories and systems meet defined organisational requirements.

Examples:

* Every production service has an owner.
* Every production workload has a deployment definition.
* Every service has required documentation.
* Every production service has a health endpoint.
* Required CI/CD configuration exists.
* Required infrastructure definitions exist.
* Required security configuration exists.
* Required service metadata is present.
* Production systems are associated with an owning team.

Example:

```yaml
id: governance.production-service
name: Production services must be owned and deployable

target:
  type: service
  where:
    environment: production

assertions:
  - property:
      path: owner
      must_exist: true

  - relationship:
      name: deployed_by
      target:
        type: deployment_definition
      must_exist: true
```

This moves the engine beyond traditional source-code analysis and into **software system governance**.

---

## Pillar 4 — Cross-System and Cross-Technology Policy

The Rule Engine should be able to reason across technology boundaries.

This is potentially one of its strongest differentiators from traditional static analysis tools.

A repository or system may contain:

```text
C# / .NET
    │
    ├── Docker
    ├── Kubernetes
    ├── Terraform
    ├── CI/CD
    ├── Configuration
    └── Documentation
```

The Rule Engine should be able to evaluate policies across those boundaries.

Examples:

* Every production application must have a deployment definition.
* Every production service must have an owner.
* Every database schema change must have a migration.
* Every public API must have an OpenAPI specification.
* Every deployed service must have required health checks.
* Every service must have required CI/CD configuration.

This is not necessarily traditional static analysis.

It is **policy evaluation over a model of the complete software system**.

---

## Pillar 5 — Change and Impact Policy

The Rule Engine should be capable of consuming deterministic information about changes.

However, its primary responsibility should be to evaluate **known policies about changes**, rather than to become a general-purpose code metrics platform.

Useful deterministic change facts may include:

* Files changed.
* Projects changed.
* Architecture boundaries crossed.
* New dependencies introduced.
* Public APIs changed.
* Database schemas changed.
* Infrastructure changed.
* Security-sensitive areas changed.
* Number of architectural components added or removed.

These facts can support policies such as:

```text
If a change modifies production infrastructure,
require additional approval.
```

Or:

```text
If a public API changes,
require an OpenAPI specification update.
```

Or:

```text
If a database schema changes,
require a corresponding migration.
```

The engine should not automatically conclude:

> "This change is over-engineered."

That is a judgement.

Instead, it can deterministically identify:

> "This change introduced 8 new architectural components and 5 database tables."

That evidence can then trigger targeted AI or human review if appropriate.

---

## Pillar 6 — Evidence, Explainability and Governance

Every rule evaluation should produce clear, actionable evidence.

A rule result should explain:

* Which rule was evaluated.
* What was being evaluated.
* What was expected.
* What was found.
* Why the rule passed or failed.
* The exact evidence supporting the result.
* The location of the violation where possible.
* The severity.
* Whether an exception or waiver applies.

Example:

```text
Rule:
  architecture.domain-no-infrastructure

Result:
  FAILED

Target:
  MyCompany.Domain

Expected:
  Domain projects must not reference Infrastructure.

Actual:
  MyCompany.Domain
    → MyCompany.Infrastructure

Evidence:
  ProjectReference found in:
  src/Domain/MyCompany.Domain.csproj

Severity:
  BLOCKER
```

The Rule Engine should make failures understandable to:

* developers
* CI/CD systems
* AI agents
* governance tooling
* engineering leaders

A failure without useful evidence is significantly less valuable than a failure that clearly explains how to fix it.

---

# 4. The Normalised Analysis Model

The most important architectural component may not actually be the Rule Engine itself.

It is the **Normalised Analysis Model** that sits underneath it.

The Rule Engine should not directly understand:

* Roslyn
* `.csproj`
* EF Core
* Kubernetes
* Terraform
* Azure DevOps
* Git
* SonarQube

Instead, specialised providers should convert source information into a common model.

For example:

```text
Roslyn
  → Entity
  → Command
  → Handler
  → Domain Event

EF Core
  → Database Schema Change
  → Migration

Kubernetes / ArgoCD
  → Workload
  → Deployment

Service Catalogue
  → Service
  → Owner
```

The Rule Engine then operates against those concepts.

Conceptually:

```text
              Analysis Providers
                     │
       ┌─────────────┼─────────────┐
       ▼             ▼             ▼
    Roslyn         Git          Kubernetes
       │             │             │
       └─────────────┼─────────────┘
                     ▼
            Normalised Model
                     │
                     ▼
                Rule Engine
                     │
                     ▼
                Rule Result
```

This allows the Rule Engine to remain technology-independent while allowing organisations to define their own concepts.

---

# 5. Organisation-Specific Concepts

The Rule Engine should not hardcode concepts such as:

* Entity
* Aggregate
* Domain Event
* Command
* Handler
* Repository
* Service
* Adapter

Different organisations use different architectures.

Instead, concepts should be defined or classified through configuration and provider capabilities.

For example:

```yaml
concept:
  id: domain_entity

  matches:
    - inherits: Entity
    - namespace: "**.Domain.Entities"
    - attribute: EntityAttribute
```

Another organisation could define the concept differently.

The underlying engine remains the same.

This creates a distinction between:

```text
Generic Analysis
    ↓
"What exists?"

Organisation Model
    ↓
"What do these things mean?"

Rules
    ↓
"What is allowed or required?"
```

This separation is essential for keeping the Rule Engine reusable.

---

# 6. What the Rule Engine SHOULD NOT Try to Validate

The Rule Engine should have explicit boundaries.

It should deliberately avoid duplicating capabilities already provided effectively by mature tools.

## 6.1 General Code Quality Metrics

The Rule Engine should not attempt to replace SonarQube or equivalent tools for:

* Cyclomatic complexity.
* Cognitive complexity.
* Code duplication.
* Standard maintainability metrics.
* Standard code smells.
* Method length.
* Class length.
* Parameter count.
* Generic code-quality thresholds.

These can be consumed as external evidence if necessary.

The Rule Engine should focus on how those facts relate to **organisational policy**, rather than recreating the analysis itself.

For example:

```text
Sonar:
  Cognitive complexity = 24

Rule Engine:
  New code exceeding organisation threshold
  requires additional review
```

The Rule Engine consumes the evidence; it does not need to calculate cognitive complexity itself.

---

## 6.2 Standard Language and Framework Linting

The Rule Engine should not replace:

* Roslyn analyzers.
* .NET analyzers.
* ESLint.
* StyleCop.
* Pylint.
* Ruff.
* Checkstyle.
* Other language-specific linters.

These tools already excel at identifying language-specific issues.

The Rule Engine should instead consume their results when those results are relevant to organisational policy.

---

## 6.3 Security Scanning

The Rule Engine should not attempt to become a full security scanner.

It should not duplicate:

* SAST.
* Dependency vulnerability scanning.
* Secret scanning.
* Container scanning.
* Infrastructure security scanning.
* DAST.

Instead, it should consume the results of specialist security tooling.

The Rule Engine may enforce policies around those results, for example:

```text
No critical vulnerabilities may be present in production.
```

But the vulnerability discovery itself belongs to specialist tools.

---

## 6.4 Automated Testing

The Rule Engine should not attempt to prove that software is functionally correct.

It cannot reliably determine:

> "This feature works correctly."

Tests, integration tests, contract tests, and end-to-end tests are responsible for this.

The Rule Engine can enforce policies such as:

```text
Every command must have tests.
```

or:

```text
Every public API change must have corresponding contract tests.
```

But the Rule Engine should not replace the tests themselves.

---

## 6.5 Business Necessity

The Rule Engine should not attempt to determine:

> "Does the business actually need this feature?"

That is a product and business decision.

It may consume metadata about requirements or change types, but it cannot reliably determine customer value.

This should remain outside the deterministic Rule Engine.

---

## 6.6 Subjective Architectural Quality

The Rule Engine should not attempt to prove:

> "This is a good architecture."

or:

> "This is the simplest possible implementation."

or:

> "This abstraction is appropriate."

These require judgement.

The Rule Engine can identify deterministic evidence:

```text
+12 classes
+4 interfaces
+3 database tables
+5 new dependencies
```

But it should not claim:

> "This is over-engineered."

That is a candidate for targeted AI or human review.

---

## 6.7 General AI Code Review

The Rule Engine should not become an AI code reviewer.

It should not attempt to evaluate:

* elegance
* maintainability in the abstract
* design quality
* whether an abstraction feels appropriate
* whether a solution is "clean"
* whether the implementation is unnecessarily complex in context

AI should only be introduced when deterministic evidence identifies an ambiguity that genuinely requires judgement.

The preferred pattern is:

```text
Deterministic Analysis
        ↓
Evidence
        ↓
Specific Ambiguity Identified
        ↓
Targeted AI Review
        ↓
Human Review if Necessary
```

Not:

```text
Every PR
    ↓
Send entire repository to AI
    ↓
"Please review everything"
```

---

# 7. Relationship with SonarQube and Static Analysis

The Rule Engine should be complementary to existing tooling.

A useful architecture is:

```text
                    Repository
                        │
        ┌───────────────┼────────────────┐
        ▼               ▼                ▼
      Roslyn          Sonar             Git
        │               │                │
        ▼               ▼                ▼
  Code Analysis     Quality          Change Data
                    Metrics
        │               │                │
        └───────────────┼────────────────┘
                        ▼
               Normalised Evidence
                        │
                        ▼
                  Rule Engine
                        │
                        ▼
                 Policy Decision
```

The Rule Engine should answer:

> "Given the evidence available, does this system comply with our policies?"

It should not answer every underlying analytical question itself.

This makes existing tools an asset rather than a competitor.

---

# 8. Rule Enforcement Levels

Not every rule should have the same enforcement level.

Rules should support differentiated outcomes such as:

```text
INFO
WARNING
ERROR
BLOCKER
```

For example:

```text
Secret detected
→ BLOCKER
```

```text
Domain references Infrastructure
→ ERROR / BLOCKER
```

```text
Class exceeds recommended size
→ WARNING
```

The engine should distinguish between:

* hard constraints
* policy violations
* advisory recommendations

This prevents the system from becoming overly restrictive and encourages organisations to reserve hard blocking for genuinely important rules.

---

# 9. Exceptions and Waivers

Exceptions should be first-class objects.

A rule engine that allows arbitrary suppression will eventually become ineffective.

Exceptions should include:

* Rule ID.
* Reason.
* Owner.
* Expiry date.
* Optional remediation plan.

Example:

```yaml
exception:
  rule: architecture.domain-no-infrastructure
  reason: Legacy migration
  owner: platform-team
  expires: 2026-09-30
```

The system should distinguish:

```text
PASS
FAIL
WAIVED
EXPIRED WAIVER
```

This allows governance reporting such as:

```text
Active violations: 3
Active waivers: 12
Expired waivers: 4
```

The objective is not to eliminate all exceptions.

It is to make exceptions **visible, accountable and temporary where appropriate**.

---

# 10. The Rule Engine's Ideal Position

The Rule Engine should ultimately be positioned as:

> **A deterministic policy engine that evaluates an organisation's software architecture, engineering standards, and system governance against a normalised model of its software systems.**

It is not:

* another SonarQube
* another linter
* another security scanner
* a test framework
* an AI code reviewer
* a generic file validator
* a replacement for human architectural judgement

Its value lies in formalising the rules that are currently often:

* written in architecture documents
* described in onboarding guides
* communicated verbally
* hidden in senior engineers' knowledge
* inconsistently applied
* difficult to enforce automatically

The Rule Engine turns those policies into **executable organisational knowledge**.

---

# 11. Core Product Test

A useful test for whether something belongs in the Rule Engine is:

> **Can this be expressed as a deterministic statement about a known software fact, structure, relationship, or organisational policy?**

If yes, it is a strong candidate.

Examples:

```text
Every command must have a handler.
```

```text
Domain must not depend on Infrastructure.
```

```text
Every production service must have an owner.
```

```text
Every database schema change must have a migration.
```

```text
Every public API must be authenticated unless explicitly exempt.
```

```text
Every production workload must have a deployment definition.
```

If the question instead asks:

```text
Is this the best design?
```

```text
Is this over-engineered?
```

```text
Is this the simplest implementation?
```

```text
Does the customer really need this?
```

```text
Is this code elegant?
```

then it is outside the deterministic core.

Those questions may be surfaced as **review opportunities**, but should not be presented as deterministic facts.

---

# 12. Final Scope

The Rule Engine should focus on:

```text
┌──────────────────────────────────────────────┐
│                 RULE ENGINE                  │
│                                              │
│  Architecture                                │
│  Organisational Standards                    │
│  System Governance                           │
│  Cross-Technology Policy                     │
│  Required Structure                          │
│  Required Relationships                      │
│  Deterministic Change Policies               │
│  Evidence and Explainability                 │
│  Exceptions and Governance                   │
└──────────────────────────────────────────────┘
```

It should integrate with:

```text
SonarQube
Roslyn
Linters
Security Scanners
Dependency Scanners
Test Frameworks
Git
MSBuild
Infrastructure Analysers
Deployment Platforms
Service Catalogues
```

It should deliberately leave to other systems or human judgement:

```text
Code Quality Analysis
Security Discovery
Functional Correctness
Business Necessity
Subjective Design Quality
Architectural Elegance
Over-engineering Judgement
General AI Code Review
```

The strategic objective is therefore not:

> **"Build a better static analysis tool."**

It is:

> **"Build a deterministic, technology-independent mechanism for turning an organisation's engineering and architectural policies into executable, explainable, and consistently enforced rules."**

That boundary should remain central to the product. It keeps the Rule Engine focused, avoids duplicating mature tooling, and creates a clear role for it within an AI-assisted engineering environment.
