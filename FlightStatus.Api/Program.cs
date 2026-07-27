using System.Text.Json.Serialization;
using FlightStatus.Api.Contracts;
using FlightStatus.Api.Endpoints;
using FlightStatus.Api.Providers;
using FlightStatus.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Register CORS for the Angular dev server (ng serve on :4200), which calls this API directly
// during local development.
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Turns unhandled exceptions into an RFC 7807 Problem Details 500 response
// instead of letting the default developer exception page/raw stack trace out.
builder.Services.AddProblemDetails();

builder.Services.AddSingleton<FlightStatusNormalizer>();

// Singleton: FlightStatusService is stateless per-call and its dependencies
// (normalizer, providers, logger) are all singleton-safe, so there is no
// reason to pay for a new instance per request scope.
builder.Services.AddSingleton<FlightStatusService>();
builder.Services.AddSingleton<IFlightStatusProvider, AeroTrackStubProvider>();
builder.Services.AddSingleton<IFlightStatusProvider, QuickFlightStubProvider>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Converts unhandled exceptions to a Problem Details response instead of
// letting them surface as an unformatted 500.
app.UseExceptionHandler();

// Enable CORS middleware for the Angular dev server
app.UseCors("AllowAngularDev");

app.MapFlightStatusEndpoints();

app.Run();

public partial class Program;
