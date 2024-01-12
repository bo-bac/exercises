using Distance.Api;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using System;


IConfiguration Configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddCommandLine(args)
    .Build();

WebHost    
    .Start(routes => routes
    .MapGet("/", async c => await c.Response.WriteAsync("To measure distance in miles between two airports (IATA code) use GET between/{from}&{to}"))
    .MapGet("between/{from}&{to}", async (req, res, data) =>
    {
        // input validation
        if (!data.Values.TryGetValue("from", out IATA codeFrom))
        {
            await res.BadRequest(IATA.InvalidMessage("from"));
            return;
        }

        if (!data.Values.TryGetValue("to", out IATA codeTo))
        {
            await res.BadRequest(IATA.InvalidMessage("to"));
            return;
        }

        // call external infrastructure
        using var client = new PlacesClient(Configuration);
        var apFrom = await client.GetAirport(codeFrom);
        var apTo = await client.GetAirport(codeTo);

        if (apFrom is null)
        {
            await res.BadRequest(Airport.UndefinedMessage(codeFrom));
            return;
        }

        if (apTo is null)
        {
            await res.BadRequest(Airport.UndefinedMessage(codeTo));
            return;
        }

        // processing
        var distance = new Distance.Api.Distance(apFrom.Location, apTo.Location);

        // format output
        await res.WriteAsync($"{distance.InMiles:F}");
    })
);

Console.ReadKey();
