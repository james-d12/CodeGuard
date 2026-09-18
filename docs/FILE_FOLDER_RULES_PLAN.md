# File/folder/filename rule-checking expansion — plan

**Status:** implemented and verified (build, full test suite, `rules
validate`/`test`/`analyze` against `examples/rules`, `dotnet format
--verify-no-changes`, and `scripts/sync-skill-references.sh` all pass). One
correction made beyond the original plan: `FileSelector`/`DirectorySelector`'s
default `path` had to change from `"*"` to `"**"` — under the old `*`-crosses-`/`
semantics the default already meant "match anywhere," and leaving it at `"*"`
under the new segment-aware semantics would have silently narrowed it to
"root-level only," breaking every rule that omits `path` (e.g. filtering by
`extension` alone). Caught by the pre-existing `FileSelectorTests` failing after
the `GlobMatcher` change. Single-initiative plan per the `docs/` lifecycle in
`CLAUDE.md` — move to `docs/done/` once this has been reviewed/merged.

## Context

CodeGuard's declarative rules can already target files/directories/the repository
(`kind: file`, `kind: directory`, `kind: repository`) and assert existence
(`must_have_file`, `must_not_have_file`, `must_have_directory`), content
(`must_match_content`, `must_not_match_content`) and a narrow type-vs-filename
convention (`must_match_filename`). This initiative deepens that to fully cover
file-system conventions: precise folder-scoped selection ("files in this folder,
not its subfolders" vs "anywhere under this folder") and filename-shape rules
("everything in `Handlers/` must end in `Handler`"), i.e. treating file/folder
checks as a first-class category alongside the existing C#/architecture checks.

Research across `CodeGuard.Evaluation`, `CodeGuard.Analyzers.Repository`,
`CodeGuard.Configuration.Parsing`, and the docs (`docs/IMPLEMENTATION_STATUS.md`,
`docs/REFACTORING.md` §2.1's "small stable primitive vocabulary" doctrine) found
three concrete gaps and one enabling fix:

1. **`GlobMatcher` is too weak to express location precisely.** It compiles `*` to
   unbounded `.*`, so it already crosses `/` (accidentally "recursive") but has no
   way to express "exactly one path segment" (i.e. "directly inside this folder,
   not nested"). It's `internal` to `CodeGuard.Evaluation` and shared by ~46
   call sites, but only 6 of them pass `/`-containing strings (the file/directory
   selectors and assertions); the other ~40 (namespace, base-type, project,
   package, method/attribute name patterns) never contain `/`, so fixing this is
   safe to do in place, in the one shared class.
2. **Directories are bare `string`s**, not a model — unlike `FileModel`, there's no
   `DirectoryModel`, so a directory candidate can't be name-matched, described in
   failure messages meaningfully, or extended later (child counts, emptiness).
3. **Asymmetries/gaps**: no `must_not_have_directory` (the `must_have_file` /
   `must_not_have_file` pair has no directory equivalent), and no way to check a
   file's *basename* independent of its full path (today every basename check
   requires hand-anchoring a regex against the full relative path).

Per the project's own stated doctrine (`docs/REFACTORING.md` §2.1, cited by
`docs/IMPLEMENTATION_STATUS.md`): extend existing primitives with new optional
parameters rather than add one narrow assertion per condition. The plan below
follows that — no new selector/assertion *kinds* beyond closing the
`must_not_have_directory` parity gap; everything else is new optional parameters
or extending an existing generic assertion (`must_match_name`) to a new candidate
kind it doesn't yet cover.

Two architectural calls were confirmed with the user up front:
- **Fix `GlobMatcher` properly** (`*` = one path segment, `**` = recursive/any
  depth, `?` = one character) rather than leave it as-is. Confirmed low-risk: only
  the 6 file/directory call sites are affected; migration is bounded (see below).
- **Introduce `DirectoryModel`** rather than keep directories as bare strings.

Explicitly **out of scope** for this pass (flagging so it isn't assumed): glob
character classes (`[abc]`) and brace expansion (`{a,b}`), a case-insensitivity
toggle, a configurable excluded-directory list, and extra `FileModel`/`DirectoryModel`
metadata beyond `Name` (size, depth, last-modified, child counts). These can be
follow-ups if wanted later.

## Changes

### 1. `GlobMatcher` — real segment-aware glob semantics

`src/CodeGuard.Evaluation/GlobMatcher.cs`: rewrite `IsMatch` to translate patterns
token-by-token instead of a single blanket `Regex.Escape` + `*`→`.*` replace:
- `**` → matches zero or more path segments (i.e. `(.*)` across `/`, including
  matching nothing so `**/Foo.cs` also matches root-level `Foo.cs`)
- `*` → matches within one segment only (`[^/]*`)
- `?` → one character, not `/` (`[^/]`)
- everything else literal (escaped)

Since no non-path caller's input contains `/`, this is a no-behavior-change for
those ~40 call sites (namespace/base-type/project/package/method/attribute glob
params) — `*` and `**` degrade to the same thing when there's no `/` to be
segment-aware about. Only file/directory `path` params change behavior.

Add test cases to `tests/CodeGuard.Evaluation.Tests/GlobMatcherTests.cs` for `**`
crossing segments, `*` *not* crossing segments, `?`, and a mixed pattern —
alongside the existing non-path cases (keep those, they must still pass
unchanged).

Update the three places documenting the old "`*`-only" limitation, since it stops
being true:
- `CLAUDE.md` (~line 100, "Adding a new selector/assertion" section)
- `docs/IMPLEMENTATION_STATUS.md` (~line 154)
- `docs/HIGH_LEVEL_AI_ASSISTING.md` (~line 248, "Globs support `*` only — no `**`,
  no path semantics")
- `ParameterType.Glob`'s XML doc comment in
  `src/CodeGuard.Configuration/Capabilities/CapabilityDescriptor.cs`

### 2. Migrate existing example rules affected by the semantics change

Audited every `path:`/glob usage in `examples/rules/` (14 occurrences). Most are
single-segment patterns (`*.slnx`, `*.Domain/*`, `*.EventHandler.Application/EventLimiting/*`)
that are unaffected or actually become *more correct* (e.g. `*.Domain/*` now
precisely means "direct children of a `*.Domain` folder," which was already the
intent). Two genuinely break and must migrate to `**`, since their test fixtures
prove multi-segment matching was relied on:
- `examples/rules/repo/golden-launch-port-ranges-001.yml` and
  `golden-launch-launchsettings-required-001.yml`: `path: "*/Properties/launchSettings.json"`
  → `"**/Properties/launchSettings.json"` (fixture: `src/Api/Properties/launchSettings.json`,
  two segments before `Properties`).
- `examples/rules/reporting/skill-reporting-requirements-json-rules-001.yml`:
  `path: "*requirements.json"` → `"**/requirements.json"` (fixture:
  `requirements/requirements.json`).

Migrate precautionarily (same "any project folder, any depth" idiom; currently
only passes because its embedded test fixture happens to use one segment, but the
intent is clearly depth-independent):
- `examples/rules/apphost/skill-apphost-pinned-cosmos-emulator-tag-001.yml`,
  `skill-apphost-internal-http-only-001.yml`: `*/AppHost/Program.cs` → `**/AppHost/Program.cs`
- `examples/rules/eventhandler/skill-eventhandler-telemetry-folder-001.yml`:
  `*/Telemetry/...` → `**/Telemetry/...` (3 occurrences in that file)
- `examples/rules/repo/skill-service-requirements-base-app-json-scope-001.yml`:
  `*/base/app.json` → `**/base/app.json`

After editing, run `dotnet run --project src/CodeGuard.Cli -- rules test --rules-source examples/rules`
and `rules validate` — this is the actual safety net; any pattern missed by this
audit will fail loudly there rather than silently.

### 3. `must_not_have_directory` — close the have/not-have asymmetry

New `src/CodeGuard.Evaluation/Assertions/MustNotHaveDirectoryAssertion.cs`,
mirroring `MustNotHaveFileAssertion.cs`'s shape against `MustHaveDirectoryAssertion.cs`
(same `AppliesTo = [CandidateKind.Repository]`, same `path` param). New parser
`src/CodeGuard.Configuration/Parsing/MustNotHaveDirectoryAssertionParser.cs`
following `MustNotHaveFileAssertionParser.cs`'s exact shape, registered in
`DefaultParsers.cs`'s `CreateAssertionRegistry` next to `must_have_directory`.
Unit tests mirroring `tests/CodeGuard.Evaluation.Tests/Assertions/MustHaveDirectoryAssertionTests.cs`.

### 4. `DirectoryModel`

`src/CodeGuard.Analysis/AnalysisModel/RepositoryModel.cs`: add
```csharp
public sealed record DirectoryModel(string Path, string RelativePath, string Name);
```
and change `RepositoryModel.Directories` from `IReadOnlyList<string>` to
`IReadOnlyList<DirectoryModel>`.

Cascading updates (bounded, all call sites already identified by grep):
- `src/CodeGuard.Analysis/Providers/AnalysisModelBuilderContext.cs`: `_directories`
  becomes `List<DirectoryModel>`, `AddDirectories(IEnumerable<DirectoryModel>)`.
- `src/CodeGuard.Analyzers.Repository/RepositoryFileProvider.cs`:
  `EnumerateDirectories` yields `new DirectoryModel(subdirectory, relativePath, Path.GetFileName(relativePath))`
  instead of a bare relative-path string.
- `src/CodeGuard.Evaluation/Selectors/DirectorySelector.cs`: filter on
  `directory.RelativePath` via `GlobMatcher.IsMatch`; candidates are now
  `DirectoryModel` objects (not strings) — this is what unlocks name-matching on
  directories in step 5.
- `src/CodeGuard.Evaluation/Assertions/MustHaveDirectoryAssertion.cs` and the new
  `MustNotHaveDirectoryAssertion.cs`: match on `d.RelativePath`.
- `src/CodeGuard.Evaluation/Assertions/CandidateDescriptor.cs`: add a
  `DirectoryModel directory => directory.RelativePath` case, matching the existing
  `FileModel` case's shape, so quantifier-assertion (`must_all_match` etc.)
  failure messages on directories are readable.
- `src/CodeGuard.Configuration/Testing/TestSetupBuilder.cs`: `ParseFile`'s sibling
  for directories — keep the embedded-test YAML shape unchanged (still a flat
  `directories: ["src", "tests"]` string array, since none of the 117 existing
  `tests:` blocks need per-directory metadata), just construct
  `DirectoryModel(path, path, Path.GetFileName(path))` per entry instead of
  passing the raw string through.
- Test fixture fixups (already located): `tests/CodeGuard.Evaluation.Tests/Selectors/DirectorySelectorTests.cs`,
  `tests/CodeGuard.Evaluation.Tests/Assertions/MustHaveDirectoryAssertionTests.cs`,
  `tests/CodeGuard.Configuration.Tests/Testing/TestSetupBuilderTests.cs` — change
  `Directories = ["src", ...]` string-collection literals to `DirectoryModel`
  instances (or keep constructing via `TestSetupBuilder`/the `with` expression
  where the test already goes through that path). Also extend
  `tests/CodeGuard.Analyzers.Repository.Tests/RepositoryFileProviderTests.cs`
  (currently doesn't assert on `model.Directories` at all) with a case asserting
  the provider now returns `DirectoryModel`s with correct `Path`/`RelativePath`/`Name`,
  and that excluded directories (`bin`, `.git`, etc.) are still skipped.

### 5. `must_match_name` gains directory support

`src/CodeGuard.Evaluation/Assertions/MustMatchNameAssertion.cs`: add a
`DirectoryModel directory => directory.Name` arm to the existing `switch`
(matches on the directory's own basename, not full path — consistent with how a
"folder must be PascalCase" naming-convention rule should read). This is a purely
additive case (existing `FileModel`/`TypeModel`/etc. arms untouched, so no
behavior change for existing rules). Update
`src/CodeGuard.Configuration/Parsing/MustMatchNameAssertionParser.cs`'s
`AppliesTo` to include `CandidateKind.Directory` and its `Descriptor.Summary` to
mention directories. Add a test case to
`tests/CodeGuard.Evaluation.Tests/Assertions/MustMatchNameAssertionTests.cs`.

This directly enables folder-naming-convention rules, e.g.:
```yaml
target: { kind: repository }
assertions:
  - must_all_match:
      selector: { kind: directory, path: "src/Features/*" }
      assertions:
        - must_match_name: { regex: "^[A-Z][A-Za-z0-9]*$" }
```

### 6. `FileSelector` gains an optional `name` (basename) parameter

`src/CodeGuard.Evaluation/Selectors/FileSelector.cs`: add a third optional
constructor param `string? name = null`, matched via
`GlobMatcher.IsMatch(Path.GetFileName(file.RelativePath), name)` when supplied.
This decouples "where" (`path`, folder-scoped) from "what the filename looks
like" (`name`, basename-only) so a rule reads naturally instead of encoding both
into one path glob:
```yaml
selector:
  kind: file
  path: "**/Handlers/*"
  name: "*Handler.cs"
```
(This is genuinely the user's literal example — "handlers in the Handlers folder
end with Handler" — expressed directly, composed with `must_all_match` the same
way step 5's directory example is.) Update `FileSelectorParser.cs`'s `Descriptor`
to add `ParameterDescriptor.OptionalGlob("name", "File's basename (name +
extension), matched independently of 'path'.")`. Add test cases to
`tests/CodeGuard.Evaluation.Tests/Selectors/FileSelectorTests.cs` covering `name`
alone, `path` + `name` combined, and confirming omitting `name` is unchanged
behavior.

### 7. Capability descriptors, skill sync, and verification

Every new/changed parser (`MustNotHaveDirectoryAssertionParser`,
`FileSelectorParser`'s new `name` param, `MustMatchNameAssertionParser`'s widened
`AppliesTo`) must carry an accurate `CapabilityDescriptor` — `CapabilityCatalogTests`
fails the build otherwise. After implementing, regenerate the skill's reference
tables (`scripts/sync-skill-references.sh`) and confirm it produces no further
diff, since CI enforces this.

## Verification

1. `dotnet build` — 0 errors, 0 warnings.
2. `dotnet test` — all projects pass, including the new/updated cases in
   `CodeGuard.Evaluation.Tests` (GlobMatcher, FileSelector, DirectorySelector,
   MustHaveDirectoryAssertion, MustNotHaveDirectoryAssertion, MustMatchNameAssertion),
   `CodeGuard.Configuration.Tests` (TestSetupBuilder), and
   `CodeGuard.Analyzers.Repository.Tests` (RepositoryFileProvider).
3. `dotnet run --project src/CodeGuard.Cli -- rules validate --rules-source examples/rules`
   and `rules test --rules-source examples/rules` — confirms the migrated rules
   (step 2) still pass and nothing else in the 125-rule set silently broke.
4. `dotnet run --project src/CodeGuard.Cli -- rules discover --format markdown` and
   `scripts/sync-skill-references.sh` — confirm no diff against `skills/codeguard-rule-generation/references/`.
5. `dotnet run --project src/CodeGuard.Cli -- rules analyze --rules-source examples/rules`
   — sanity check the widened `must_match_name` `AppliesTo` didn't flip any rule's
   reachability analysis.
6. Optionally, hand-author one new example rule exercising the new capability
   (folder-scoped filename suffix convention via `must_all_match` + `file`
   selector's `name` param) to confirm the end-to-end authoring experience works,
   though adding it to `examples/rules/` isn't required by the user's request.
