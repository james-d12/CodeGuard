# Deterministic Engineering Rules & Analysis Engine

## 1. Purpose

This project provides a deterministic analysis and validation engine for enforcing an organisation's engineering standards across software repositories.

The engine is intended to operate alongside AI agents, skills, and workflows.

The goal is to provide a reliable, deterministic feedback mechanism that AI agents can use to:

1. Understand applicable engineering constraints.
2. Validate generated or modified code.
3. Identify violations.
4. Provide structured feedback to the agent.
5. Prevent invalid changes from progressing through automated workflows.

The engine should not attempt to replace AI reasoning, human review, or engineering standards.

Instead, the responsibilities are:

* **Standards** define how the organisation builds software.
* **Rules** express machine-checkable requirements derived from those standards.
* **Skills** describe how agents should perform particular tasks.
* **Agents** perform reasoning and execute work.
* **Workflows** orchestrate agents and determine the sequence of activities.
* **The analysis engine** deterministically evaluates rules and reports violations.

The intended initial implementation is a .NET/C# application using Roslyn and MSBuild analysis capabilities.

---

# 2. High-Level Architecture

The overall model is:

```text
                         COMPANY WAYS OF WORKING
                                    │
                                    ▼
                               STANDARDS
                                    │
                      ┌─────────────┴─────────────┐
                      │                           │
                      ▼                           ▼
                    RULES                       SKILLS
              "What must be true"         "How to do the work"
                      │                           │
                      ▼                           │
               RULES ENGINE                      │
                      │                           │
                      │                           ▼
                      │                         AGENTS
                      │                           │
                      └─────────────┬─────────────┘
                                    ▼
                                 WORKFLOW
                                    │
                                    ▼
                               CODE CHANGE
                                    │
                                    ▼
                         DETERMINISTIC ANALYSIS
                                    │
                         ┌──────────┴──────────┐
                         ▼                     ▼
                       PASS                  FAIL
                                               │
                                               ▼
                                      Agent remediation
```

The engine should support both:

### Pre-generation / pre-change usage

An AI agent retrieves applicable rules before generating code.

```text
Agent
  │
  ├── Load relevant standards
  ├── Load relevant skills
  ├── Load applicable rules
  │
  ▼
Generate code
```

### Post-generation / validation usage

The deterministic engine validates the resulting code.

```text
Generated code
      │
      ▼
Rules Engine
      │
      ├── PASS
      │
      └── FAIL
            │
            ▼
      Structured violations
            │
            ▼
         AI Agent
            │
            ▼
          Fix
```

This creates a deterministic feedback loop for AI-driven development.

---

# 3. Design Principles

## 3.1 Deterministic

Given the same repository state, configuration, and rules, the engine should produce the same result.

The core validation process must not rely on an LLM.

AI may consume the results, but the engine itself should remain deterministic.

---

## 3.2 Declarative

Rules should primarily be expressed as data rather than custom C# code.

A rule should ideally be defined by:

* A unique identifier.
* A human-readable name.
* A description.
* A target selector.
* One or more assertions.
* Optional conditions.
* Severity.
* References to standards.
* Optional remediation guidance.

Example:

```yaml
id: DDD-ENTITY-001
name: Domain entities must inherit from Entity

description: >
  All domain entities must inherit from the approved
  Entity<TId> base class.

standard: DDD-001

target:
  kind: class
  namespace: "*.Domain.Entities"

assertions:
  - must_inherit_from:
      type: "Company.Domain.Entity<TId>"

severity: error
```

The goal is to allow new organisational rules to be added without modifying the analysis engine.

---

## 3.3 Modular

The engine must be designed so that new capabilities can be added independently.

Examples:

* New analysis providers.
* New rule primitives.
* New file formats.
* New project systems.
* New programming languages in the future.
* New rule output formats.

The core engine should not become tightly coupled to Roslyn.

---

## 3.4 Extensible

The system should support a plugin-like internal architecture.

For example:

```text
Analysis Engine
    │
    ├── Roslyn Provider
    ├── MSBuild Provider
    ├── Repository Provider
    ├── Configuration Provider
    └── Future Providers
```

