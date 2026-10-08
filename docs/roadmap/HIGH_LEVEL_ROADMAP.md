# CodeGuard — High-Level Platform Design

## 1. Purpose

CodeGuard is a deterministic engineering standards and validation platform.

Its primary purpose is to allow organisations to define engineering rules once, distribute them consistently across repositories, and enforce them locally and within CI/CD pipelines.

Over time, CodeGuard will evolve from a CLI-based rule engine into a broader platform providing:

* Centralised rule distribution
* Versioned and composable rule packages
* CI/CD integrations
* Organisation and team-level governance
* Analysis history and reporting
* A web portal for violations and engineering quality
* Integration with AI coding agents

The core principle is:

> **The CodeGuard engine remains deterministic and independent of the distribution mechanism, CI platform, or web portal.**

---

# 2. Goals

### Primary goals

CodeGuard should provide:

1. **Deterministic validation**

    * Rules produce predictable results.
    * The same source and rules should produce the same result.

2. **Portable rule definitions**

    * Rules should work locally and in CI.
    * Rules should not depend on a particular CI provider.

3. **Versioned rule distribution**

    * Organisations should be able to centrally maintain rules.
    * Projects should be able to consume specific rule versions.

4. **Composable rule sets**

    * Projects should be able to consume multiple rule packages.
    * Local rules should be possible alongside organisation-wide rules.

5. **Reproducible analysis**

    * A historical CodeGuard result should be traceable to the exact rules used.

6. **Simple CI adoption**

    * Adding CodeGuard to GitHub Actions or Azure DevOps should require minimal configuration.

7. **Future centralised governance**

    * The architecture should support a future CodeGuard server and portal without making the CLI dependent on them.

---

# 3. Non-Goals

The initial platform should not attempt to become a direct replacement for SonarQube/SonarCloud.

CodeGuard should focus on:

* Organisation-specific engineering standards
* Architecture rules
* DDD conventions
* Layering constraints
* Framework-specific conventions
* Naming and structural rules
* AI-generated code validation
* Deterministic governance

Generic security scanning, vulnerability management, code duplication and broad static-analysis functionality are not the primary focus.

---

# 4. High-Level Architecture

```text
                         CodeGuard Platform
                                │
        ┌───────────────────────┼───────────────────────┐
        │                       │                       │
        ▼                       ▼                       ▼
   Rule System              CLI / Engine             CI/CD
        │                       │                       │
        │                       │               ┌───────┴────────┐
        │                       │               │                │
        │                       │            GitHub           Azure
        │                       │            Actions          DevOps
        │                       │
        └───────────────┬───────┘
                        │
                 Rule Resolution
                        │
                        ▼
                 Deterministic Run
                        │
                ┌───────┴────────┐
                │                │
                ▼                ▼
               JSON            SARIF
                │                │
                │          GitHub/Azure
                │          integrations
                │
                ▼
          Future CodeGuard
             Server
                │
                ▼
             Portal
```

The **CLI and engine remain the core product**.

Everything else consumes the engine's output.

---

# 5. Core Components

## 5.1 CodeGuard Engine

The engine performs deterministic analysis.

Responsibilities include:

* Loading rules
* Resolving rule dependencies
* Analysing source files
* Executing assertions/analyzers
* Producing violations
* Producing structured analysis results
* Applying severity and enforcement policies

The engine should have no dependency on:

* GitHub
* Azure DevOps
* The web portal
* A hosted CodeGuard service

---

# 6. Rule System

The rule system is a major part of the CodeGuard platform.

Rules should be treated as **versioned engineering standards**, rather than simply a collection of YAML files.

## 6.1 Rule Sources

Initially CodeGuard should support:

```text
Local
  ↓
Git
```

Future sources may include:

```text
Registry
Remote service
```

Git support already exists and should become a formal rule distribution mechanism.

Example:

