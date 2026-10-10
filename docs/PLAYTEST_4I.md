# The 4i external playtest

The playtest of 4i-D's release builds (decision D9): 3–5 friends and family on a restricted itch.io page, feedback through the questionnaire (`docs/tester-kit/questionnaire.md`). Started 2026-10-09.

## Builds

| Tag | Web | Windows (IL2CPP) | Notes |
|---|---|---|---|
| `playtest-0.4i.1` (`0032be21`) | `0.4i-d.395` | `0.4i-d.394` (built from `132c0075`; only docs differ) | the first posted build |

Builds come from **`release/0.4i`**, which started at `playtest-0.4i.1`; `main` moves on to Phase 5. Each re-upload is tagged `playtest-0.4i.<n>` on the release branch, and its version says so: the release branch's `VersionStamp.Milestone` becomes `0.4i.<n>` at its first fix, so its stamps read `0.4i.<n>.<commits>+<hash>` (the stamp's pattern takes this form from that first fix on).

**Testers' saves:** the saves the playtest build itself wrote are fixtures (`Tests/EditMode/Fixtures/Saves/v10_tester-0.4i.1_*.json`, loaded by `SaveCompatibilityTests` and continued by `SaveContinueTests`), so every later build, on either branch, still loads them. Each new playtest build adds its own.

## How feedback is sorted

- **Blocking:** a crash, lost progress, a softlock, or something most testers can't get past. Fixed on `main` first, then cherry-picked to `release/0.4i`, rebuilt from the release branch with the next patch version, and handed to the owner to re-upload.
- **Phase 5 input:** a real finding that belongs to a Phase 5 milestone (named). Each Phase 5 plan from 5b on opens by reading this section.
- **Later:** worth keeping, not for Phase 5's current shape (polish, a wish, a question for a later phase).

The owner pastes the answers; each item is logged with the tester, the version and the device, and sorted. A tester's words are quoted only where the wording matters.

## Round 1

_Waiting for the first answers._

| # | Tester | Version | Device | What they found | Sort | Status |
|---|---|---|---|---|---|---|

### Blocking (round 1)

_None yet._

## Phase 5 input

Read by every Phase 5 milestone plan from 5b on.

| Milestone | From | The finding | What it suggests |
|---|---|---|---|

## Later

| From | The finding |
|---|---|

## Sign-off and the milestone tag

When round 1's blocking fixes are in and the owner signs off, `milestone-4i` is tagged on `release/0.4i` and the 4i closeout docs are written. Testing can go on after the tag; later rounds are logged here as rounds 2, 3 and on.