New analysis providers should be addable without rewriting the rule evaluation engine.

---

## 3.5 Human-readable

Rules are part of organisational governance.

A developer should be able to review a rule PR and understand:

* What the organisation is enforcing.
* Why the rule exists.
* What code it applies to.
* What constitutes a violation.

Rules should therefore be easy to read, review, version, and discuss.

---

## 3.6 AI-friendly

The engine should produce structured output that AI agents can consume.

Example:

```json
{
  "status": "failed",
  "violations": [
    {
      "ruleId": "DDD-ENTITY-004",
      "severity": "error",
      "file": "src/Payments.Domain/Entities/Payment.cs",
      "line": 12,
      "message": "Domain entities must have a private or protected constructor.",
      "remediation": "Change the constructor accessibility to private or protected."
    }
  ]
}
```

The output should be concise enough for AI agents while retaining enough information for humans.

---

# 4. Standards, Rules, Skills, and Agents

## Standards

Standards represent organisational engineering policies and principles.

Examples:

```text
DDD
Event-Driven Architecture
C# Engineering
Testing
Security
Observability
Financial Controls
Auditability
```

Standards answer:

> What does the organisation believe is good engineering practice?

Not every standard needs to be deterministically enforceable.

---

## Rules

Rules are machine-checkable expressions of standards.

Example:

```text
Standard:
Domain entities must be encapsulated.

Rule:
Domain entities must not expose public setters.
```

Rules answer:

> What can we prove automatically?

---

## Skills

Skills describe how an agent should perform a task.

Example:

```text
Create Domain Entity

1. Identify aggregate boundary.
2. Identify invariants.
3. Create entity.
4. Use approved base class.
5. Add domain behaviour.
6. Add tests.
7. Run deterministic validation.
```

Skills answer:

> How should the agent perform the work?

---

## Agents

Agents reason about a problem and execute skills.

They should not be relied upon as the sole enforcement mechanism.

Agents may use:

* Standards for context.
* Skills for procedure.
* Rules for constraints.
* The analysis engine for deterministic validation.

---

# 5. Core Engine Model

The engine should conceptually implement:

```text
Rule
 │
 ├── Target Selector
 │
 ├── Conditions
 │
 └── Assertions
          │
          ▼
     Rule Evaluator
          │
          ▼
    Analysis Providers
          │
          ▼
    Analysis Model
          │
          ▼
      Violations
```

A rule can be viewed as:

```text
WHEN
    target matches

AND

    conditions are satisfied

THEN

    assertions must pass
```

Conceptually:

```text
SELECT
    all classes in *.Domain.Entities

ASSERT
    each class must inherit Entity<TId>
```

---

# 6. Analysis Model

The engine should build a common internal analysis model.

The rule system should operate against this model rather than directly coupling every rule to Roslyn APIs.

Possible entities include:

## Repository

```text
Repository
Directory
File
Path
FileName
FileExtension
FileContent
```

## Project

```text
Solution
Project
ProjectReference
PackageReference
FrameworkReference
TargetFramework
ProjectProperty
ProjectItem
ProjectSdk
```

## C# symbols

```text
Namespace
Type
Class
Record
Struct
Interface
Enum
Delegate

Method
Constructor
Property
Field
Event
Parameter
```

## Type relationships

```text
BaseType
Interface
GenericType
GenericParameter
ContainingType
ContainingNamespace
ContainingAssembly
```

## C# metadata

```text
Attribute
Modifier
Accessibility
Static
Abstract
Sealed
Virtual
Override
Readonly
Const
Async
Partial
Unsafe
Required
Nullable
```

## Dependencies

```text
TypeReference
MethodCall
PropertyAccess
FieldAccess
ConstructorCall
ObjectCreation
NamespaceReference
UsingDirective
AssemblyReference
PackageReference
```

---

# 7. Analysis Providers

Analysis providers are responsible for building or exposing parts of the analysis model.

## 7.1 Roslyn Provider

Responsible for C# semantic and syntax analysis.

Capabilities include:

