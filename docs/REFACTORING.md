# CodeGuard Refactoring & Evolution Design Document

## 1. Purpose

This document defines the refactoring and architectural evolution required for the existing deterministic CodeGuard.

The initial implementation established a generic rule engine with:

* Rule definitions.
* Targets.
* Assertions.
* Roslyn-based C# analysis.
* MSBuild/project analysis.
* Repository/file analysis.
* Declarative rule configuration.
* Rule evaluation.
* Diagnostic output.
* A modular project structure.

The initial design has proven the basic concept, but several architectural risks have been identified.

The primary risks are:

1. The primitive vocabulary becoming excessively large.
2. The rule DSL becoming an unintended programming language.
3. Primitive concepts becoming tightly coupled to Roslyn.
4. The engine attempting to replace Roslyn analyzers.
5. The distinction between selectors, predicates, assertions, and rules becoming unclear.
6. Composite organisational concepts becoming hard-coded into the engine.
7. Analysis being repeatedly performed for individual rules, causing performance issues.
8. Rules lacking strong versioning and lifecycle management.
9. Rule behaviour not being tested as first-class artefacts.
10. Diagnostics not containing sufficient structured information for humans and AI agents.
11. Dependency analysis having ambiguous semantics.
12. The analysis model becoming an overly generic abstraction that leaks across unrelated analysis domains.

This refactoring should address these risks while preserving the existing system wherever practical.

The desired end state is:

> A deterministic, modular governance engine that allows an organisation to define, version, test, distribute, and execute engineering and architectural rules as data, while retaining the ability to use specialised analyzers when declarative primitives are insufficient.

---

# 2. Design Goals

The refactoring should achieve the following.

## 2.1 Small and stable primitive vocabulary

The primitive layer should contain a relatively small number of reusable capabilities.

The system should avoid creating a new primitive for every combination of conditions.

For example, the engine should not contain:

```text
MustHavePublicMethod
MustHavePublicStaticMethod
MustHavePublicStaticAsyncMethod
MustHavePublicStaticAsyncMethodReturningTask
```

Instead, it should have:

```text
MustHaveMethod
```

with configurable constraints:

```yaml
method:
  name: Create
  accessibility: public
  static: true
  async: true
  return_type: Task<Payment>
```

The primitive describes the capability.

The configuration describes the specific requirement.

---

## 2.2 Declarative composition

Organisation-specific concepts should be defined by composing primitives.

For example:

```text
DomainEntity
```

should not necessarily be implemented as a special C# class inside the engine.

Instead:

```text
DomainEntity
    =
    IsClass
    AND MustInheritFrom Entity<TId>
    AND MustBeInProject *.Domain
    AND MustNotDependOn *.Infrastructure
```

This allows the organisation's vocabulary to grow without continuously modifying the engine.

---

## 2.3 Custom analyzer escape hatch

Not every requirement should be forced into the declarative rule system.

The architecture must support specialised analyzers for rules that require:

* Complex semantic analysis.
* Advanced Roslyn APIs.
* Performance-sensitive analysis.
* Code-flow analysis.
* Data-flow analysis.
* IDE integration.
* Code fixes.
* Analysis that is difficult or inappropriate to express declaratively.

The system should therefore support two implementation mechanisms:

```text
Rule
├── Declarative
│   └── Primitive composition
│
└── Custom
    └── Specialised analyzer
```

Both should produce a common diagnostic format.

---

# 3. Revised Conceptual Model

The existing model should be refined into five distinct concepts.

```text
Rule
  │
  ▼
Selector
  │
  ▼
Candidate Entities
  │
  ▼
Predicate Evaluation
  │
  ▼
Assertion
  │
  ▼
Diagnostic
```

The concepts should have clear responsibilities.

---

## 3.1 Rule

A rule is the governance-level requirement.

Example:

```text
DDD-ENTITY-001
Domain entities must inherit from Entity<TId>.
```

The rule contains:

```text
Rule ID
Rule version
Rule metadata
Target selector
Conditions
Assertions
Severity
Remediation
```

The rule should not directly perform Roslyn analysis.

---

## 3.2 Selector

A selector determines which entities a rule applies to.

Examples:

```text
Classes
Records
Methods
Projects
Packages
Files
```

with constraints such as:

```text
Namespace matches *.Domain.Entities
Project matches *.Domain
Name matches *Entity
```

Conceptually:

```text
SELECT
    Class
WHERE
    Namespace matches *.Domain.Entities
```

The selector answers:

> "What does this rule apply to?"

---

## 3.3 Predicate

A predicate evaluates a property or relationship.

Examples:

```text
HasBaseType
HasInterface
HasMethod
HasProperty
ReferencesProject
ReferencesPackage
IsPublic
IsStatic
IsAsync
```

Predicates return a logical result:

```text
true
false
```

The predicate answers:

> "Is this condition true for this entity?"

---

## 3.4 Assertion

An assertion requires a predicate to be true or false.

Conceptually:

```text
ASSERT
    HasBaseType(Entity<TId>)
```

or:

```text
ASSERT NOT
    ReferencesProject(*.Infrastructure)
```

This avoids requiring separate implementations for:

```text
MustInheritFrom
MustNotInheritFrom
```

where the underlying capability is the same.

The distinction between positive and negative assertions should be represented through composition.

For example:

```text
Assert
    Predicate: HasBaseType(Entity<TId>)
    Expected: true
```

and:

```text
Assert
    Predicate: ReferencesProject(*.Infrastructure)
    Expected: false
```

The exact implementation may differ, but the conceptual model should remain consistent.

---

## 3.5 Diagnostic

A diagnostic represents the result of a rule evaluation.

Every failure should provide enough structured information for:

* Developers.
* CI pipelines.
* Reporting systems.
* AI agents.
* Automated remediation workflows.

A diagnostic should conceptually contain:

```text
Rule ID
Rule Version
Rule Name
Severity

Target
Target Type
Source Location

Primitive / Predicate
Expected
Actual

Message
Remediation
```

Example:

```json
{
  "ruleId": "DDD-ENTITY-001",
  "ruleVersion": 2,
  "severity": "error",
  "target": {
    "kind": "class",
    "name": "Payment",
    "file": "Payment.cs",
    "line": 12
  },
  "predicate": "HasBaseType",
  "expected": "Entity<PaymentId>",
  "actual": "object",
  "message": "Domain entities must inherit from Entity<TId>.",
  "remediation": "Change Payment to inherit from Entity<PaymentId>."
}
```

---

# 4. Primitive Architecture

The existing primitive catalogue should be reviewed and consolidated.

The target architecture should favour a small number of parameterised primitives.

For example:

```text
MustHaveMethod
```

should handle:

```text
Name
Accessibility
Return type
Parameters
Static
Async
Generic arity
```

Similarly:

```text
MustHaveProperty
```

should handle:

```text
Name
Type
Accessibility
Getter
Setter
Static
Required
Nullable
```

The goal is:

```text
Small primitive set
        +
Rich parameters
        +
Composition
```

rather than:

```text
Large primitive set
        +
Many specialised variants
```

---

# 5. Primitive Categories

The primitive system should be reorganised around capabilities rather than individual rule statements.

## 5.1 Selection primitives

Examples:

```text
SelectType
SelectClass
SelectRecord
SelectInterface
SelectMethod
SelectProperty
SelectField
SelectConstructor
SelectProject
SelectPackage
SelectFile
```

Filters:

```text
ByName
ByNamespace
ByProject
ByFile
ByAttribute
ByBaseType
ByInterface
```

---

## 5.2 Type predicates

Examples:

```text
HasBaseType
ImplementsInterface
HasAttribute
HasModifier
HasAccessibility
HasGenericParameter
```

---

## 5.3 Member predicates

Examples:

```text
HasMethod
HasProperty
HasField
HasConstructor
```

These should accept structured constraints.

---

## 5.4 Dependency predicates

Examples:

```text
ReferencesType
ReferencesNamespace
ReferencesProject
ReferencesPackage
```

Dependency semantics must be explicitly defined.

---

## 5.5 Project predicates

Examples:

```text
HasProjectReference
HasPackageReference
TargetsFramework
HasProperty
```

---

## 5.6 Repository predicates

Examples:

