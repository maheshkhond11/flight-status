# Reflection

## What I'd improve with more time

- **Provider resilience.** The stub providers always answer immediately, so `FlightStatusService`
  has no timeout, retry, or circuit-breaker around `IFlightStatusProvider.GetStatusAsync`. A real
  provider integration over HTTP needs a per-provider timeout (so one slow provider can't stall the
  whole `/flights/status` call past a reasonable SLA) and a small retry policy for transient
  failures, with the existing "log and treat as unavailable" fallback kept as the last resort.
- **Telemetry.** Provider failures are only logged via `ILogger`. There's no metric for how often
  each provider is unavailable, which provider "wins" the merge, or how long a lookup takes —
  exactly the kind of thing an on-call engineer would want a dashboard for once this talks to real
  providers instead of stubs.
- **Richer UI tests.** The Angular tests added in this pass cover component-level behaviour
  (colour mapping, conditional fields, loading spinner, error state) with `TestBed`, but there is no
  end-to-end test that drives the real form → HTTP call → rendered card path in a browser. A small
  Playwright suite covering the five documented scenarios end-to-end would catch integration
  regressions the component tests can't see, e.g. the dev proxy config silently breaking.
- **Accessibility review.** The result card uses `aria-live="polite"` and the error message uses
  `role="alert"`, but there's been no systematic pass with a screen reader or an automated
  accessibility linter (e.g. axe). Colour is also the primary signal for status — the badge already
  repeats the status as text, but a real accessibility pass would check contrast ratios and confirm
  colour is never the *only* signal.
- **Arrival times in the result card.** `FlightStatusResult` carries `scheduledArrivalUtc` /
  `actualArrivalUtc`, but the result card only renders departure times. Not required by the brief,
  but worth adding for completeness, particularly for `Diverted` flights where the arrival airport
  is the whole point.
- **CI pipeline.** There's no GitHub Actions workflow running `dotnet test` / `ng test` on push.
  For a take-home this is a reasonable line to draw, but it's the natural next step.
- **Caching.** Every lookup re-queries both providers even for a flight/date pair just looked up a
  moment ago. A short-lived in-memory cache keyed on `(flightNumber, date)` would cut duplicate
  provider calls without adding real persistence.
- **Containerised delivery.** A Dockerfile was tried and then deliberately removed (see below) —
  worth revisiting once there's a real reason to ship a single deployable artifact rather than two
  local processes, ideally with the image build exercised in CI rather than only reviewed by eye.

## Critical reflection on AI tooling usage

AI was used across the SDLC, not just to generate implementation code: turning the brief into
`spec.md`, drafting the normalisation/merge logic, generating stub data, scaffolding the Angular
components, and drafting unit tests. That sped up the mechanical parts of each layer considerably.
It also introduced real problems that only surfaced under review, which is worth being honest about
rather than glossing over:

- **Broken test scaffolding that looked complete.** Three Angular spec files
  (`flight-result-card.component.spec.ts`, `flight-search.component.spec.ts`, and
  `flight-status-api.service.spec.ts`) imported class names (`FlightResultCard`, `FlightSearch`,
  `FlightStatusApi`) that never matched what the components actually exported
  (`FlightResultCardComponent`, `FlightSearchComponent`, `FlightStatusApiService`). These files
  would not even compile, let alone assert anything meaningful — but at a glance, a `.spec.ts` file
  existing next to every component looked like coverage was in place. The lesson: "a test file
  exists" and "the test suite runs and passes" are different claims, and AI-generated scaffolding
  needs the same scrutiny as AI-generated implementation code, not less.
- **A hardcoded URL that would have broken outside a single local setup.** The Angular API service
  pointed at `https://localhost:7024` instead of a relative URL, which happens to work fine against
  the local `dotnet run` HTTPS profile and therefore looks correct in casual local testing, but
  silently fails the second the app is served from anywhere else (a different dev port, a real
  deployment). This is the kind of thing that AI-generated code gets "locally correct" and a human
  has to catch by checking it against the actual requirement (a same-origin relative call), not just
  against "does it run on my machine."
- **A Docker setup that added risk without being asked for.** A multi-stage Dockerfile was added at
  one point to have the API serve the compiled Angular build from one container. Each piece in
  isolation looked reasonable, but nobody had verified the pieces actually fit together (the initial
  version built the API only and never touched the Angular app at all), and the fix would have made
  "does it run from a clean clone" depend on a multi-stage image build rather than two commands.
  Rather than keep debugging that path, it was removed outright — a reminder that the right response
  to AI-generated scope creep isn't always "fix it," sometimes it's "was this actually needed?"
- **An unreviewed commit message.** One commit in the repository's history is an AI chat response
  that was pasted in verbatim ("Sure! Please provide the list of code change descriptions so I can
  generate the commit message.") instead of an actual summary of the change. It's harmless
  functionally, but it is a visible, easily-avoidable sign of not reading AI output before using it,
  in a context (git history) that's genuinely public and reviewed.
- **What did work well:** the normalisation/merge logic and its test coverage held up under review
  with only small adjustments (the AeroTrack tie-break, the inclusive 15-minute boundary), and the
  AI was useful for surfacing ambiguities in the brief (e.g. asking what should happen on a
  `lastUpdatedUtc` tie) rather than silently picking an interpretation.

The overall takeaway: AI tooling was a genuine accelerant for structure and boilerplate, but every
item above was a case where the generated output was plausible-looking and wrong or incomplete in a
way that only surfaced by actually running it, tracing it against the spec line by line, or reading
the diff rather than trusting the summary of it.
