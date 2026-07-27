# AI Prompts & Decisions Log

This log records the significant AI prompts used across the SDLC for this challenge, what came out
of them, how the output was checked, and the engineering call that followed. It is not a full chat
transcript — routine prompts ("add a null check here", "rename this variable") are omitted; only
prompts that shaped design, correctness, or a judgement call are kept.

An AI coding assistant (Claude) was used as the IDE-integrated tool throughout, alongside manual
review of every generated diff before it was accepted.

## 1. Analysis & spec

**Prompt (paraphrased):** "Turn this challenge brief into a spec.md with a concrete unified domain
model, provider contract, normalisation rules, and merge rules — call out anything the brief leaves
ambiguous."

**Output/use:** A first draft of `spec.md`, including the `FlightStatus` enum, the
`FlightStatusResult`/`ProviderFlightStatus` records, and a normalisation/merge algorithm.

**Verification:** Read every rule against the brief line by line. Two ambiguities the AI had
flagged were resolved manually, not left to the AI: (1) the brief's merge rule only says "prefer
the later `lastUpdatedUtc`" with no tie-break — I decided the tie-break should be deterministic
(AeroTrack wins) rather than arbitrary/random, since the spec explicitly requires deterministic
stubs; (2) the 15-minute boundary's inclusivity ("within 15 minutes" vs "beyond 15 minutes") was
made explicit as inclusive-OnTime, exclusive-Delayed, with a unit test at exactly 15 and 16 minutes
to lock that decision in.

**Decision:** `spec.md` was committed before any implementation file, per the brief's Definition of
Done.

## 2. Normalisation & merge logic

**Prompt (paraphrased):** "Implement `FlightStatusNormalizer` and `FlightStatusService` per
spec.md sections 5–6: concurrent provider calls, provider failure isolation, latest-timestamp-wins
merge, AeroTrack tie-break, no field-mixing between providers."

**Output/use:** The bulk of `FlightStatusNormalizer.ResolveStatus` and
`FlightStatusService.GetStatusAsync`/`GetUsableResponseAsync`.

**Verification:** Wrote/ran the boundary, precedence, and merge unit tests in
`FlightStatusNormalizerTests` and `FlightStatusServiceTests` by hand against the generated code
rather than asking the AI to also generate the tests it would need to pass. One test in particular
(`GetStatusAsync_ProviderFailsButOtherProviderSucceeds_ReturnsSuccessfulProviderResponse`) exists
specifically to pin down that a provider throwing does not take down the other provider's result,
which the generated `try/catch` in `GetUsableResponseAsync` needed to satisfy.

**Decision:** Kept the AI's suggestion to log provider failures as warnings rather than swallow them
silently, since a support agent debugging a stuck lookup needs that signal.

## 3. Stub provider data

**Prompt (paraphrased):** "Generate deterministic AeroTrack/QuickFlight stub data for SR100–SR999
matching the scenario table in spec.md section 8."

**Output/use:** `AeroTrackStubProvider` and `QuickFlightStubProvider`.

**Verification:** Manually traced each scenario through the normaliser and merge logic by hand to
confirm the expected unified result, because a subtle case existed: for `SR200`, AeroTrack's own
data normalises to `OnTime` (its 10-minute actual delay is inside the 15-minute threshold) — the
scenario only produces `Delayed` because QuickFlight's `lastUpdatedUtc` is later. That's correct per
the merge rule, but it was worth confirming by hand that it wasn't accidentally masking a
normalisation bug rather than genuinely exercising the recency tie-break. A comment and a dedicated
endpoint test (`GetStatus_SR200_QuickFlightWinsOnRecencyAndReportsDelayed`) were added afterwards so
a future change can't silently break this without a test failing.

## 4. Angular UI

**Prompt (paraphrased):** "Build the search form, result card, and app shell per spec.md section 9:
required inputs before submit, loading state, colour-coded result card, conditional AeroTrack-only
fields, error state."

**Output/use:** `FlightSearchComponent`, `FlightResultCardComponent`, `App`.

**Verification:** Manually checked the colour-class mapping against the spec's stated colours
(green/amber/red/red/grey) and clicked through each of the five states in the browser during
development.

**Decision:** Rejected the AI's first pass at the loading state, which only disabled the submit
button with a text swap — added a visible spinner element separately, since "disabled + different
label" is not a strong enough loading signal for a support-agent tool used under time pressure.

## 5. Tests

**Prompt (paraphrased):** "Write unit tests for the normaliser boundary conditions, the merge
service's tie-break and failure-isolation paths, and the endpoint's validation errors."

**Output/use:** Most of `FlightStatusNormalizerTests`, `FlightStatusServiceTests`, and
`FlightStatusEndpointTests`.

**Verification:** Ran `dotnet test` locally after every batch of generated tests and read each
assertion to confirm it actually pins the behaviour described (not just "does not throw"). Rejected
a couple of AI-suggested assertions that only checked `result is not null` — replaced them with
assertions on the specific `Status`/field values the spec cares about.

## 6. Feedback remediation pass

After the second-round review, feedback flagged several concrete gaps. Rather than re-explaining
each one here, the fixes and the reasoning behind them are recorded directly in `reflection.md` and
in code comments/commit messages at the point of the fix (e.g. the DI-lifetime change, the
exception-handler middleware, and the previously-broken Angular spec files that referenced
component names that no longer existed). Each fix was checked by re-reading the affected file
end-to-end and, where the environment allowed it, by running the relevant test suite; two frontend
spec files (`flight-result-card.component.spec.ts`, `flight-search.component.spec.ts`) had been
silently failing to compile because they imported class names (`FlightResultCard`, `FlightSearch`)
that never matched the actual exported class names (`FlightResultCardComponent`,
`FlightSearchComponent`) — a reminder that "tests exist" is not the same as "tests run", and that
AI-generated scaffolding needs the same scrutiny as AI-generated implementation code.

## 7. Dropping Docker packaging

As part of the same remediation pass, a multi-stage Dockerfile was initially added so one container
image would build the Angular app and serve it from the API's `wwwroot`. On reflection this added
real risk for no real benefit for this challenge: it makes the "does it run from a clean clone"
check depend on a Docker build (Node + .NET SDK images, an `npm ci` inside the image) rather than on
two commands the interviewer can run directly, and the demo/live-change-tasks setting favours the
simpler, more transparent path. Removed `Dockerfile`, `.dockerignore`, the Docker-only launch
profile, the Docker tooling package reference, and the static-file/SPA-fallback middleware that only
existed to serve a Docker-built `wwwroot`. `spec.md` and the README were updated to match — the two
processes (`dotnet run`, `ng serve` with its dev proxy) are now the only documented way to run the
app, in dev and for the demo alike.