```text
HasFile
HasDirectory
FileMatchesPattern
DirectoryMatchesPattern
```

---

## 5.7 Logical composition

The initial system should support a deliberately constrained logical language:

```text
And
Or
Not
Any
All
```

Conditional evaluation may be represented through normal composition rather than creating an extensive conditional programming model.

For example:

```text
IF class implements IDomainEvent
THEN it must be immutable
```

can conceptually become:

```text
NOT(
    Implements(IDomainEvent)
)
OR
(
    IsStructurallyImmutable
)
```

The rule language must not become a general-purpose programming language.

It should not support:

```text
Loops
Variables
Functions
Arbitrary code execution
User-defined expressions
```

If a requirement needs these capabilities, it should be implemented as a custom analyzer.

---

# 6. Composite Rules

Composite rules are the primary mechanism for building organisation-specific concepts.

For example:

```yaml
name: DomainEntity

target:
  kind: class

assertions:
  - HasBaseType:
      type: Entity<TId>

  - IsInProject:
      pattern: "*.Domain"

  - ReferencesPackage:
      name: Company.Domain

  - Not:
      ReferencesProject:
        pattern: "*.Infrastructure"
```

The engine should treat this as a composition of primitives.

The following should preferably be composites:

```text
DomainEntity
AggregateRoot
ValueObject
DomainEvent
Command
CommandHandler
Query
QueryHandler
Repository
IntegrationEvent
```

These concepts belong to the organisation's standards vocabulary, not necessarily to the core engine.

---

# 7. Primitive vs Composite vs Custom Analyzer

Every rule should fit into one of three categories.

## Primitive

A reusable engine capability.

```text
HasBaseType
HasMethod
ReferencesProject
HasPackageReference
```

Implemented in code.

---

## Composite

A declarative combination of primitives.

```text
DomainEntity
AggregateRoot
CommandHandler
```

Implemented as rule configuration.

---

## Custom Analyzer

A specialised implementation for complex analysis.

Examples:

```text
Detect incorrect event sourcing implementation
Detect complex data-flow violation
Enforce advanced async usage pattern
Detect semantic business invariant violation
```

Implemented in C# using Roslyn or other analysis APIs.

The system should not attempt to force every possible rule into the declarative primitive system.

---

# 8. Analysis Provider Architecture

The analysis model should be split into distinct domains.

The engine should avoid creating one universal abstraction such as:

```text
AnalysisModel.Type
```

that attempts to represent every possible thing.

Instead:

```text
Analysis
├── CSharp
│   ├── Type
│   ├── Method
│   ├── Property
│   └── Syntax
│
├── DotNet
│   ├── Project
│   ├── Package
│   ├── Reference
│   └── Framework
│
└── Repository
    ├── File
    ├── Directory
    └── Configuration
```

Each primitive should declare or inherently require the analysis domain it operates against.

For example:

```text
HasMethod
    → CSharp

ReferencesPackage
    → DotNet

HasFile
    → Repository
```

This keeps the system honest about what it is actually analysing.

---

# 9. Roslyn as an Analysis Provider

Roslyn should remain the primary mechanism for C# semantic analysis.

However, the core of CodeGuard should not depend directly on Roslyn concepts everywhere.

The architecture should be:

```text
                   CodeGuard
                       │
                       ▼
              Analysis Abstractions
                       │
          ┌────────────┼────────────┐
          ▼            ▼            ▼
       Roslyn        MSBuild     Repository
```

Roslyn is responsible for providing C# semantic information.

CodeGuard is responsible for evaluating organisational requirements against that information.

This separation allows CodeGuard to remain focused on governance rather than becoming a second Roslyn framework.

---

# 10. Analysis Session and Caching

The engine should introduce an explicit analysis session.

Conceptually:

```text
Repository
    │
    ▼
Analysis Session
    │
    ├── Roslyn Workspace
    ├── Compilation Cache
    ├── Semantic Model Cache
    ├── Symbol Index
    ├── Project Graph
    ├── Dependency Graph
    └── Repository Index
            │
            ▼
         CodeGuard
            │
            ├── Rule 1
            ├── Rule 2
            ├── Rule 3
            └── Rule N
```

