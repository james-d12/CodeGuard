I want you to PLAN the MVP for a new CodeGuard Reporting API.

IMPORTANT:
- Do NOT implement anything yet.
- Do NOT modify the repository.
- This is a planning/review task only.
- You have the full CodeGuard source tree available locally, so inspect the actual code before proposing the design.
- Be critical. Do not blindly accept the architecture I describe below.
- Where you see flaws, over-engineering, unnecessary complexity, or better alternatives, explicitly call them out.

## Context

CodeGuard is a .NET CLI that performs deterministic validation of a repository against organisation-specific engineering standards.

The core concept is:

- Rules are declarative YAML.
- Rules currently focus primarily on .NET/C# and repository/file-system structure.
- CodeGuard is deliberately NOT intended to replace SonarQube/SonarCloud, security scanners, SCA, etc.
- It complements those tools by enforcing organisation-specific engineering policies and standards.
- CodeGuard can run entirely locally, with no data leaving the machine.
- In CI, however, we want the option to centrally report validation results.

The next major capability is centralised reporting.

The desired high-level architecture is:

    CodeGuard CLI
          |
          | HTTPS
          v
    CodeGuard Reporting API
          |
          v
      PostgreSQL

The API and dashboard will be separate products/components.

For now I ONLY want to build the Reporting API MVP.

The dashboard comes later.

## Deployment model

For now CodeGuard is SELF-HOSTED.

Do NOT design this as a SaaS/multi-tenant platform.

The immediate goal is:

1. Build the Reporting API.
2. Package it as a Docker image.
3. Make it possible to run locally/self-hosted with PostgreSQL.
4. Later we can add Helm/Kubernetes deployment.
5. The dashboard will be added later.

Do not introduce Kubernetes/Helm complexity into the MVP unless there is a very strong reason.

A simple Docker Compose setup for local/self-hosted use is likely sufficient for the first deployment target.

## Important architectural boundary

I am considering a public NuGet package for the API contracts, something like:

    CodeGuard.Reporting.Contracts

The CLI should communicate with the API using these contracts.

The contracts package should NOT expose internal CodeGuard implementation details.

For example, I do NOT want the API contract to simply become a transport representation of internal:

    CodeGuard.Core.ValidationResult
    CodeGuard.Core.Violation

Instead, define an explicit reporting contract/API model that represents the information useful for central reporting.

Critically evaluate this approach and propose the correct boundary.

The contracts package should ideally be usable by:
- CodeGuard CLI
- future CodeGuard dashboard/API clients
- potentially other integrations later

## Reporting behaviour

Local execution:

    codeguard validate

should remain completely local.

No network request should happen unless reporting is explicitly configured.

CI execution may report results centrally.

We want the central system to answer questions such as:

- What is the current state of all repositories?
- Which repositories are failing CodeGuard?
- Which rules are causing the most violations?
- What violations exist in a repository?
- What happened on main?
- How is compliance changing over time?
- Which repositories have not reported recently?
- Which branches/runs have violations?
- What CodeGuard version/rule version was used?

Do not over-design the analytics/dashboard layer yet.

The API MVP only needs to persist enough structured information to support these future questions.

## Payload size

Be conscious that CodeGuard can potentially generate many violations.

I do NOT want the CLI blindly uploading huge reports containing unnecessary analysis data.

The design should distinguish between:

- data required for central reporting
- local diagnostic/analysis data
- SARIF/HTML/full reports that may remain local CI artifacts

Consider:
- DTO design
- payload size
- compression if useful
- maximum request size
- potentially batching/chunking if actually justified

Do not introduce complexity just because it might theoretically be needed. Assess whether batching is actually necessary for the MVP.

## Persistence

Persistence has not been designed yet.

I want you to investigate and recommend an MVP persistence model.

Likely technology:

- PostgreSQL
- Entity Framework Core

But don't assume this blindly. Explain whether it is appropriate.

We need to consider entities such as:

- Repository
- Analysis/Validation Run
- Rule result
- Violation

Potentially:
- organisation/team metadata
- branch
- commit
- rule information
- CodeGuard version
- rule-set/version information

But do NOT create unnecessary entities just because they sound useful.

The model should be designed around the actual reporting use cases.

Think carefully about:
- identifiers
- uniqueness
- repository identity
- commit SHA
- branch
- repeated CI runs
- duplicate submissions
- idempotency
- retention
- indexing
- query patterns
- whether violations should be stored individually
- whether aggregate run statistics should also be persisted
- whether rule metadata belongs in the API database or should remain owned by the rule repository

## Rules are separate