* Types.
* Classes.
* Records.
* Interfaces.
* Methods.
* Properties.
* Fields.
* Constructors.
* Attributes.
* Base types.
* Interfaces.
* Generic parameters.
* Accessibility.
* Modifiers.
* Type references.
* Method calls.
* Object creation.
* Namespace dependencies.

---

## 7.2 MSBuild Provider

Responsible for project-level information.

Capabilities include:

* Projects.
* Project references.
* Package references.
* Target frameworks.
* Project properties.
* Project SDK.
* Project items.
* Build configuration.

---

## 7.3 Repository Provider

Responsible for repository-level information.

Capabilities include:

* Files.
* Directories.
* Paths.
* File names.
* File extensions.
* Repository configuration.

---

## 7.4 Future Providers

The architecture should allow future providers for:

* YAML.
* JSON.
* Terraform.
* Kubernetes.
* Docker.
* SQL.
* OpenAPI.
* Git metadata.
* CI/CD configuration.
* Other programming languages.

These should be optional modules.

---

# 8. Primitive Vocabulary

The following primitives represent the proposed initial vocabulary.

The list is intentionally broad. The initial implementation should prioritise a smaller subset and expand based on real rules.

---

## 8.1 Target Selectors

Selectors determine which entities a rule applies to.

### Type selectors

```text
SelectClass
SelectRecord
SelectStruct
SelectInterface
SelectEnum
SelectDelegate
SelectType
```

### Namespace selectors

```text
InNamespace
NamespaceMatches
NamespaceStartsWith
NamespaceEndsWith
NamespaceContains
```

### Project selectors

```text
InProject
ProjectMatches
ProjectNameMatches
ProjectNameStartsWith
ProjectNameEndsWith
```

### File selectors

```text
InFile
FileMatches
FileExtension
FilePathMatches
DirectoryMatches
```

### Inheritance selectors

```text
InheritsFrom
InheritsDirectlyFrom
InheritsIndirectlyFrom
```

### Interface selectors

```text
Implements
ImplementsDirectly
ImplementsIndirectly
```

### Attribute selectors

```text
HasAttribute
HasAnyAttribute
HasAllAttributes
```

### Modifier selectors

```text
IsPublic
IsPrivate
IsProtected
IsInternal
IsStatic
IsAbstract
IsSealed
IsReadonly
IsPartial
IsAsync
IsVirtual
IsOverride
```

### Generic selectors

```text
IsGeneric
GenericArity
HasGenericParameter
```

### Name selectors

```text
NameEquals
NameMatches
NameStartsWith
NameEndsWith
NameContains
NameMatchesRegex
```

### Package selectors

```text
InProjectReferencingPackage
ProjectHasPackage
ProjectUsesPackage
```

### Dependency selectors

```text
ReferencesType
ReferencesNamespace
ReferencesProject
ReferencesPackage
DependsOn
```

---

# 9. Type Assertions

```text
MustInheritFrom
MustNotInheritFrom
MustDirectlyInheritFrom
MustIndirectlyInheritFrom

MustImplement
MustNotImplement
MustDirectlyImplement
MustImplementAny
MustImplementAll

MustHaveAttribute
MustNotHaveAttribute
MustHaveAnyAttribute
MustHaveAllAttributes
MustNotHaveAnyAttribute

MustBePublic
MustBePrivate
MustBeProtected
MustBeInternal
MustBeProtectedInternal
MustBePrivateProtected

MustBeStatic
MustNotBeStatic

MustBeAbstract
MustNotBeAbstract

MustBeSealed
MustNotBeSealed

MustBeReadonly
MustNotBeReadonly

MustBePartial
MustNotBePartial

MustBeRecord
MustNotBeRecord

MustBeGeneric
MustNotBeGeneric
MustHaveGenericParameter
MustHaveGenericArity
MustHaveGenericParameterNamed
```

---

# 10. Method Assertions

```text
MustHaveMethod
MustNotHaveMethod
MustHaveMethodNamed
MustNotHaveMethodNamed

MustHaveReturnType
MustNotReturnType

MustHaveParameter
MustNotHaveParameter

MustHaveParameterCount
MustHaveParameterTypes
MustHaveParameterNamed

MustReturnTypeAssignableTo
MustAcceptType
```

