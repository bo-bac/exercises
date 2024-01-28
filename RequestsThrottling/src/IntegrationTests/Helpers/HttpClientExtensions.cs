using System.Diagnostics;

namespace IntegrationTests.Helpers;

internal class HttpResponseMessageWithTiming
{
    internal HttpResponseMessage Response { get; set; } = default!;

    internal TimeSpan Timing { get; set; }
}

internal static class HttpClientExtensions
{
    internal static async Task<HttpResponseMessageWithTiming> GetWithTimingAsync(this HttpClient client, string requestUri)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        HttpResponseMessage response = await client.GetAsync(requestUri);
        TimeSpan timing = stopwatch.Elapsed;

        stopwatch.Stop();

        return new HttpResponseMessageWithTiming
        {
            Response = response,
            Timing = timing
        };
    }
}
