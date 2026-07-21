# Flight Status Tracker - Technical Specification

## 1. Purpose

Build an offline flight-status lookup feature for SkyRoute. A support agent enters a flight number and a date. The backend queries two deterministic stub providers, normalizes provider-specific data into one model, applies the merge rules, and returns a single flight status for the Angular UI to display.

## 2. Scope and constraints

- Backend: .NET 8 Minimal API written in C#.
- Frontend: Angular standalone-component application.
- Providers are local deterministic stubs only. No real flight API, credentials, authentication, database, or persistence is included.
- The running application must make no external network calls.
- The UI and API are independently runnable during development. The production Docker image serves the compiled Angular files from the .NET application.

## 3. Unified domain model

### 3.1 Status enum

```csharp
public enum FlightStatus
{
    OnTime,
    Delayed,
    Cancelled,
    Diverted,
    Unknown
}
```

| Status | Definition |
| --- | --- |
| `OnTime` | Departure or arrival is within 15 minutes of its scheduled time. |
| `Delayed` | Departure or arrival is more than 15 minutes later than its scheduled time. |
| `Cancelled` | The flight will not operate. |
| `Diverted` | The flight landed or will land at a different airport. |
| `Unknown` | Neither provider supplied a usable status. |

### 3.2 API response model

All timestamps are UTC ISO-8601 values. Fields that a provider does not supply are `null` and are omitted from the UI.

```csharp
public sealed record FlightStatusResult(
    string FlightNumber,
    DateOnly Date,
    FlightStatus Status,
    DateTimeOffset? ScheduledDepartureUtc,
    DateTimeOffset? ActualDepartureUtc,
    DateTimeOffset? ScheduledArrivalUtc,
    DateTimeOffset? ActualArrivalUtc,
    string? Terminal,
    string? Gate,
    string? DelayReason,
    DateTimeOffset? LastUpdatedUtc,
    string? Message);
```

Example successful response:

```json
{
  "flightNumber": "SR100",
  "date": "2026-07-21",
  "status": "OnTime",
  "scheduledDepartureUtc": "2026-07-21T10:00:00+00:00",
  "actualDepartureUtc": "2026-07-21T10:10:00+00:00",
  "terminal": "1",
  "gate": "A12",
  "delayReason": null,
  "lastUpdatedUtc": "2026-07-21T09:30:00+00:00",
  "message": null
}
```

## 4. Provider contract

```csharp
public interface IFlightStatusProvider
{
    string Name { get; }

    Task<ProviderFlightStatus?> GetStatusAsync(
        string flightNumber,
        DateOnly date,
        CancellationToken cancellationToken);
}
```

`ProviderFlightStatus` is an internal transport model. It retains the provider's raw status text plus any available scheduled/actual times and optional operational details.

```csharp
public sealed record ProviderFlightStatus(
    string ProviderName,
    string FlightNumber,
    DateOnly Date,
    string? RawStatus,
    DateTimeOffset? ScheduledDepartureUtc,
    DateTimeOffset? ActualDepartureUtc,
    DateTimeOffset? ScheduledArrivalUtc,
    DateTimeOffset? ActualArrivalUtc,
    string? Terminal,
    string? Gate,
    string? DelayReason,
    DateTimeOffset LastUpdatedUtc);
```

### 4.1 Provider capabilities

| Provider | Data supplied | Stub vocabulary |
| --- | --- | --- |
| `AeroTrack` | Status, scheduled and actual times, terminal, gate, delay reason, last update time | `ON_TIME`, `DELAYED`, `CANCELLED`, `DIVERTED` |
| `QuickFlight` | Status, scheduled times, last update time | `SCHEDULED`, `LATE`, `CXL`, `REROUTED` |

Returning `null` means the provider has no result for that lookup. A provider exception is logged and treated as unavailable so that the other provider can still produce a result.

## 5. Normalization rules

Normalization converts a `ProviderFlightStatus` into the unified status model.

1. An explicit cancellation raw status maps to `Cancelled`.
2. An explicit diversion raw status maps to `Diverted`.
3. If an actual departure or arrival is more than 15 minutes later than its scheduled equivalent, map to `Delayed`.
4. If at least one actual/scheduled pair exists and no pair is more than 15 minutes late, map to `OnTime`.
5. If timings cannot decide the result, map known provider vocabulary: `DELAYED` and `LATE` to `Delayed`; `ON_TIME` and `SCHEDULED` to `OnTime`; `CANCELLED` and `CXL` to `Cancelled`; `DIVERTED` and `REROUTED` to `Diverted`.
6. If there is no usable timing or recognized raw status, the response is unusable for merging.

