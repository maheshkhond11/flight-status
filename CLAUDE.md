# Repository guide for AI coding assistants

This file orients any AI coding assistant (Claude Code, GitHub Copilot, Cursor, etc.) working in
this repository. Read `spec.md` first — it is the authoritative contract for the domain model,
normalisation rules, merge rules, and HTTP API. This file is about conventions and process, not the
domain rules themselves.

## Project shape

- `FlightStatus.Api/` — ASP.NET Core Minimal API (.NET). `Contracts/` defines
  `IFlightStatusProvider`; `Providers/` holds the two deterministic stub implementations;
  `Services/` holds the normaliser and the merge service; `Endpoints/` maps the HTTP surface.
- `FlightStatus.Tests/` — xUnit tests for the normaliser, the merge service, and the endpoint.
- `flight-status-ui/` — Angular (standalone components, signals, native control flow). Its own
  conventions live in `flight-status-ui/.github/copilot-instructions.md` — follow those for any
  Angular-specific work.

## Conventions that apply repo-wide

- Providers are stubs by design (see `spec.md` section 2: no real flight APIs, no network calls).
  Do not wire up real HTTP calls to third-party services; extend `IFlightStatusProvider` instead.
- Keep provider-specific vocabulary and shape out of `FlightStatusService` — normalisation belongs
  in `FlightStatusNormalizer` only, so the merge logic never has to know which provider said what.
- The Angular app must call the API via a relative URL (`/flights/status`), never a hardcoded host.
  Local dev relies on `flight-status-ui/proxy.conf.json` to proxy that relative URL to the running
  API. There is no Docker/container packaging in this repo — both apps run as two local processes.
- Any change to the merge/normalisation rules must come with a unit test that would fail without
  the change — this codebase treats "the deterministic scenarios in spec.md section 8 still pass"
  as a regression gate, not a nice-to-have.
- Don't commit `wwwroot/`, `node_modules/`, `bin/`, `obj/`, or `dist/` — they're already
  gitignored; if a build tool starts writing into a new generated folder, add it to `.gitignore`
  rather than committing it.

## Working with AI on this repo

- Record significant prompts, the judgement calls made about the output, and how the output was
  verified in `prompts.md` — not routine one-line prompts, only the ones that shaped a design or
  correctness decision.
- Keep `reflection.md` honest: it should name real gaps (including ones AI-assisted work introduced
  and a human had to catch), not just list generic "future work" bullets.
- Before trusting generated tests as coverage, actually run them. This repo has already shipped
  spec files that referenced component names that didn't exist and would not compile — "a test file
  exists" is not the same claim as "the test suite runs and passes."
