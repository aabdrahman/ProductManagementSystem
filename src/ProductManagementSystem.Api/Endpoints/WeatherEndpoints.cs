using Serilog;
using System.Text.Json;

namespace ProductManagementSystem.Api.Endpoints;

public static class WeatherEndpoints
{
    private static string[] summaries =
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };
    public static RouteGroupBuilder MapWeatherEndpoints(this WebApplication app)
    {
        var endPointRoutes = app.MapGroup("Weather");


        endPointRoutes.MapGet("/weatherforecast", () =>
        {
            var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                (
                    DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    Random.Shared.Next(-20, 55),
                    summaries[Random.Shared.Next(summaries.Length)]
                ))
                .ToArray();
            Log
            .ForContext("MethodName", "GetForecasts")
            .ForContext("ClassName", "Program")
            .Information($"{JsonSerializer.Serialize(forecast)}");
            return forecast;
        });

        endPointRoutes.MapGet("/", () =>
        {
            return $"Hello World!!!From {Environment.MachineName}";
        });


        return endPointRoutes;
    }
}




internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
