# Flight Status Tracker

An offline flight-status lookup feature for SkyRoute. A support agent enters a flight number and a
date; the backend queries two deterministic stub providers, normalises their data into one model,
merges the results, and the Angular UI displays a single unified status.

Full behavioural contract: see [`spec.md`](./spec.md).

- **Backend:** ASP.NET Core Minimal API (.NET 10)
- **Frontend:** Angular (standalone components, signals)

---

## Architecture

```
  browser  ───▶  ng serve :4200  ───(proxy.conf.json)───▶  FlightStatus.Api :5094
                                                              │
                                                              ▼
                                                      IFlightStatusProvider (DI)
                                                        ├─ AeroTrackStubProvider
                                                        └─ QuickFlightStubProvider
```

- `FlightStatusEndpoints` validates the request and delegates to `FlightStatusService`.
- `FlightStatusService` invokes both `IFlightStatusProvider` implementations concurrently, discards
  failures/unusable results, and applies the merge rule (latest `lastUpdatedUtc` wins, AeroTrack
  breaks ties, single-provider and no-provider fallbacks).
- `FlightStatusNormalizer` turns a provider's raw vocabulary and timings into the unified
  `FlightStatus` enum, independent of which provider produced it.
- Providers are plain DI-registered classes behind `IFlightStatusProvider`, so a real provider
  integration could replace a stub without touching the service or normalizer.
- The Angular dev server and the API run as two separate local processes, connected by a dev proxy
  (see below) — there is no containerisation or combined single-process deployment in this repo.

---

## Prerequisites

- .NET 10 SDK
- Node.js (LTS) + npm

---

## Running locally

**Backend:**

```bash
cd FlightStatus.Api
dotnet restore
dotnet run
```

Runs on `http://localhost:5094` (see `Properties/launchSettings.json`). Swagger UI is available at
`/swagger` in the `Development` environment only.

**Frontend:**

```bash
cd flight-status-ui
npm install
npm start
```

Runs on `http://localhost:4200`. `ng serve` uses `proxy.conf.json`, which forwards `/flights/*`
requests to `http://localhost:5094`, so the UI calls a relative `/flights/status` URL rather than a
hardcoded host — both processes need to be running (API first) for the app to work end to end.

---

## Tests

**Backend (xUnit):**

```bash
cd FlightStatus.Tests
dotnet test
```

Covers: status normalisation (both providers' vocabularies, the 15/16-minute delay boundary,
cancellation/diversion precedence), the merge service (latest-timestamp win, the deterministic
AeroTrack tie-break, single-provider results, no-provider results, one provider throwing), and the
`/flights/status` endpoint (all five deterministic scenarios, missing/invalid query parameters).

**Frontend (Angular / vitest):**

```bash
cd flight-status-ui
npm test
```

Covers: result-card status-colour mapping for all five statuses, conditional rendering of
AeroTrack-only fields (terminal/gate/delay reason shown only when present), the search form's
validation and loading/spinner state, the API service's use of the relative URL, and the
application-level API error state.

---

## Deterministic stub scenarios

Every scenario is fixed to the requested `date` with hardcoded UTC times — nothing depends on the
system clock or randomness. Flight-number matching is case-insensitive and trims whitespace.

| Flight number | AeroTrack | QuickFlight | Unified result | Why |
| --- | --- | --- | --- | --- |
| `SR100` | `ON_TIME`, 10-min actual delay, terminal `1`, gate `A12`, updated 09:30Z | `LATE`, updated 09:10Z | `OnTime` | AeroTrack wins (newer `lastUpdatedUtc`); its own timing is inside the 15-min threshold |
| `SR200` | `ON_TIME`, 10-min actual delay, updated 09:10Z | `LATE`, updated 09:35Z | `Delayed` | QuickFlight wins (newer `lastUpdatedUtc`). AeroTrack's own data is genuinely `OnTime` — this is a timestamp-recency win, not a normalisation conflict |
| `SR300` | `CANCELLED`, updated 09:20Z | no result | `Cancelled` | Only AeroTrack responds |
| `SR400` | no result | `REROUTED`, updated 09:20Z | `Diverted` | Only QuickFlight responds |
| `SR999` | no result | no result | `Unknown` with message "No usable status was returned by either provider." | Neither provider has this flight |

Any other flight number returns no result from either stub.

---

## Assumptions & trade-offs

- No persistence, authentication, or real provider integrations — this is explicitly out of scope
  (see `spec.md` section 2). `IFlightStatusProvider` is the seam where a real integration would
  plug in later.
- The API validates `flightNumber` and `date` shape only (non-empty, `yyyy-MM-dd`); it does not
  validate that a flight number is a real IATA/ICAO code, since the providers are closed stubs with
  a fixed vocabulary of flight numbers.
- `FlightStatusService` is registered as a singleton because it and its dependencies are stateless
  per call — see `reflection.md` for what would need to change (timeouts, telemetry) before that
  remains true against real, potentially slow/stateful provider clients.
- CORS is configured for `http://localhost:4200` to support running the Angular dev server against
  a locally-running API.
- The frontend does not retry failed requests automatically; a failed lookup shows a single error
  state and the agent re-submits manually.
- The stub API responds effectively instantly, so `App.lookup()` holds every response (success or
  error) back by a fixed 1.5s before clearing the loading state, purely so the spinner is visibly
  on screen. This is a deliberate UI affordance, not simulated network latency, and would be removed
  once a real provider integration supplies its own natural response time.

---

## Project structure

```
flight-status/
├── README.md
├── spec.md                # data models and interface contracts (committed before implementation)
├── prompts.md             # AI prompts used, with notes on decisions
├── reflection.md          # what would be improved with more time
├── FlightStatus.Api/
│   ├── Contracts/         # IFlightStatusProvider
│   ├── Models/             # FlightStatus enum, FlightStatusResult, ProviderFlightStatus
│   ├── Providers/           # AeroTrackStubProvider, QuickFlightStubProvider
│   ├── Services/            # FlightStatusNormalizer, FlightStatusService
│   ├── Endpoints/            # FlightStatusEndpoints
│   └── Program.cs
├── FlightStatus.Tests/     # xUnit tests
└── flight-status-ui/       # Angular app
```
