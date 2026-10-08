# Engineering Governance

Illustrative organisational standards document for the `Contoso.*` example solution used by
`examples/rules/` - see `examples/docs/architecture-standards.md` for the note on regenerating
fingerprints after an edit.

## Code Ownership

Every repository declares its owners in a `CODEOWNERS` file, in one of the locations GitHub reads it
from: the repository root, `.github/` or `docs/`. A `CODEOWNERS` file anywhere else is ignored by
GitHub and does not count.

## API Documentation

Every API project registers OpenAPI document generation (`builder.Services.AddOpenApi()`), so each
public API publishes an up-to-date OpenAPI specification.

## Testing

Every command must have tests: each command type in `Contoso.Application.Commands` has a
corresponding `{CommandName}Tests` class in the Application test project.