The 15-minute boundary is inclusive: exactly 15 minutes late is `OnTime`; 16 minutes late is `Delayed`.

## 6. Merge rules

1. Invoke AeroTrack and QuickFlight concurrently.
2. Normalize each non-null provider response.
3. Discard a response that has no usable normalized status.
4. If one usable response remains, return it.
5. If two usable responses remain, return the complete response with the later `lastUpdatedUtc` value.
6. If their `lastUpdatedUtc` values are identical, select AeroTrack deterministically.
7. Do not combine fields from two responses. The winning provider supplies the complete result, so AeroTrack-only fields are displayed only when AeroTrack wins and supplies them.
8. If no usable response remains, return `Unknown` with the message: `No usable status was returned by either provider.`

## 7. HTTP API

### 7.1 Lookup endpoint

```http
GET /flights/status?flightNumber={code}&date={yyyy-MM-dd}
```

| Condition | HTTP status | Response |
| --- | --- | --- |
| Valid lookup with a usable provider result | `200 OK` | `FlightStatusResult` |
| Valid lookup with no usable provider result | `200 OK` | `FlightStatusResult` with `Unknown` and a clear message |
| Missing or whitespace `flightNumber` | `400 Bad Request` | Clear validation error |
| Missing, invalid, or non-ISO `date` | `400 Bad Request` | Clear validation error |
| Unexpected unhandled server failure | `500 Internal Server Error` | Problem Details response |

The API accepts and returns JSON. Flight-number matching in the stubs is case-insensitive and ignores leading/trailing whitespace.

## 8. Deterministic stub scenarios

Each scenario uses the supplied `date` with fixed UTC clock times. No scenario depends on the current clock or randomness.

| Flight number | AeroTrack | QuickFlight | Expected unified result |
| --- | --- | --- | --- |
| `SR100` | `ON_TIME`; 10-minute actual departure delay; terminal `1`; gate `A12`; update 09:30Z | `LATE`; update 09:10Z | `OnTime`; AeroTrack wins because it is newer |
| `SR200` | `ON_TIME`; update 09:10Z | `LATE`; update 09:35Z | `Delayed`; QuickFlight wins because it is newer |
| `SR300` | `CANCELLED`; update 09:20Z | No result | `Cancelled` |
| `SR400` | No result | `REROUTED`; update 09:20Z | `Diverted` |
| `SR999` | No result | No result | `Unknown` with the specified message |

Any other flight number returns no result from both providers.

## 9. Angular UI behavior

- Display a search form with required flight-number and date inputs.
- Do not issue a request until both inputs are valid.
- Display an initial empty state before the first search.
- Disable the submit button and show a loading indication while the request is pending.
- Display the unified result in a card.
- Apply status colours: green for `OnTime`, amber for `Delayed`, red for `Cancelled` and `Diverted`, grey for `Unknown`.
- Render gate, terminal, and delay reason only if the returned values are non-empty.
- Display a clear error state for non-success API responses or network errors.
- The frontend calls `/flights/status` using a relative URL. This works when the Angular build is served by the API container.

## 10. Test requirements

Unit tests must cover:

- AeroTrack and QuickFlight vocabulary normalization.
- The 15-minute and 16-minute delay boundaries.
- Cancellation and diversion precedence.
- Latest `lastUpdatedUtc` winning when both responses are usable.
- The deterministic AeroTrack tie-breaker.
- A single provider result.
- Neither provider returning a result.
- A provider failure while the other provider succeeds.
- Missing and invalid endpoint query parameters.

Angular tests must cover the result-card colour mapping, conditional AeroTrack-only fields, and API error state.

## 11. Delivery requirements

- `spec.md` is committed before implementation files.
- `README.md` documents prerequisites, local development, tests, Docker build/run commands, and the scenario table.
- `prompts.md` records only real significant AI prompts, their output/use, verification performed, and resulting engineering decisions.
- `reflection.md` describes genuine future improvements, such as provider timeouts/retries, telemetry, richer UI tests, and accessibility review.
- No secrets, credentials, generated dependency folders, or build artifacts are committed.