```yaml
rules:
  - source: local
    path: .codeguard/rules

  - source: git
    repository: https://github.com/company/codeguard-rules
    ref: v2.4.0
```

---

# 7. Rule Packages

A rule package provides a well-defined unit of rule distribution.

Example:

```text
company-codeguard-dotnet/
├── codeguard-package.yaml
├── rules/
│   ├── DOTNET001.yaml
│   ├── DOTNET002.yaml
│   └── DOTNET003.yaml
├── tests/
└── README.md
```

Package metadata should identify:

* Package ID
* Version
* Description
* Owner
* CodeGuard compatibility
* Rules contained within the package
* Dependencies

Example:

```yaml
id: company.codeguard.dotnet
version: 2.4.0

name: .NET Engineering Standards
description: Company standards for .NET development

requires:
  codeguard: ">=1.5.0"
```

The package abstraction should exist independently of the transport mechanism.

A Git repository can therefore represent a package today, while a future CodeGuard Registry could provide the same package tomorrow.

---

# 8. Rule Identity

Rule identity must remain stable across rule versions.

For example:

```text
Package:
company.codeguard.architecture

Rule:
ARCH001

Versions:
1.x
2.x
3.x
```

`ARCH001` remains the identity of the rule.

This is important for:

* Historical reporting
* Violation tracking
* Rule adoption
* Dashboards
* Migration between rule versions
* Comparing analysis results

A rule version changing should not create an entirely new logical rule.

---

# 9. Rule Composition

Projects should be able to combine multiple rule sources.

Conceptually:

```text
Organisation rules
        +
Team rules
        +
Repository rules
        +
Local rules
        │
        ▼
     Rule Set
        │
        ▼
    CodeGuard
```

Example:

```yaml
version: 1

rules:
  - package: company.codeguard.dotnet@3.2.0
  - package: company.codeguard.architecture@2.1.0
  - path: ./rules
```

The exact syntax is subject to implementation design.

---

# 10. Rule Governance and Precedence

As rule composition becomes more sophisticated, CodeGuard must define explicit precedence rules.

Potential hierarchy:

```text
Organisation
     ↓
Team
     ↓
Repository
     ↓
Local
```

Organisation-mandated rules should not be silently disabled by lower-level configuration.

For example, a repository should not be able to override a mandatory organisation rule with:

```yaml
disable:
  - ARCH001
```

unless the organisation explicitly permits that behaviour.

Rule configuration and rule disabling should therefore be separate concepts.

The system should favour **explicit governance over implicit overrides**.

---

# 11. Versioning

Rules and packages must be versioned.

A project should be able to explicitly consume:

```text
company.codeguard.dotnet@3.2.0
```

rather than relying on an unversioned moving reference.

Floating references may eventually be supported, but deterministic CI should favour pinned versions.

For example:

```text
Preferred:

company.codeguard.dotnet@3.2.0

Less deterministic:

company.codeguard.dotnet@main
```

---

# 12. Locking and Reproducibility

A future CodeGuard lock file should capture the exact versions/revisions used during analysis.

Example:

```text
.codeguard/
├── codeguard.yaml
└── codeguard.lock
```

Conceptually:

```yaml
packages:
  - id: company.codeguard.dotnet
    version: 2.4.0
    commit: 8f31c7...

  - id: company.codeguard.architecture
    version: 1.8.0
    commit: a91bd2...
```

This provides a guarantee that:

> **The same repository and locked rules produce a reproducible CodeGuard analysis.**

It also allows future portal results to show exactly which rule versions were responsible for a violation.

---

# 13. Rule Testing

Rules should remain testable without requiring test files to be permanently created on disk.

Tests should live alongside the rule definition/package.

Example:

```yaml
tests:
  - name: Domain referencing persistence fails

    files:
      - path: Domain/Order.cs
        content: |
          using Persistence;

    expect:
      status: fail

  - name: Domain without persistence passes

    files:
      - path: Domain/Order.cs
        content: |
          namespace Domain;

    expect:
      status: pass
```

