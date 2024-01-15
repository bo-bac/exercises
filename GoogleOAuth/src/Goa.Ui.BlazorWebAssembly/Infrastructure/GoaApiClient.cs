namespace Goa.Ui.BlazorWebAssembly.Infrastructure
{
    public class GoaApiClient
    {
        private readonly HttpClient httpClient;

        public GoaApiClient(HttpClient httpClient, IConfiguration configuration)
        {
            httpClient.BaseAddress = new Uri(configuration["Api:BaseAddress"]!);
            this.httpClient = httpClient;
        }

        public HttpClient Client => httpClient;
    }
}
