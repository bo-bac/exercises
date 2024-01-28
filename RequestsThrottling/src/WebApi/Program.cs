using Microsoft.AspNetCore.Mvc;
using WebApi.Domain;
using WebApi.Middlewares.RequestThrottling;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<WeatherForecastService>();
//builder.Services.AddRequestThrottling((options) => { });

var app = builder.Build();
app.UseRequestThrottling();


app.MapGet("/weatherforecast", async ([FromServices] WeatherForecastService service) =>
{
    return await service.GetForecasts();
});

app.Run();

public partial class Program { }