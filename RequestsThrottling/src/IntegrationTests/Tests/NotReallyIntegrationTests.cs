using FluentAssertions;
using Microsoft.AspNetCore.TestHost;
using System.Net;
using WebApi.Middlewares.RequestThrottling;

namespace IntegrationTests.Tests;

public class NotReallyIntegrationTests
{
    private const int CONCURRENT_REQUESTS_COUNT = 30;
    private const int MAX_CONCURRENT_REQUESTS_LIMIT = 10;
    private const int MAX_QUEUE_LENGTH = 10;
    private const int TIME_SHORTER_THAN_PROCESSING = 300;

    [Theory]
    [InlineData("/weatherforecast")]
    public async Task Drop_Should_Reject_All_Requests_That_Exceeded_RequestThrottlingOptions_Limit(string url)
    {
        // Arrange
        using var host = await PrepareTestServer((options) =>
        {
            options.Limit = MAX_CONCURRENT_REQUESTS_LIMIT;
            options.LimitExceededPolicy = RequestThrottlingLimitExceededPolicy.Drop;
        });

        //Act
        var requests =
            (from _ in Enumerable.Range(1, CONCURRENT_REQUESTS_COUNT)
             select Task.Run(() =>
             {
                 var client = host.GetTestClient();
                 return client.GetAsync(url);
             })).ToArray();

        await Task.WhenAll(requests);

        var actual = requests.Select(task => new
        {
            task.Result.Headers,
            task.Result.StatusCode
        }).ToArray();

        // Assert
        actual.Count(i => i.StatusCode == HttpStatusCode.TooManyRequests)
            .Should()
            .Be(CONCURRENT_REQUESTS_COUNT - MAX_CONCURRENT_REQUESTS_LIMIT);
    }

    private Task<IHost> PrepareTestServer(Action<RequestThrottlingOptions> configure)
    {
        return new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRequestThrottling(configure);
                    })
                    .Configure(app =>
                    {
                        app.UseRequestThrottling();

                        app.Map("/weatherforecast", (IApplicationBuilder app) =>
                        {
                            app.Run(async context =>
                            {
                                await Task.Delay(500);
                            });
                        });
                    });
            })
            .StartAsync();
    }
}
