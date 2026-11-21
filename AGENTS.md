# Island: working rules (read this file only; do not read other docs unless told)
Stack: C# WPF .NET 8. App: src/Island.App. Tests: tests/Island.Tests.

## Scope discipline (most important)
- Do ONLY what the task says. Anything not named in the task is FROZEN: do not edit, restyle, rename or "improve" it. The Header (clock, tab bar, tab icons) is frozen unless named.
- Before editing, state in one line which feature folder and which files you will touch. Touch only those. If you need a file outside the feature folder, say why first. Change shared code (Shell/, Design/, Core/) only when the task is about shared code.
- Never rewrite or revert working code to "clean up".

## Finding code fast
- Read CODEMAP.md first. Open only the folder of the feature named in the task.
- No repo-wide grep/glob. If CODEMAP.md has no answer, search only inside src/ and only the likely folder.
- Never read or index: bin/, obj/, .tools/, docs/, tests snapshots, images, or any .md other than CODEMAP.md and the README of the feature folder you edit.
- If a fix seems to need more than 8 files, stop and ask.

## Verify small
- Build only the app project. Run only tests related to the changed files. Check only the affected tab, with one screenshot. No full audits, stress tests, snapshot suites, or all-tabs screenshots unless the task asks.
- Write no reports (no AUDIT/FIXLOG/PROGRESS docs). Add a 2-line entry to CHANGELOG.md.
- One commit per fix: "fix(<feature>): <what>". Never mix features in one commit.

## Architecture
Each feature folder owns its views, viewmodels, services and models. Features never import each other; they use only Core/, Design/ and Shell/. Keep files under 400 lines.

## Design rules
Tokens only (Design/). The content root padding in Shell is the only outer padding. Tab icons use the icon font. Buttons, labels and other UI chrome never use decorative fonts.