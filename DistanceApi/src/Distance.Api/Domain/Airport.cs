using System.Text.Json.Serialization;

namespace Distance.Api
{
    public class Airport
    {
        [JsonPropertyName("location")]
        public GeoLocation Location { get; set; }

        public static string UndefinedMessage(IATA code) => $"Airport {code} is undefined";
    }
}
