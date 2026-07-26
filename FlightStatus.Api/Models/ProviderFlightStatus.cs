namespace FlightStatus.Api.Models;

/// <summary>
/// Represents the flight status information provided by a specific provider.
/// </summary>
/// <param name="ProviderName"></param>
/// <param name="FlightNumber"></param>
/// <param name="Date"></param>
/// <param name="RawStatus"></param>
/// <param name="ScheduledDepartureUtc"></param>
/// <param name="ActualDepartureUtc"></param>
/// <param name="ScheduledArrivalUtc"></param>
/// <param name="ActualArrivalUtc"></param>
/// <param name="Terminal"></param>
/// <param name="Gate"></param>
/// <param name="DelayReason"></param>
/// <param name="LastUpdatedUtc"></param>
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
