namespace FlightStatus.Api.Models;

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
