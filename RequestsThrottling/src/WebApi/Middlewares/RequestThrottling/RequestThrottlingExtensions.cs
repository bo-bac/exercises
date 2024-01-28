namespace WebApi.Middlewares.RequestThrottling;

public static class RequestThrottlingExtensions
{
    public static IServiceCollection AddRequestThrottling(this IServiceCollection services, Action<RequestThrottlingOptions> configure)
    {
        services.Configure(configure);

        return services;
    }

    public static IApplicationBuilder UseRequestThrottling(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        return app.UseMiddleware<RequestThrottlingMiddleware>();
    }
}