Rule packages should therefore be independently testable.

Potential commands:

```bash
codeguard rules test
codeguard rules validate
```

This ensures that rule changes themselves can be validated before distribution.

---

# 14. CodeGuard Configuration

A project-level configuration file should provide the entry point for CodeGuard.

Proposed structure:

```text
.codeguard/
├── codeguard.yaml
├── rules/
└── codeguard.lock
```

Example:

```yaml
version: 1

rules:
  - package: company.codeguard.dotnet@3.2.0
  - package: company.codeguard.architecture@2.1.0
  - path: ./rules
```

The CLI should therefore be able to operate with minimal configuration:

```bash
codeguard validate
```

---

# 15. Analysis Result Contract

The analysis result should be treated as a stable API contract.

Conceptually:

```text
Analysis
├── toolVersion
├── rulesVersion
├── repository
├── commit
├── branch
├── timestamp
├── duration
├── status
├── metrics
└── violations[]
```

Each violation should contain information such as:

```text
Violation
├── ruleId
├── ruleVersion
├── severity
├── message
├── file
├── startLine
├── startColumn
├── endLine
├── endColumn
└── metadata
```

This result contract should be independent of how results are displayed.

---

# 16. Output Formats

CodeGuard should support multiple output formats from the same analysis result.

```text
                   Analysis Result
                         │
          ┌──────────────┼──────────────┐
          ▼              ▼              ▼
       Console          JSON           SARIF
          │              │              │
       Developer       APIs         CI platforms
```

Primary formats:

### Console

For developers.

```bash
codeguard validate
```

### JSON

For automation, agents and future CodeGuard services.

```bash
codeguard validate --format json
```

### SARIF

For integration with platforms supporting code scanning/reporting.

```bash
codeguard validate --format sarif
```

Platform-specific integrations should consume these structured results wherever possible rather than reimplementing the engine.

---

# 17. CI/CD Integrations

CI integrations should be thin wrappers around the CodeGuard CLI.

## GitHub Actions

Target experience:

```yaml
- uses: codeguard/action@v1
```

The Action should handle:

* Installing/downloading CodeGuard
* Version selection
* Binary caching
* Running validation
* Producing annotations
* Uploading SARIF
* Returning the correct exit status

---

## Azure DevOps

Target experience:

```yaml
- task: CodeGuard@1
```

The task should handle:

* Installing/downloading CodeGuard
* Version selection
* Binary caching
* Running validation
* Publishing pipeline annotations
* Returning the appropriate pipeline status

---

# 18. Generic CI Support

The CLI remains the universal integration mechanism.

Any CI system should be able to run:

```bash
codeguard validate
```

and consume:

```text
exit code
JSON
SARIF
console output
```

This means CodeGuard does not need a bespoke integration for every CI platform.

Potential future integrations include:

* GitLab CI
* Jenkins
* Bitbucket Pipelines
* TeamCity
* Other generic CI systems

---

# 19. Baselines

Existing repositories may already contain significant numbers of violations.

CodeGuard should support a baseline/adoption model.

For example:

```text
Existing violations: 1,847
New violations:          0

PASS
```

If a pull request introduces new violations:

```text
Existing violations: 1,847
New violations:          3

FAIL
```

This allows organisations to adopt CodeGuard without requiring immediate remediation of all existing technical debt.

Baseline behaviour should eventually support:

* Existing violation suppression
* New violation detection
* Resolved violation tracking
* Baseline regeneration
* Baseline versioning

---

# 20. Pull Request / Diff Awareness

A later CI capability should distinguish between:

* Existing violations
* New violations
* Resolved violations

Example:

```text
main:
1,847 violations

PR:
1,849 violations

New:
2

FAIL
```

Or:

```text
main:
1,847 violations

PR:
1,843 violations

Resolved:
4

PASS
```

This should become an important part of the CodeGuard CI experience.

---

