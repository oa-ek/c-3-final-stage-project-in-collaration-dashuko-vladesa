using LogisticsGroup.Web.DTOs;
using Microsoft.Extensions.Caching.Memory;
using System.Globalization;
using System.Text.Json;

namespace LogisticsGroup.Web.Services
{
    public class NominatimApiService : IGeocodingApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly ILogger<NominatimApiService> _logger;

        public NominatimApiService(HttpClient httpClient, IMemoryCache cache, ILogger<NominatimApiService> logger)
        {
            _httpClient = httpClient;
            _cache = cache;
            _logger = logger;
        }

        public async Task<(double? Lat, double? Lng)> GetCoordinatesAsync(string address)
        {
            // Спрощуємо адресу, бо Nominatim не любить "вул." і зайві коми
            var cleanAddress = address.Replace("вул.", "").Replace("  ", " ").Trim();

            var cacheKey = $"geo_{cleanAddress.ToLower()}";

            // ЗАВДАННЯ 8: Перевіряємо, чи є вже координати в кеші
            if (_cache.TryGetValue(cacheKey, out (double? Lat, double? Lng) cachedCoords))
            {
                _logger.LogInformation($"Взято з кешу координати для: {cleanAddress}");
                return cachedCoords;
            }

            try
            {
                // Робимо запит до Nominatim
                var url = $"?q={Uri.EscapeDataString(cleanAddress)}&format=json&limit=1";
                var response = await _httpClient.GetAsync(url);

                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var results = JsonSerializer.Deserialize<List<NominatimResponseDto>>(jsonString);

                if (results != null && results.Any())
                {
                    var firstResult = results.First();
                    var lat = Convert.ToDouble(firstResult.Lat, CultureInfo.InvariantCulture);
                    var lon = Convert.ToDouble(firstResult.Lon, CultureInfo.InvariantCulture);

                    var coords = (Lat: (double?)lat, Lng: (double?)lon);

                    // Зберігаємо в кеш на 30 днів
                    _cache.Set(cacheKey, coords, TimeSpan.FromDays(30));

                    return coords;
                }
            }
            catch (Exception ex)
            {
                // ЗАВДАННЯ 7: Обробка помилок
                _logger.LogWarning(ex, $"Не вдалося отримати координати для {cleanAddress}");
            }

            return (null, null);
        }
    }
}