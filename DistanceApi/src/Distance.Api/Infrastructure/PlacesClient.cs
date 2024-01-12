using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Distance.Api
{
    public sealed class PlacesClient : IDisposable
    {
        private readonly HttpClient client;

        public PlacesClient(IConfiguration configuration)
        {
            var config = configuration
                .GetSection(nameof(PlacesClient))
                .Get<PlacesClientOptions>();

            client = new HttpClient()
            {
                BaseAddress = new Uri(config.Url),
                DefaultRequestVersion = new Version(1, 1)
            };
        }

        public async Task<Airport> GetAirport(IATA code) 
        {
            try
            {
                return await client.GetFromJsonAsync<Airport>($"airports/{code}");
            }
            catch
            {
                // logging or any other handlind could be here
                return null;
            }
        } 

        public void Dispose() => client.Dispose();
    }
}
