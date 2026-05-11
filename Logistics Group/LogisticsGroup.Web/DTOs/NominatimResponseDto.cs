using System.Text.Json.Serialization;

namespace LogisticsGroup.Web.DTOs
{
    public class NominatimResponseDto
    {
        [JsonPropertyName("lat")]
        public string Lat { get; set; } = string.Empty;

        [JsonPropertyName("lon")]
        public string Lon { get; set; } = string.Empty;
    }
}