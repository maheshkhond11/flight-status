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

// Register CORS for Angular
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
builder.Services.AddSingleton<FlightStatusNormalizer>();
builder.Services.AddScoped<FlightStatusService>();
builder.Services.AddSingleton<IFlightStatusProvider, AeroTrackStubProvider>();
builder.Services.AddSingleton<IFlightStatusProvider, QuickFlightStubProvider>();

var app = builder.Build();

//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

// Enable CORS middleware
app.UseCors("AllowAngularDev");

app.MapFlightStatusEndpoints();


app.Run();

public partial class Program;
