using FluentAssertions;
using IntegrationTests.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Net;
using WebApi.Middlewares.RequestThrottling;
using Xunit.Abstractions;

namespace IntegrationTests.Tests;

public class RequestThrottlingTests(WebApiAppFactory<Program> factory, ITestOutputHelper output) : IClassFixture<WebApiAppFactory<Program>>
{
    private const int REQUESTS_COUNT = 30;
    private const int MAX_CONCURRENT_REQUESTS_LIMIT = 10;
    private const int MAX_QUEUE_LENGTH = 10;
    private const int TIME_SHORTER_THAN_PROCESSING = 300;
    private const int PROCESSING_TIME = 500;

    private readonly WebApplicationFactory<Program> _factory = factory;
    private readonly ITestOutputHelper _output = output;

    [Theory]
    [InlineData("/weatherforecast")]
    public async Task With_Default_RequestThrottling_Settings_Get_Endpoints_Return_Success_And_Correct_ContentType(string url)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        response.Content.Headers.ContentType?.ToString()
            .Should().Be("application/json; charset=utf-8");
    }
    
    public static TheoryData<(RequestThrottlingOptions options, int expected, string hint)> BaseConfigurations = new(
    [
        (
            new RequestThrottlingOptions
            {
                Limit = MAX_CONCURRENT_REQUESTS_LIMIT,
                LimitExceededPolicy = RequestThrottlingLimitExceededPolicy.Drop
            },
            expected: REQUESTS_COUNT - MAX_CONCURRENT_REQUESTS_LIMIT,
            hint: "drop exceeded"
        ),

        (
            new RequestThrottlingOptions
            {
                Limit = MAX_CONCURRENT_REQUESTS_LIMIT,
                LimitExceededPolicy = RequestThrottlingLimitExceededPolicy.UseQueueDropHead,
                MaxQueueLength = MAX_QUEUE_LENGTH
            },
            expected: REQUESTS_COUNT - MAX_CONCURRENT_REQUESTS_LIMIT - MAX_QUEUE_LENGTH,
            hint: "use queue drop head"
        ),

        (
            new RequestThrottlingOptions
            {
                Limit = MAX_CONCURRENT_REQUESTS_LIMIT,
                LimitExceededPolicy = RequestThrottlingLimitExceededPolicy.UseQueueDropTail,
                MaxQueueLength = MAX_QUEUE_LENGTH
            },
            expected: REQUESTS_COUNT - MAX_CONCURRENT_REQUESTS_LIMIT - MAX_QUEUE_LENGTH,
            hint: "use queue drop tail"
        ),

        (
            new RequestThrottlingOptions
            {
                Limit = MAX_CONCURRENT_REQUESTS_LIMIT,
                LimitExceededPolicy = RequestThrottlingLimitExceededPolicy.UseQueueDropTail,
                MaxQueueLength = MAX_QUEUE_LENGTH,
                MaxTimeInQueue = TIME_SHORTER_THAN_PROCESSING
            },
            expected: REQUESTS_COUNT - MAX_CONCURRENT_REQUESTS_LIMIT,
            hint: "use queue with NOT enough time"
        ),

        (
            new RequestThrottlingOptions
            {
                Limit = MAX_CONCURRENT_REQUESTS_LIMIT,
                LimitExceededPolicy = RequestThrottlingLimitExceededPolicy.UseQueueDropTail,
                MaxQueueLength = REQUESTS_COUNT - MAX_CONCURRENT_REQUESTS_LIMIT,
                MaxTimeInQueue = (int)(PROCESSING_TIME * (REQUESTS_COUNT - MAX_CONCURRENT_REQUESTS_LIMIT) / MAX_CONCURRENT_REQUESTS_LIMIT * 1.05)
            },
            expected: 0,
            hint: "all should be processed"
        )
    ]);

    [Theory]
    [MemberData(nameof(BaseConfigurations))]
    public async Task RequestThrottling_Should_Respect_BaseConfigurations((RequestThrottlingOptions options, int expected, string hint) config)
    {
        // Arrange
        const string url = "/weatherforecast";
        using var host = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddRequestThrottling((o) =>
                {
                    o.Limit = config.options.Limit;
                    o.LimitExceededPolicy = config.options.LimitExceededPolicy;
                    o.MaxQueueLength = config.options.MaxQueueLength;
                    o.MaxTimeInQueue = config.options.MaxTimeInQueue;
                });
            });
        }).Server;

        //Act
        var requests =
            (from _ in Enumerable.Range(1, REQUESTS_COUNT)
             select Task.Run(async () =>
             {
                 using var client = host.CreateClient();
                 return await client.GetWithTimingAsync(url);
             })).ToArray();

        await Task.WhenAll(requests);

        var actual = requests.Select(task => new
        {
            task.Result.Response.StatusCode,
            task.Result.Timing
        }).ToArray();

        var avg = requests
            .GroupBy(r => r.Result.Response.StatusCode)
            .Where(g => g.Key == HttpStatusCode.OK)
            .Select(g => g.Sum(r => r.Result.Timing.Microseconds) / g.Count())
            .FirstOrDefault();

        _output.WriteLine($"{config.hint}");
        _output.WriteLine($"Avg: {avg}ms");

        // Assert
        actual.Count(i => i.StatusCode == HttpStatusCode.TooManyRequests)
            .Should()
            .Be(config.expected);
    }
}