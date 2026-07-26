namespace FlightStatus.Api.Models;

/// <summary>
/// Represents the flight status information returned by the API.
/// </summary>
/// <param name="FlightNumber"></param>
/// <param name="Date"></param>
/// <param name="Status"></param>
/// <param name="ScheduledDepartureUtc"></param>
/// <param name="ActualDepartureUtc"></param>
/// <param name="ScheduledArrivalUtc"></param>
/// <param name="ActualArrivalUtc"></param>
/// <param name="Terminal"></param>
/// <param name="Gate"></param>
/// <param name="DelayReason"></param>
/// <param name="LastUpdatedUtc"></param>
/// <param name="Message"></param>
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
