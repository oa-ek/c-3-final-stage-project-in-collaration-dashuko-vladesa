using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace LogisticsGroup.Web.Services
{
    public class WeatherApiService
    {
        private readonly HttpClient _httpClient;

        public WeatherApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Метод повертає температуру та опис погоди (з іконкою)
        public async Task<(double Temp, string Description)?> GetCurrentWeatherAsync(double lat, double lng)
        {
            try
            {
                // Open-Meteo API - безкоштовний і не вимагає ключів!
                var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)}&longitude={lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}&current_weather=true";

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    var current = doc.RootElement.GetProperty("current_weather");

                    var temp = current.GetProperty("temperature").GetDouble();
                    var code = current.GetProperty("weathercode").GetInt32();

                    // Розшифровуємо міжнародні коди погоди WMO у зрозумілий текст з емодзі
                    var description = code switch
                    {
                        0 => "Ясно ☀️",
                        1 or 2 or 3 => "Мінлива хмарність ⛅",
                        45 or 48 => "Туман 🌫️",
                        51 or 53 or 55 => "Мряка 🌧️",
                        61 or 63 or 65 => "Дощ 🌧️",
                        71 or 73 or 75 => "Сніг ❄️",
                        95 or 96 or 99 => "Гроза ⛈️",
                        _ => "Похмуро ☁️"
                    };

                    return (temp, description);
                }
            }
            catch
            {
                // Якщо сервер погоди недоступний, ми просто повернемо null, щоб сайт не впав
            }
            return null;
        }
    }
}