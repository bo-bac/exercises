using Core;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProducer(() => new()
{
    Options = (options) =>
    {
        options.XPublishers = Environment.ProcessorCount;
        builder.Configuration.Bind(options);
    },
    ConnectionString = builder.Configuration.GetConnectionString("ProducerConsumer")!
});

var app = builder.Build();

app.Services.UseProducerConsumer();

app.MapGet("/ping", () =>
{
    return "pong";
});

app.MapGet("/hashes", ([FromServices] IProducer producer) =>
{
    return producer.GetProducedSummary();
});

app.MapPost("/hashes", async([FromServices] IProducer producer) =>
{
    await producer.Produce();
});

app.Run();