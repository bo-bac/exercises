using HyperLinkDisarmer;

using IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService(options =>
    {
        options.ServiceName = "Resec Disarmer Service";
    })
    .ConfigureServices((hostContext, services) =>
    {
        services.Configure<Options>(hostContext.Configuration.GetSection(nameof(Options)));

        services.AddSingleton<Service>();
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();