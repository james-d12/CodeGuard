# Orchestration run logs

This folder is the shared memory of the `orchestrator` agent (`.claude/agents/orchestrator.md`).
Every orchestrator run writes one markdown file here recording when it ran, what it was asked
to do, what it accomplished, and anything the next orchestrator should know. A new orchestrator
reads the most recent logs before planning, so this folder is how runs hand off to each other.

Humans can read and edit these files too: a note left here for the next orchestrator is a
legitimate way to steer future runs.

## File naming

```
docs/orchestration/YYYY-MM-DD-HHMM-<slug>.md
```

UTC date and time the run started, then a short kebab-case slug for the requirement, e.g.
`2026-10-14-1830-json-result-provenance.md`. Sorting by filename is sorting by time.

## File format

```markdown
---
started: 2026-10-14T18:30Z
finished: 2026-10-14T19:12Z
status: completed | partial | abandoned | in-progress
branch: feature/<integration-branch>
pr: https://github.com/<owner>/<repo>/pull/<n>   # omit if none
---

# <Requirement in one line>

## Requirement
What the user asked for, restated, with acceptance criteria and explicit exclusions.

## Tasks
| # | Task | Agent | Model | Outcome |
|---|------|-------|-------|---------|
| 1 | ... | task-worker | sonnet | done, merged |
| 2 | ... | merge-conflict-resolver | opus | resolved, 1 spot flagged for review |

## Outcome
What now exists that did not before: files, commands, behaviour. Where it lives.

## Verification
Commands run on the integration branch and their results.

## Decisions and assumptions
Choices made without asking the user, and why.

## Open items
Work deliberately left out, failures not fixed, observations from workers not acted on.

## Notes for the next orchestrator
Anything that would save the next run time: gotchas hit, briefs that worked or did not,
model choices that were wrong, parts of the repo that were harder than expected.
```

The orchestrator creates the file with `status: in-progress` as soon as it has restated the
requirement, and fills in the rest as the run proceeds, so an interrupted run still leaves a
record. The log is committed on the run's integration branch and reaches `main` with the
rest of the work.

## What does not belong here

Design documents, plans and status reports belong in the other `docs/` folders (see
[`docs/README.md`](../README.md)). This folder is a chronological log, not a place for
documents that are updated in place.
