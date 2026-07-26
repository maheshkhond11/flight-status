namespace FlightStatus.Api.Models;

/// <summary>
/// Represents the possible flight statuses.
/// </summary>
public enum FlightStatus
{
    /// <summary>
    /// The flight is on time.
    /// </summary>
    OnTime,

    /// <summary>
    /// The flight is delayed.
    /// </summary>
    Delayed,

    /// <summary>
    /// The flight has been cancelled.
    /// </summary>
    Cancelled,

    /// <summary>
    /// The flight has been diverted to a different airport.
    /// </summary>
    Diverted,

    /// <summary>
    /// The flight status is unknown or could not be determined.
    /// </summary>
    Unknown
}
