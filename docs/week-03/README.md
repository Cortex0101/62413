# Week 3 Deliverables

This package records the Week 3 implementation of the RatMan WPF presentation shell.

## Contents

- [Week 3 report](week-03-report.md) — objectives, requirements, implementation log, decisions, evidence, retrospective, and handoff.
- [Architecture](architecture.md) — MVVM/component diagrams and responsibility boundaries.
- [Acceptance checklist](acceptance-checklist.md) — automated evidence and repeatable hands-on checks.

## Outcome

Week 3 establishes a navigable WPF shell with a main menu, a playable Canvas prototype, placeholders for future screens, reusable resources, explicit application composition, and testable view models. Gameplay rules remain in `RatMan.Game`; WPF is confined to `RatMan.App`.

## Validation snapshot

- Solution build: passed
- Automated tests: 30 passed, 0 failed, 0 skipped
- Application startup smoke test: passed
- Interactive visual checklist: provided for repeatable execution in Visual Studio
