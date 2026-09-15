# C# Conventions

Illustrative organisational standards document for the `Contoso.*` example solution used by
`examples/rules/` - see `examples/docs/architecture-standards.md` for the note on regenerating
fingerprints after an edit.

## Dependency Injection

Dependencies are always injected through the constructor. Resolving a dependency at a call site via
`IServiceProvider.GetService`/`GetRequiredService` (the service-locator pattern) is not permitted
outside the DI extension methods that wire the container up in the first place - it hides a type's
real dependencies from its constructor signature and makes them untestable without a live container.

## Nullable Reference Types

Nullable reference types are enabled project-wide; a type's signature is the source of truth for
whether a reference can be null, not a comment or a runtime check.