Rules should query shared analysis data rather than repeatedly performing expensive analysis.

The engine should:

1. Load the repository once.
2. Build the required analysis context.
3. Cache reusable information.
4. Evaluate multiple rules against the same context.

This is particularly important when running hundreds or thousands of rules.

---

# 11. Dependency Semantics

Dependency analysis must explicitly define what constitutes a dependency.

The initial implementation should distinguish:

```text
Project reference
Package reference
Assembly reference
Namespace reference
Type reference
Method invocation
```

The first implementation should focus on deterministic compile-time dependencies.

For example:

```text
MustNotDependOn *.Infrastructure
```

should have an explicitly documented meaning.

Potentially:

```text
Project reference
OR
Type reference
OR
Namespace reference
```

The engine should not claim to detect runtime dependencies, reflection-based dependencies, dependency injection relationships, or messaging relationships unless it actually analyses those mechanisms.

Future dependency models may be introduced as separate capabilities.

---

# 12. Rule Versioning

Rules should be treated as versioned governance artefacts.

Every rule should have:

```text
ID
Version
Status
Severity
```

Example:

```yaml
id: DDD-ENTITY-001
version: 2
status: active
severity: error
```

Supported lifecycle states should include:

```text
experimental
active
deprecated
retired
```

Rule identity should remain stable while the version changes.

For example:

```text
DDD-ENTITY-001 v1
DDD-ENTITY-001 v2
DDD-ENTITY-001 v3
```

The system should eventually support determining:

```text
Which rule version was executed?
Which rule version produced this diagnostic?
```

This is important for auditability and reproducibility.

---

# 13. Rule Testing

Rules should become first-class testable artefacts.

Each declarative rule should have associated fixtures.

Example:

```text
rules/
└── ddd_entity_001/
    ├── rule.yaml
    └── tests/
        ├── valid/
        │   └── payment.cs
        │
        └── invalid/
            └── payment.cs
```

The test runner should verify:

```text
Valid fixture
    → No violation

Invalid fixture
    → Expected violation
```

Tests should also validate:

```text
Rule ID
Diagnostic code
Expected primitive
Expected target
```

Where appropriate.

This provides protection against changes to the underlying primitive implementations.

For example:

```text
Change MustHaveMethod
        │
        ▼
Run all rule tests
        │
        ├── 487 passed
        └── 3 failed
```

This should become a core part of developing CodeGuard.

---

# 14. Rule Change Impact

The system should be designed to eventually support rule impact analysis.

For example:

```text
DDD-ENTITY-001
Version 1 → Version 2

Affected repositories:
    23

Affected projects:
    117

New violations:
    147
```

This does not necessarily need to be implemented in the initial refactoring.

However, rule execution results should contain enough information to enable this later.

---

# 15. Diagnostics as an API Contract

Diagnostics should be treated as a stable machine-readable contract.

The output should support:

```text
CLI
CI/CD
Dashboards
Developer tooling
AI agents
```

A diagnostic should include:

```text
Rule ID
Rule Version
Severity
Target
Target Type
Source Location
Predicate / Primitive
Expected
Actual
Message
Remediation
```

The system should support structured output such as:

```json
{
  "ruleId": "DDD-ENTITY-001",
  "ruleVersion": 2,
  "severity": "error",
  "target": "Payment",
  "targetType": "Class",
  "primitive": "HasBaseType",
  "expected": "Entity<PaymentId>",
  "actual": "object",
  "location": {
    "file": "Payment.cs",
    "line": 12,
    "column": 14
  },
  "message": "Payment must inherit from Entity<PaymentId>.",
  "remediation": "Update the Payment base type."
}
```

The output should be deterministic and stable.

---

# 16. AI Integration Considerations

CodeGuard is intended to provide deterministic validation for AI-assisted development.

The AI should not be responsible for determining whether a deterministic rule has passed.

Instead:

```text
AI Agent
    │
    ▼
Generate / Modify Code
    │
    ▼
Deterministic CodeGuard
    │
    ├── PASS
    │
    └── FAIL
          │
          ▼
       Diagnostics
          │
          ▼
      AI Agent
          │
          ▼
      Remediation
```