Method constraints may include:

```text
Name
Accessibility
ReturnType
ParameterCount
ParameterTypes
ParameterNames
GenericArity
Async
Static
Virtual
Override
Abstract
```

Potential method body assertions:

```text
MustCall
MustNotCall
MustCallMethod
MustNotCallMethod

MustCreateType
MustNotCreateType

MustAccessProperty
MustNotAccessProperty

MustAccessField
MustNotAccessField

MustThrow
MustNotThrow

MustAwait
MustNotAwait
```

Method-body assertions should be introduced carefully because they can create brittle rules.

---

# 11. Property Assertions

```text
MustHaveProperty
MustNotHaveProperty

MustHaveGetter
MustHaveSetter
MustNotHaveSetter

MustHavePublicGetter
MustHavePrivateSetter

MustBeRequired
MustNotBeRequired

MustBeNullable
MustNotBeNullable
```

Property constraints:

```text
Name
Type
Accessibility
GetterAccessibility
SetterAccessibility
Static
Readonly
Required
Nullable
```

---

# 12. Field Assertions

```text
MustHaveField
MustNotHaveField

MustBeStatic
MustNotBeStatic

MustBeReadonly
MustNotBeReadonly

MustBeConst
MustNotBeConst
```

---

# 13. Constructor Assertions

```text
MustHaveConstructor
MustNotHaveConstructor

MustHaveConstructorWith
MustHaveConstructorWithParameters

MustHavePrivateConstructor
MustHaveProtectedConstructor
MustHavePublicConstructor
```

Constraints:

```text
Accessibility
ParameterCount
ParameterTypes
ParameterNames
```

---

# 14. Namespace Assertions

```text
MustBeInNamespace
MustNotBeInNamespace

MustHaveNamespacePrefix
MustHaveNamespaceSuffix

MustMatchNamespacePattern
```

---

# 15. Project Assertions

```text
MustBeInProject
MustNotBeInProject

MustHaveProjectReference
MustNotHaveProjectReference

MustReferenceProject
MustNotReferenceProject

MustHavePackageReference
MustNotHavePackageReference

MustReferencePackage
MustNotReferencePackage

MustUsePackageVersion
MustUsePackageVersionAtLeast
MustUsePackageVersionAtMost
MustUsePackageVersionExactly
```

---

# 16. Dependency Assertions

```text
MustDependOn
MustNotDependOn

MustReference
MustNotReference

MustOnlyDependOn
MustDependOnlyOn

MustHaveAllowedDependencies
```

Dependency relationships may operate at:

```text
Type → Type
Type → Namespace
Type → Project
Project → Project
Project → Package
Project → Namespace
```

This is a key capability for enforcing DDD and layered architecture.

---

# 17. File Assertions

```text
MustHaveFile
MustNotHaveFile

MustBeInDirectory
MustNotBeInDirectory

MustMatchFileName
MustMatchFilePattern

MustHaveExtension
MustNotHaveExtension
```

---

# 18. Repository Assertions

```text
MustHaveDirectory
MustNotHaveDirectory

MustHaveFile
MustNotHaveFile

MustHaveConfiguration
MustHaveConfigurationValue
```

---

# 19. Naming Assertions

```text
MustFollowNamingConvention
MustMatchNamingPattern

MustUsePascalCase
MustUseCamelCase
MustUseSnakeCase
MustUseKebabCase

MustEndWith
MustStartWith
MustContain
MustNotContain
```

---

# 20. Cardinality Assertions

```text
MustHaveAtLeast
MustHaveAtMost
MustHaveExactly
MustHaveOne
MustHaveNone
MustHaveAny
MustHaveAll
```

---

# 21. Relationship Assertions

```text
MustBeRelatedTo
MustNotBeRelatedTo

MustHaveParent
MustHaveChild

MustBelongTo
MustBeOwnedBy

MustBeInSameProjectAs
MustBeInDifferentProjectFrom
```

---

# 22. Conditional and Logical Primitives

Rules should support composition.

```text
When
If
Unless
OnlyWhen

And
Or
Not

Any
All
None
```

Example:

```yaml
when:
  - implements: "IDomainEvent"

then:
  - must_be_immutable
  - must_be_in_namespace:
      pattern: "*.Domain.Events"
```

The expression language should remain intentionally constrained.

Avoid evolving the DSL into a general-purpose programming language.

---

# 23. Cross-Entity Assertions

Potential future capabilities:

```text
MustHaveCorresponding
MustHaveMatching
MustHaveRelated

MustHaveOneToOne
MustHaveOneToMany
```

Example:

```text
Every CommandHandler must have a corresponding Command.
```

These should be implemented only when real requirements justify them.

---

# 24. Composite Concepts

Higher-level architectural concepts should be built from primitive rules.

For example:

```text
DomainEntity
```

could compose:

```text
IsClass

MustInheritFrom Entity<TId>

MustBeInProject *.Domain

MustReferencePackage Company.Domain

MustHaveConstructor
    private OR protected

MustNotDependOn *.Infrastructure
```

Then:

```text
AggregateRoot
```

could extend:

```text
DomainEntity
```

and add:

```text
MustImplement IAggregateRoot
MustHaveMethod Create
```

Other possible concepts:

```text
DomainEntity
AggregateRoot
ValueObject
DomainEvent
Command
CommandHandler
Query
QueryHandler
IntegrationEvent
EventHandler
Repository
```

These concepts should ideally be declarative compositions rather than hard-coded engine behaviour.

---

# 25. Architecture Rules

Higher-level architecture primitives may eventually include:

```text
MustBeInLayer
MustNotCrossLayer
MustRespectDependencyDirection
MustBeInBoundedContext
MustNotCrossBoundedContext
MustHaveArchitectureRole
```

For example:

```text
Domain
  └── must not depend on
          Infrastructure
```

This may be represented internally as project/layer metadata plus generic dependency assertions.

---

# 26. Event-Driven Architecture Rules

Potential future primitives:

```text
MustPublishEvent
MustNotPublishEvent

MustConsumeEvent
MustNotConsumeEvent

MustHaveEventHandler
MustHaveConsumer

MustHaveEventContract
MustMatchEventContract

MustBeVersioned
MustBeBackwardCompatible

MustBeIdempotent
```

Not all of these are necessarily deterministically provable.

Rules should therefore support classification:

```text
deterministic
partially_deterministic
ai_review
human_review
```

For example:

```text
MustPublishEvent
```

may be deterministic.

Whereas:

```text
MustBeIdempotent
```

may require AI or human review depending on implementation.

---

# 27. Testing Rules

Potential capabilities:

```text
MustHaveTest
MustHaveTests
MustHaveTestProject
MustHaveTestFor
MustHaveTestMethod
MustHaveTestAttribute
```

Example:

```text
Every domain entity must have corresponding domain tests.
```

---

# 28. Configuration Rules

For non-C# configuration:

```text
MustHaveConfig
MustHaveConfigValue
MustNotHaveConfig

MustMatchConfigValue

MustUseEnvironmentVariable
MustNotHardcodeSecret
```

Potentially supporting:

* JSON.
* YAML.
* XML.
* Environment configuration.

---

# 29. Security Rules

Potential future rules:

```text
MustNotContainSecret
MustNotLog
MustNotLogProperty

MustRequireAuthorization
MustNotAllowAnonymous

MustUseApprovedPackage
MustNotUsePackage

MustUseApprovedCrypto
```

These should be added incrementally based on real security requirements.

---

# 30. Rule Metadata

Every rule should have metadata.

Example:

```yaml
id: DDD-ENTITY-001

name: Domain entities must inherit from Entity

description: >
  Domain entities must inherit from the approved
  Entity<TId> base class.

category: architecture

standard: DDD-001

severity: error

enforcement:
  deterministic: true

tags:
  - ddd
  - domain
  - entity

remediation: >
  Inherit from Company.Domain.Entity<TId>.

documentation:
  - DDD-001

enabled: true
```

Possible severity levels:

```text
info
warning
error
critical
```

---

# 31. Rule Results

The engine should produce structured results.

