namespace FlightStatus.Api.Models;

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