# 21. Rule Distribution Lifecycle

The intended lifecycle is:

```text
Engineering standard
        │
        ▼
Markdown / documentation
        │
        ▼
Rule generation
        │
        ▼
Rule package
        │
        ▼
Rule tests
        │
        ▼
Human review
        │
        ▼
Versioned release
        │
        ▼
Git distribution
        │
        ▼
Repositories consume package
        │
        ▼
CodeGuard validates
```

AI can assist with rule generation, but rule execution remains deterministic.

---

# 22. Future Rule Registry

A central CodeGuard Registry should be considered a future capability rather than an initial requirement.

Initially:

```text
Git repository
      ↓
Rule package
      ↓
CodeGuard
```

Eventually:

```text
CodeGuard Registry
      │
      ├── Packages
      ├── Versions
      ├── Organisations
      ├── Access control
      └── Package metadata
```

The package abstraction should ensure that introducing a registry does not require changing how CodeGuard understands rules.

---

# 23. Future CodeGuard Server

Once rule distribution and CI integrations are mature, CodeGuard can introduce a server.

```text
CodeGuard CLI
      │
      │ analysis result
      ▼
CodeGuard Server
      │
      ├── Projects
      ├── Repositories
      ├── Branches
      ├── Analyses
      ├── Violations
      ├── Rules
      └── Trends
```

The server should consume the existing analysis result contract rather than becoming part of the analysis engine.

---

# 24. Future Portal

The portal can then provide:

### Organisation

* Repository overview
* Overall violation counts
* Quality trends
* Rule adoption
* Rule versions
* Teams

### Project

* Current quality status
* Violations
* Rule breakdown
* Analysis history
* Branches
* Trends

### Rule

* Rule definition
* Version history
* Tests
* Adoption
* Affected repositories
* Violation history

### Violation

* Repository
* File
* Line
* Rule
* Severity
* Commit
* History

This should be built **after** the underlying distribution and analysis model is stable.

---

# 25. Future AI Integration

CodeGuard's deterministic engine is particularly suited to AI coding agents.

The intended feedback loop is:

```text
AI Agent
    │
    ▼
Generate code
    │
    ▼
CodeGuard
    │
    ├── violations
    │
    ▼
AI Agent
    │
    ▼
Fix code
    │
    ▼
CodeGuard
    │
    └── PASS
```

This provides a deterministic feedback mechanism for AI-generated code.

CodeGuard does not need to generate the fix itself; it can provide structured information that an AI agent can consume.

---

# 26. Proposed Roadmap

## Phase 1 — Core Rule Distribution

Focus on making rules a robust, versioned system.

* Formalise Git rule sources
* Introduce rule packages
* Package metadata
* Stable rule identity
* Package versioning
* Rule composition
* Rule precedence
* Rule testing
* Project `codeguard.yaml`
* Begin design of lock file

**Outcome:**

> CodeGuard can reliably distribute and consume organisation-wide rule sets.

---

## Phase 2 — CI/CD Distribution

Make CodeGuard extremely easy to adopt.

* Standalone binaries
* GitHub Action
* Azure DevOps task
* JSON output
* SARIF output
* CI exit codes
* CI annotations
* Binary caching
* Version selection
* Documentation/examples

**Outcome:**

> A team can add CodeGuard to an existing pipeline with minimal effort.

---

## Phase 3 — CI Governance

Move from "run the tool" to meaningful enforcement.

* Baselines
* New-vs-existing violations
* Pull request/diff awareness
* Quality gates
* Rule severity thresholds
* Resolved violation detection
* CI configuration policies

**Outcome:**

> Organisations can introduce CodeGuard without blocking existing development while preventing new violations.

---

## Phase 4 — Centralised Rule Governance

Build stronger organisation-level management.

* Organisation rule sets
* Team rule sets
* Mandatory rules
* Rule inheritance
* Rule ownership
* Rule lifecycle
* Rule deprecation
* Rule migration
* Central rule documentation