```text
ValidationResult
    Status
    RulesEvaluated
    RulesPassed
    RulesFailed
    Violations
```

A violation should include:

```text
RuleId
StandardId
Severity
Message
File
Line
Column
Symbol
Project
Remediation
DocumentationReference
```

Example:

```json
{
  "ruleId": "DDD-ENTITY-001",
  "severity": "error",
  "message": "Domain entity 'Payment' must inherit from Entity<TId>.",
  "project": "Payments.Domain",
  "file": "Entities/Payment.cs",
  "line": 12,
  "column": 14,
  "symbol": "Payment",
  "remediation": "Inherit from Company.Domain.Entity<TId>."
}
```

---

# 32. Proposed Modular Folder Structure

The implementation should be modular and avoid a monolithic project.

A potential structure:

```text
src/
│
├── RulesEngine/
│   ├── RulesEngine.sln
│   │
│   ├── RulesEngine.Cli/
│   │   ├── Commands/
│   │   ├── Options/
│   │   └── Program.cs
│   │
│   ├── RulesEngine.Core/
│   │   ├── Rules/
│   │   ├── Results/
│   │   ├── Evaluation/
│   │   └── Metadata/
│   │
│   ├── RulesEngine.RuleModel/
│   │   ├── Rules/
│   │   ├── Selectors/
│   │   ├── Assertions/
│   │   ├── Conditions/
│   │   └── Combinators/
│   │
│   ├── RulesEngine.Analysis/
│   │   ├── AnalysisModel/
│   │   ├── Providers/
│   │   └── Workspace/
│   │
│   ├── RulesEngine.Analyzers.Roslyn/
│   │   ├── Symbols/
│   │   ├── Types/
│   │   ├── Members/
│   │   ├── Dependencies/
│   │   └── Syntax/
│   │
│   ├── RulesEngine.Analyzers.MSBuild/
│   │   ├── Projects/
│   │   ├── Packages/
│   │   ├── References/
│   │   └── Properties/
│   │
│   ├── RulesEngine.Analyzers.Repository/
│   │   ├── Files/
│   │   ├── Directories/
│   │   └── Configuration/
│   │
│   ├── RulesEngine.Evaluation/
│   │   ├── Selectors/
│   │   ├── Assertions/
│   │   ├── Conditions/
│   │   └── Combinators/
│   │
│   ├── RulesEngine.Configuration/
│   │   ├── Loading/
│   │   ├── Discovery/
│   │   ├── Parsing/
│   │   └── Validation/
│   │
│   └── RulesEngine.Reporting/
│       ├── Console/
│       ├── Json/
│       └── Sarif/
│
tests/
│
├── RulesEngine.Core.Tests/
├── RulesEngine.RuleModel.Tests/
├── RulesEngine.Evaluation.Tests/
├── RulesEngine.Analyzers.Roslyn.Tests/
├── RulesEngine.Analyzers.MSBuild.Tests/
├── RulesEngine.Analyzers.Repository.Tests/
├── RulesEngine.Configuration.Tests/
└── RulesEngine.IntegrationTests/
│
rules/
│
├── standards/
├── architecture/
├── csharp/
├── ddd/
├── event-driven/
├── security/
└── testing/
```

This is a starting point, not a mandatory final structure.

The implementation should avoid prematurely creating projects for every conceptual namespace if they do not have independent responsibilities.

The key boundaries are:

```text
Core
Rule Model
Analysis Providers
Evaluation
Configuration
Reporting
CLI
```

---

# 33. Configurable Repository Discovery

The engine must not assume a fixed repository structure.

Different repositories may store:

```text
.github/agents
.github/skills
standards
.rules
engineering
docs/standards
```

Therefore, discovery should be configurable.

Example:

```yaml
repository:
  standards:
    - ".engineering/standards"

  rules:
    - ".engineering/rules"

  skills:
    - ".github/skills"

  agents:
    - ".github/agents"

  source:
    - "src"

  tests:
    - "tests"
```

The engine should not require all of these to exist.

The discovery layer should resolve configured locations into an internal model.

This is particularly important because the organisation may evolve its repository structures independently of the validation engine.

---

# 34. AI Integration

