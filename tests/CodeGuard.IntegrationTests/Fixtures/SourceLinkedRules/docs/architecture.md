# Architecture Standards

This is a stable, deliberately never-edited fixture file - RuleSourceValidationEndToEndTests.cs
asserts a fingerprint computed from its exact current content. If you need to change it, regenerate
that test's expected fingerprint (run `rules validate --update-fingerprints` against the fixture
rule and copy the new value in).

## Domain Layer

Domain projects must not reference Infrastructure.
