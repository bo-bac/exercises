using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Core;

public static class StartupExtensions
{
    public static IServiceCollection AddConsumer(this IServiceCollection services, Func<(Action<Options> Options, string ConnectionString)> configure)
    {
        var config = configure();
        services.Configure(config.Options);

        services.AddDbContextFactory<Db>(options =>
            options
                .UseSqlServer(config.ConnectionString, x => x.UseDateOnlyTimeOnly())
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.AddSingleton<IConsumer, Consumer>();

        return services;
    }

    public static IServiceCollection AddProducer(this IServiceCollection services, Func<(Action<Options> Options, string ConnectionString)> configure)
    {
        var config = configure();
        services.Configure(config.Options);
        
        services.AddDbContext<Db>(options => options
            .UseSqlServer(config.ConnectionString, x => x.UseDateOnlyTimeOnly())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

        services.AddScoped<IProducer, Producer>();

        return services;
    }

    public static void UseProducerConsumer(this IServiceProvider serviceProvider)
    {
        //var scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        using var scope = serviceProvider.CreateScope();
        using var context = scope.ServiceProvider.GetRequiredService<Db>();
        context.Database.EnsureCreated();
    }
}