The deterministic engine should be designed to work with the existing AI architecture.

Example:

```text
User Story
    │
    ▼
Orchestrator Agent
    │
    ├── Select Skills
    │
    ├── Identify Relevant Standards
    │
    └── Identify Relevant Rules
    │
    ▼
Implementation Agent
    │
    ▼
Code Change
    │
    ▼
Deterministic Validation
    │
    ├── PASS ──────────────► Next Workflow Step
    │
    └── FAIL
          │
          ▼
    Structured Violations
          │
          ▼
    Agent Remediation
          │
          ▼
    Validation Again
```

The agent should never be expected to perfectly remember all standards.

The deterministic engine provides the final machine-checkable guardrail.

---

# 35. Initial Implementation Scope

The first implementation should deliberately avoid attempting to implement the entire primitive catalogue.

Recommended initial capabilities:

## Analysis

* Roslyn workspace loading.
* MSBuild project discovery.
* Repository file discovery.

## Target selectors

* Type.
* Class.
* Namespace.
* Project.
* Base type.
* Interface.
* Attribute.
* Package.

## Assertions

* MustInheritFrom.
* MustImplement.
* MustHaveMethod.
* MustHaveProperty.
* MustHaveConstructor.
* MustBeInNamespace.
* MustBeInProject.
* MustReferencePackage.
* MustNotReferencePackage.
* MustReferenceProject.
* MustNotReferenceProject.
* MustNotDependOn.

## Logic

* When.
* And.
* Or.
* Not.

## Reporting

* Console.
* JSON.
* SARIF.

## Configuration

* Configurable repository discovery.
* YAML/JSON rule loading.
* Rule schema validation.

---

# 36. Initial Real-World Validation

Before implementing a large number of primitives, the planning agent should identify approximately 10-20 real existing standards from the organisation.

Examples:

```text
Domain entity requirements
Aggregate requirements
Domain event requirements
Command requirements
Command handler requirements
Project dependency rules
NuGet package requirements
Namespace rules
File naming rules
Testing requirements
```

Each requirement should be classified:

```text
Can be expressed using existing primitive
    OR
Requires new primitive
    OR
Requires custom analysis provider
    OR
Cannot be deterministically validated
```

This exercise should drive the initial primitive set.

The system should not be designed around hypothetical requirements alone.

---

# 37. Important Architectural Constraint

The engine should distinguish between:

### Primitive

A low-level reusable capability.

Example:

```text
MustInheritFrom
```

### Composite

A reusable collection of primitives.

Example:

```text
DomainEntity
```

### Standard

An organisational policy.

Example:

```text
Domain entities must encapsulate business behaviour.
```

### Rule

A machine-checkable implementation of a standard.

Example:

```text
Domain entities must have private setters.
```

The hierarchy is:

```text
STANDARD
    │
    └── RULE
          │
          └── COMPOSITE
                │
                └── PRIMITIVES
```

This distinction should remain explicit in the design.

---

# 38. Initial Planning Agent Task

An AI planning agent should be tasked with planning the initial implementation rather than immediately writing the entire system.

The planning agent should:

1. Review this design document.
2. Inspect the existing repository structure.
3. Identify the existing agents, skills, standards, and workflows.
4. Identify the existing .NET/C# conventions.
5. Identify approximately 10-20 real standards that could be validated.
6. Categorise each standard by enforcement mechanism.
7. Identify the minimum viable primitive vocabulary.
8. Design the initial analysis model.
9. Design the Roslyn/MSBuild integration.
10. Design the rule schema.
11. Design repository discovery and configuration.
12. Propose the modular project structure.
13. Define the CLI interface.
14. Define test strategy.
15. Produce an implementation plan broken into small, independently reviewable increments.

The planning agent should **not assume that every requirement can or should be expressed as a deterministic rule**.

The plan should explicitly identify:

```text
Deterministic
AI-assisted review
Human review
Not currently enforceable
```

---

# 39. Recommended Initial CLI

The initial CLI could expose commands such as:

```text
rules-engine validate
rules-engine list-rules
rules-engine explain-rule DDD-ENTITY-001
rules-engine list-standards
```