The AI can interpret diagnostics and make changes.

CodeGuard remains the authority for deterministic requirements.

This creates a useful separation:

```text
AI
    → Generate
    → Reason
    → Remediate

CodeGuard
    → Validate
    → Enforce
    → Explain

Human
    → Define
    → Approve
    → Govern
```

CodeGuard should therefore optimise diagnostics for machine consumption as well as human consumption.

---

# 17. AI-Specific Guardrail Requirements

The engine should support an execution mode intended for AI agents.

The output should clearly distinguish:

```text
PASS
FAIL
ERROR
UNSUPPORTED
```

This distinction is important.

For example:

```text
PASS
```

means the rule was evaluated and passed.

```text
FAIL
```

means the rule was evaluated and violated.

```text
ERROR
```

means analysis failed.

```text
UNSUPPORTED
```

means the rule could not be evaluated in the current analysis context.

An AI agent must not interpret:

```text
ERROR
```

as:

```text
PASS
```

or silently ignore it.

For automated workflows, fail-closed behaviour should be available.

---

# 18. Custom Analyzer Integration

The engine should define a common interface for specialised analyzers.

Conceptually:

```text
IAnalyzer
    Analyze(AnalysisSession)
        → Diagnostics
```

The exact API should be determined by the existing implementation.

Custom analyzers should be able to use:

```text
Roslyn
MSBuild
Analysis Session
```

without requiring the declarative primitive system to represent every possible operation.

A custom analyzer should still produce the same structured diagnostics as declarative rules.

---

# 19. Refactored Project Structure

The existing project structure should be evolved toward clear capability boundaries.

An example:

```text
src/
└── CodeGuard/
    │
    ├── CodeGuard.Core/
    │   ├── Rules/
    │   ├── Diagnostics/
    │   ├── Results/
    │   └── Execution/
    │
    ├── CodeGuard.Analysis/
    │   ├── Sessions/
    │   ├── CSharp/
    │   ├── DotNet/
    │   └── Repository/
    │
    ├── CodeGuard.Analysis.Roslyn/
    │   ├── Workspace/
    │   ├── Compilation/
    │   ├── Symbols/
    │   ├── Dependencies/
    │   └── Indexing/
    │
    ├── CodeGuard.Analysis.MSBuild/
    │   ├── Projects/
    │   ├── Packages/
    │   └── References/
    │
    ├── CodeGuard.Analysis.Repository/
    │   ├── Files/
    │   └── Directories/
    │
    ├── CodeGuard.Primitives/
    │   ├── Selection/
    │   ├── Predicates/
    │   │   ├── Types/
    │   │   ├── Members/
    │   │   ├── Projects/
    │   │   ├── Dependencies/
    │   │   └── Repository/
    │   │
    │   └── Logic/
    │
    ├── CodeGuard.Declarative/
    │   ├── Loading/
    │   ├── Parsing/
    │   ├── Composition/
    │   └── Evaluation/
    │
    ├── CodeGuard.Analyzers/
    │   ├── Abstractions/
    │   └── Execution/
    │
    ├── CodeGuard.Configuration/
    │   ├── Schema/
    │   ├── Serialization/
    │   └── Validation/
    │
    └── CodeGuard.Cli/
        ├── Commands/
        ├── Output/
        └── Formatting/
```

The exact structure may differ from the existing implementation.

The key architectural boundaries are:

```text
Core
    ↓
Analysis Sessions
    ↓
Analysis Providers
    ↓
Primitives
    ↓
Declarative Rules
    ↓
Custom Analyzers
    ↓
Unified Diagnostics
    ↓
CLI / Integrations
```

These boundaries should be maintained without introducing unnecessary abstractions.

---

# 20. Refactoring Strategy

The refactoring should be incremental.

## Phase 1 — Stabilise existing behaviour

Before changing architecture:

* Add tests around existing primitives.
* Capture current behaviour.
* Ensure existing rules continue to produce the same results.
* Establish baseline performance.

Do not combine architectural refactoring with large behavioural changes.

---

## Phase 2 — Separate concepts

Refactor the internal model to clearly distinguish:

```text
Rule
Selector
Predicate
Assertion
Diagnostic
```

