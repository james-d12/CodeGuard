# Architecture Standards

Illustrative organisational standards document for the `Contoso.*` example solution used by
`examples/rules/`. A handful of rules link to sections of this file via `metadata.source.file` to
demonstrate `codeguard rules validate`'s documentation-drift checking
(see `docs/done/RULE_SOURCE_AND_LINKED_DOCUMENTATION.md`) - if you edit a section a rule links to,
run `codeguard rules validate --rules-source examples/rules --update-fingerprints` afterwards to
recapture the fingerprint, and check the drift warning was expected before committing.

## Domain Layer

Dependencies must point inward, per the dependency rule: Domain projects must not take a project
reference on an Infrastructure project. Where Domain needs infrastructure behaviour, it depends on
an abstraction defined in Domain, and Infrastructure provides the implementation.

## Application Layer

Application projects may depend on Domain, but never on Infrastructure directly - infrastructure
concerns are wired up at the composition root, not referenced from application/command-handling code.