CodeGuard also has a separate concept of centralised rule distribution.

A repository can configure CodeGuard to pull rules from a Git repository.

For example, the existing `codeguard setup` functionality can take a Git repository containing rules and clone them locally.

This rule distribution system is a SEPARATE concern from reporting.

Do not merge rule package/distribution into the Reporting API MVP.

However, the reporting model may eventually need to know:
- which rule set/version was used
- perhaps the commit SHA of the rule repository

Design for this possibility without building a rule-management service now.

## Authentication

The API will be called from CI pipelines.

We need some mechanism for authenticating submissions.

Investigate a simple MVP approach, likely API keys/tokens.

Consider:
- how credentials are represented
- how they are transmitted
- how they are stored
- whether tokens should be hashed
- revocation
- configuration
- self-hosted administration

Do not build a full identity/SSO/RBAC platform for the MVP.

But explicitly identify what should be left for later.

## API design

Propose a small, clean HTTP API.

For example, conceptually:

    POST /api/v1/reports

and read/query endpoints for future dashboard use.

Do NOT assume those exact routes are correct.

Design:
- endpoints
- request/response contracts
- HTTP status codes
- validation
- error model
- versioning strategy
- idempotency
- pagination
- filtering
- sorting
- authentication

Keep the API thin.

The API should not perform CodeGuard analysis.

The CLI does the analysis.

The API receives/persists reporting data.

## Project/repository structure

Inspect the current CodeGuard solution and recommend how the Reporting API should be added.

We already have projects such as:

- CodeGuard.Cli
- CodeGuard.Core
- CodeGuard.RuleModel
- CodeGuard.Analysis
- CodeGuard.Evaluation
- CodeGuard.Configuration
- CodeGuard.Reporting
- CodeGuard.Analyzers.*
- tests/...

Determine whether the API should live in this repository or whether a separate repository makes more sense.

Also assess whether the contracts should:
- live in the existing CodeGuard repository
- live in the API repository
- be a separate repository/package

I am currently leaning towards a single CodeGuard monorepo initially, with separate projects/packages, but challenge this if you think it is a mistake.

## Docker

The MVP should produce a Docker image for the API.

Plan:
- Dockerfile
- configuration
- PostgreSQL connection
- migrations
- health/readiness checks
- environment variables
- non-root execution where appropriate
- logging
- development Docker Compose

Do not design Kubernetes deployment yet.

## Observability

Keep this appropriate for an MVP.

Consider:
- structured logging
- request logging
- health checks
- useful API metrics
- correlation/request IDs

Do not introduce a full observability stack.

## Testing

Plan a sensible testing strategy.

I expect:
- contract tests where useful
- API integration tests
- persistence tests
- authentication tests
- idempotency tests
- migration tests
- Docker smoke test if useful

Avoid an excessive number of brittle controller/unit tests if integration testing gives better coverage.

## Security

This API will receive information from company repositories.

Consider:
- authentication
- authorization
- request size limits
- input validation
- avoiding arbitrary file uploads
- SQL injection/EF Core safety
- sensitive information accidentally appearing in violation messages
- HTTPS assumptions
- secrets
- Docker hardening

Again, keep this appropriate for an MVP.

## Deliverable

Produce a detailed implementation/design plan, NOT code.

I want the output to contain:

1. Executive summary
2. Critical architectural review
3. Recommended architecture
4. Project/solution structure
5. Contracts design
6. API endpoint design
7. Persistence/domain model
8. Database schema
9. Indexing/query strategy
10. Authentication approach
11. Idempotency strategy
12. Payload/report design
13. Docker/deployment design
14. Configuration model
15. Testing strategy
16. Security considerations
17. What is explicitly OUT of MVP
18. Future evolution path
19. Incremental implementation plan / PR breakdown

For the PR breakdown, make the PRs independently reviewable and buildable.

I particularly want you to identify:
- what MUST be decided now
- what can safely be deferred
- where we risk over-engineering
- where we risk creating a painful migration later

## Most important constraint

Keep the MVP SMALL.

The purpose of this phase is to get:

    CodeGuard CLI
          ↓
       HTTP API
          ↓
      PostgreSQL
          ↓
    queryable reporting data

running reliably in a Docker container.

The dashboard, Helm charts, Kubernetes deployment, advanced analytics, SSO, RBAC, SaaS/multi-tenancy, rule management, and other platform features can come later.

Before producing the final plan, inspect the existing CodeGuard implementation carefully and reference actual projects/classes/interfaces where relevant. The plan should fit the architecture that actually exists, rather than inventing a generic greenfield architecture.