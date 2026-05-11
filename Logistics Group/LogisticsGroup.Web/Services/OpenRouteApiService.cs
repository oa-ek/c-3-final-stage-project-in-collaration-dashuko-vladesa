using System.Text.Json;

namespace LogisticsGroup.Web.Services
{
    public class OpenRouteApiService : IRouteApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenRouteApiService> _logger;

        public OpenRouteApiService(HttpClient httpClient, ILogger<OpenRouteApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<(double DistanceKm, double TimeHours)> GetRouteInfoAsync(double startLat, double startLng, double endLat, double endLng)
        {
            try
            {
                // Зверни увагу: ORS приймає координати у форматі "Довгота,Широта" (Lng, Lat)
                // Використовуємо профіль driving-hgv (важкі вантажівки)
                var url = $"v2/directions/driving-hgv?start={startLng.ToString(System.Globalization.CultureInfo.InvariantCulture)},{startLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&end={endLng.ToString(System.Globalization.CultureInfo.InvariantCulture)},{endLat.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonString);

                // Дістаємо дані з JSON (summary.distance та summary.duration)
                var summary = doc.RootElement
                    .GetProperty("features")[0]
                    .GetProperty("properties")
                    .GetProperty("summary");

                var distanceMeters = summary.GetProperty("distance").GetDouble();
                var durationSeconds = summary.GetProperty("duration").GetDouble();

                // Переводимо метри в кілометри, а секунди в години
                return (distanceMeters / 1000.0, durationSeconds / 3600.0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при розрахунку маршруту через OpenRouteService");
                return (0, 0);
            }
        }
    }
}