**Outcome:**

> An organisation can centrally govern engineering standards across hundreds of repositories.

---

## Phase 5 — CodeGuard Server

Introduce central analysis/result storage.

* Analysis API
* Projects
* Repositories
* Branches
* Analysis history
* Violation storage
* Rule metadata
* Authentication
* Authorisation

**Outcome:**

> CodeGuard becomes a central engineering governance service.

---

## Phase 6 — CodeGuard Portal

Build the dashboard on top of the server.

* Organisation dashboard
* Project dashboard
* Violation explorer
* Rule explorer
* Trends
* Quality gates
* Repository health
* Rule adoption
* Historical analysis

**Outcome:**

> Teams can understand and manage engineering-standard compliance centrally.

---

## Phase 7 — CodeGuard Registry

If demand justifies it:

* Hosted rule packages
* Private packages
* Package discovery
* Access control
* Version management
* Package publishing
* Package dependencies

**Outcome:**

> CodeGuard becomes a distribution platform for engineering standards.

---

## Phase 8 — AI Governance

Integrate CodeGuard deeply with AI development workflows.

* Agent-friendly JSON output
* AI rule authoring
* Documentation → rule generation
* Agent validation loops
* IDE integrations
* Automated rule remediation workflows

**Outcome:**

> CodeGuard becomes a deterministic governance layer around AI-generated software.

---

# 27. Architectural Principles

The following principles should guide implementation.

### 1. Engine first

The CodeGuard engine must remain usable without a server or portal.

### 2. Git-first distribution

Git is the initial distribution mechanism. A registry is a future optimisation, not a prerequisite.

### 3. Rules are packages

Rules should eventually be treated as versioned packages rather than arbitrary YAML files.

### 4. Deterministic execution

The same source, rule versions and configuration should produce reproducible results.

### 5. Structured results

Analysis results must be independent of their presentation or destination.

### 6. Thin integrations

GitHub Actions and Azure DevOps should wrap the CLI rather than reproduce CodeGuard functionality.

### 7. Explicit governance

Organisation-level rules must be distinguishable from repository-specific rules.

### 8. Version everything important

Rules, packages, analyses and eventually configurations should be traceable to versions or immutable revisions.

### 9. Local-first

Developers should be able to run the exact same CodeGuard validation locally that runs in CI.

### 10. AI assists; CodeGuard enforces

AI may generate rules or consume violations, but enforcement remains deterministic.

---

# 28. Target End State

The long-term CodeGuard architecture should look approximately like:

```text
                         ┌───────────────────────┐
                         │ Engineering Standards │
                         │ Documentation / AI     │
                         └───────────┬───────────┘
                                     │
                                     ▼
                           ┌───────────────────┐
                           │   Rule Packages   │
                           └─────────┬─────────┘
                                     │
                         ┌───────────┴───────────┐
                         │                       │
                         ▼                       ▼
                    Git Repository        CodeGuard Registry
                         │                       │
                         └───────────┬───────────┘
                                     │
                                     ▼
                              CodeGuard CLI
                                     │
                              Rule Resolution
                                     │
                                     ▼
                            Deterministic Engine
                                     │
                         ┌───────────┼───────────┐
                         ▼           ▼           ▼
                       Local       GitHub       Azure
                                     │
                                     ▼
                              Analysis Results
                                     │
                                     ▼
                              CodeGuard Server
                                     │
                         ┌───────────┼───────────┐
                         ▼           ▼           ▼
                     Projects     Rules      Violations
                                     │
                                     ▼
                              CodeGuard Portal
                                     │
                                     ▼
                              Engineering
                                Governance
```

The important sequencing is:

> **Rule system → distribution → CI integrations → governance → server → dashboard.**

The dashboard is therefore the **last major layer**, not the foundation. The real foundation is making CodeGuard capable of reliably defining, packaging, versioning, distributing and enforcing engineering standards at scale.
