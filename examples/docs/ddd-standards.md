# DDD Standards

Illustrative organisational standards document for the `Contoso.*` example solution used by
`examples/rules/` - see `examples/docs/architecture-standards.md` for the note on regenerating
fingerprints after an edit.

## Entities

Every domain entity inherits from the shared `Contoso.Domain.Entity<TId>` base class rather than
defining its own identity/equality handling. This keeps identity semantics consistent across the
domain model and lets tooling (including CodeGuard's own DDD rules) recognise entities structurally.

## Aggregates

An aggregate root is the only entity in its aggregate that outside code may hold a reference to;
everything else in the aggregate is reached through it.

## Domain Events

Every domain event must have at least one handler implementing
`Contoso.Application.IDomainEventHandler<TEvent>` for it. An event nothing handles is either dead
code or a missing reaction to something the domain said happened.
