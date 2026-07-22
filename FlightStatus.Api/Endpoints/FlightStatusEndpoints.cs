using System.Globalization;
using FlightStatus.Api.Services;

namespace FlightStatus.Api.Endpoints;

public static class FlightStatusEndpoints
{
    public static IEndpointRouteBuilder MapFlightStatusEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/flights/status", async (
            string? flightNumber,
            string? date,
            FlightStatusService flightStatusService,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(flightNumber))
            {
                return Results.BadRequest(new { error = "flightNumber is required." });
            }

            if (string.IsNullOrWhiteSpace(date))
            {
                return Results.BadRequest(new { error = "date is required and must use yyyy-MM-dd format." });
            }

            if (!DateOnly.TryParseExact(
                    date,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var requestedDate))
            {
                return Results.BadRequest(new { error = "date must use yyyy-MM-dd format." });
            }

            var result = await flightStatusService.GetStatusAsync(flightNumber, requestedDate, cancellationToken);
            return Results.Ok(result);
        })
        .WithName("GetFlightStatus")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        return endpoints;
    }
}
