namespace WebApi.Domain;

internal class WeatherForecastService
{
    private readonly static string[] summaries =
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public async Task<IEnumerable<WeatherForecast>> GetForecasts()
    {
        await Task.Delay(500);

        return
            from index in Enumerable.Range(1, 5)
            select new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            );
    }            
}