Existing concepts can be adapted rather than rewritten immediately.

---

## Phase 3 — Consolidate primitives

Review the existing primitive catalogue.

Identify primitives that are simply variations of the same capability.

For example:

```text
MustHavePublicMethod
MustHaveStaticMethod
MustHaveAsyncMethod
```

should become:

```text
HasMethod
```

with structured constraints.

Remove duplication where practical.

---

## Phase 4 — Introduce composite rules

Move organisation-specific concepts out of engine code.

For example:

```text
DomainEntity
AggregateRoot
DomainEvent
```

should become declarative compositions wherever possible.

---

## Phase 5 — Introduce analysis sessions

Centralise:

```text
Roslyn Workspace
Compilation
Semantic Models
Symbol Index
Project Graph
Dependency Graph
```

Ensure multiple rules reuse analysis results.

---

## Phase 6 — Introduce custom analyzers

Add an explicit extension mechanism for rules that cannot or should not be represented declaratively.

Ensure custom analyzers produce standard diagnostics.

---

## Phase 7 — Strengthen diagnostics

Standardise:

```text
Expected
Actual
Location
Rule ID
Rule Version
Primitive
Remediation
```

Ensure the output is suitable for AI consumption.

---

## Phase 8 — Add rule metadata and versioning

Introduce:

```text
Rule ID
Version
Status
Severity
```

without immediately requiring a complex version management system.

---

## Phase 9 — Add rule fixture testing

Build a rule test runner that can execute rules against valid and invalid fixtures.

Make rule tests part of the normal development workflow.

---

# 21. Definition of Done

The refactoring should be considered successful when:

### Primitive architecture

* The primitive vocabulary is smaller and more generic.
* Primitive variations are expressed through parameters.
* Selectors and predicates have clear responsibilities.
* Composite concepts are represented declaratively.

### Analysis

* Roslyn is treated as an analysis provider.
* MSBuild/project analysis is separate.
* Repository analysis is separate.
* Analysis results can be reused across rules.
* Dependency semantics are documented.

### Extensibility

* New primitives can be added independently.
* Composite rules can be added without engine code changes.
* Custom analyzers can be added when declarative rules are insufficient.

### Governance

* Rules have stable IDs.
* Rules have versions.
* Rules have lifecycle states.
* Rules have severity.

### Testing

* Rules can be tested against fixtures.
* Primitive changes run regression tests.
* Invalid rules produce deterministic diagnostics.

### AI integration

* Diagnostics are structured.
* Expected and actual values are available.
* Analysis failures are distinguishable from rule failures.
* The engine can be run in fail-closed mode.
* AI agents can consume results deterministically.

---

# 22. Target Architecture

The final conceptual architecture should be:

```text
                         ENGINEERING GOVERNANCE
                                  │
                                  ▼
                            RULE DEFINITIONS
                                  │
                    ┌─────────────┴──────────────┐
                    │                            │
             COMPOSITE RULES              CUSTOM ANALYZERS
                    │                            │
                    ▼                            ▼
              PRIMITIVES                    ROSLYN / APIs
                    │                            │
                    └─────────────┬──────────────┘
                                  ▼
                         ANALYSIS SESSION
                                  │
               ┌──────────────────┼──────────────────┐
               ▼                  ▼                  ▼
            C# / Roslyn        .NET / MSBuild     Repository
               │                  │                  │
               └──────────────────┼──────────────────┘
                                  ▼
                          RULE EVALUATION
                                  │
                                  ▼
                            DIAGNOSTICS
                                  │
               ┌──────────────────┼──────────────────┐
               ▼                  ▼                  ▼
              CLI                CI               AI Agent
```

The core philosophy of the refactored system is:

> **Keep the engine small. Keep the primitives generic. Keep organisation-specific knowledge in declarative rules. Use custom analyzers when the problem genuinely requires code.**

This creates a system where the **engine changes relatively infrequently**, while the organisation can continuously evolve its engineering standards and governance through versioned, tested rule definitions.

The ultimate goal is not to build a better Roslyn analyzer framework. It is to build a **deterministic engineering governance layer** that can sit underneath your AI development workflows and act as a trusted validation boundary.
