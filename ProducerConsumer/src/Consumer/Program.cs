using Consumer;
using Core;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((builder, services) =>
    {
        // Add services to the container.
        services.AddConsumer(() => new()
        {
            Options = (options) => 
            {
                options.XConsumers = Environment.ProcessorCount;
                builder.Configuration.Bind(options); 
            },
            ConnectionString = builder.Configuration.GetConnectionString("ProducerConsumer")!
        });

        services.AddHostedService<Worker>();
    })
    .Build();

host.Services.UseProducerConsumer();

await host.RunAsync();
