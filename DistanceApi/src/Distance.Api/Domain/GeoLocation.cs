using System.Text.Json.Serialization;

namespace Distance.Api
{
    public class GeoLocation
    {
        [JsonPropertyName("lon")]
        public double Lon { get; set; }
        [JsonPropertyName("lat")]
        public double Lat { get; set; }
    }
}
