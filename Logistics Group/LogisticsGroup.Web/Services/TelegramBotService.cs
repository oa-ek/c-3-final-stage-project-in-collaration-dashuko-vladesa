using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Polling;
using LogisticsGroup.Hubs;
using LogisticsGroup.Infrastructure.Data;

namespace LogisticsGroup.Services
{
    public class TelegramBotService : BackgroundService
    {
        private readonly ILogger<TelegramBotService> _logger;
        private readonly IHubContext<LocationHub> _locationHub;
        private readonly HttpClient _httpClient;
        private readonly IServiceProvider _serviceProvider;
        private TelegramBotClient _botClient;

        private readonly string _botToken = "8871980613:AAG1vBy-HhDsEi3TIlM6_eiqkomY2QZNL5w";
        private readonly string _orsApiKey = "eyJvcmciOiI1YjNjZTM1OTc4NTExMTAwMDFjZjYyNDgiLCJpZCI6IjZiMzQ0YzZhYTk1MDRlMDhiMGU4MTkwN2VlNzViMDIwIiwiaCI6Im11cm11cjY0In0=";

        public TelegramBotService(ILogger<TelegramBotService> logger, IHubContext<LocationHub> locationHub, HttpClient httpClient, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _locationHub = locationHub;
            _httpClient = httpClient;
            _serviceProvider = serviceProvider;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _botClient = new TelegramBotClient(_botToken);
            var receiverOptions = new ReceiverOptions { AllowedUpdates = Array.Empty<UpdateType>() };
            _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions, stoppingToken);
            return Task.CompletedTask;
        }

        private async Task HandleUpdateAsync(ITelegramBotClient bot, Telegram.Bot.Types.Update update, CancellationToken cancellationToken)
        {
            // 1. Обробка текстових повідомлень (Команда /start або введення цифрового коду авторизації)
            if (update.Type == UpdateType.Message && update.Message?.Text != null)
            {
                var chatId = update.Message.Chat.Id;
                string messageText = update.Message.Text.Trim();

                if (messageText.StartsWith("/start"))
                {
                    await bot.SendMessage(chatId,
                        "👋 Вітаємо в системі авторизації водіїв!\n\n" +
                        "🔑 Будь ласка, введіть **4-значний код авторизації**, який ви бачите у своєму особистому кабінеті водія на сайті, щоб прив'язати цей чат.",
                        parseMode: ParseMode.Markdown,
                        cancellationToken: cancellationToken);
                    return;
                }

                if (messageText.Length == 4 && int.TryParse(messageText, out int authCode))
                {
                    int driverId = authCode - 1000;

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var driver = context.Drivers.FirstOrDefault(d => d.Id == driverId);

                        if (driver != null)
                        {
                            driver.TelegramChatId = chatId;
                            await context.SaveChangesAsync(cancellationToken);

                            await bot.SendMessage(chatId,
                                $"✅ Авторизація успішна!\n\n" +
                                $"👤 Водій: **{driver.FullName}**\n" +
                                $"📱 Телефон: {driver.Phone}\n\n" +
                                "📌 Тепер ви підключені. Щоб логіст бачив вас на карті, натисніть:\n" +
                                "📎 Скріпка -> Геопозиція -> 'Транслювати геопозицію' (Live Location).",
                                parseMode: ParseMode.Markdown,
                                cancellationToken: cancellationToken);
                            return;
                        }
                    }

                    await bot.SendMessage(chatId, "❌ Невірний код авторизації або термін його дії закінчився. Перевірте код у кабінеті водія.", cancellationToken: cancellationToken);
                    return;
                }
            }

            // 2. ОДНОРАЗОВА ГЕОПОЗИЦІЯ (Додано малювання точки!)
            if (update.Type == UpdateType.Message && update.Message?.Location != null)
            {
                var lat = update.Message.Location.Latitude;
                var lng = update.Message.Location.Longitude;
                var chatId = update.Message.Chat.Id;

                await bot.SendMessage(chatId, "⏳ Отримуємо дані з серверів погоди та маршрутів...", cancellationToken: cancellationToken);

                string weatherText = await GetWeatherAsync(lat, lng);
                string routeText = await GetDistanceAsync(lat, lng, 50.4501, 30.5234);

                await bot.SendMessage(chatId, $"📊 Звіт за вашими координатами:\n\n🌡 {weatherText}\n\n🛣 {routeText}", cancellationToken: cancellationToken);

                // --- НОВЕ: Знаходимо водія і відправляємо точку на карту! ---
                int driverId = 0;
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var driver = context.Drivers.FirstOrDefault(d => d.TelegramChatId == chatId);
                    if (driver != null) driverId = driver.Id;
                }

                if (driverId > 0)
                {
                    // Відправляємо саме UpdateLocation, як чекає JS на сайті
                    await _locationHub.Clients.All.SendAsync("UpdateLocation", driverId, lat, lng, cancellationToken);
                    _logger.LogInformation($"[SignalR] ОДНОРАЗОВА локація водія ID={driverId} оновлена: Lat={lat}, Lng={lng}");
                }
            }

            // 3. ТРАНСЛЯЦІЯ РУХУ (Live Location)
            if (update.Type == UpdateType.EditedMessage && update.EditedMessage?.Location != null)
            {
                var lat = update.EditedMessage.Location.Latitude;
                var lng = update.EditedMessage.Location.Longitude;
                var chatId = update.EditedMessage.Chat.Id;
                int currentDriverId = 0;

                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var driver = context.Drivers.FirstOrDefault(d => d.TelegramChatId == chatId);
                    if (driver != null)
                    {
                        currentDriverId = driver.Id;
                    }
                }

                if (currentDriverId > 0)
                {
                    // --- ВИПРАВЛЕНО: Було UpdateDriverLocation, стало UpdateLocation ---
                    await _locationHub.Clients.All.SendAsync("UpdateLocation", currentDriverId, lat, lng, cancellationToken);
                    _logger.LogInformation($"[SignalR] LIVE Координати водія ID={currentDriverId} оновлено: Lat={lat}, Lng={lng}");
                }
            }
        }

        private async Task<string> GetWeatherAsync(double lat, double lng)
        {
            try
            {
                string sLat = lat.ToString(CultureInfo.InvariantCulture);
                string sLng = lng.ToString(CultureInfo.InvariantCulture);

                var response = await _httpClient.GetStringAsync($"https://api.open-meteo.com/v1/forecast?latitude={sLat}&longitude={sLng}&current_weather=true");
                using var doc = JsonDocument.Parse(response);
                var temp = doc.RootElement.GetProperty("current_weather").GetProperty("temperature").GetDouble();
                return $"Погода поруч: {temp}°C";
            }
            catch (Exception)
            {
                return $"Погода тимчасово недоступна";
            }
        }

        private async Task<string> GetDistanceAsync(double startLat, double startLng, double endLat, double endLng)
        {
            try
            {
                string sStartLat = startLat.ToString(CultureInfo.InvariantCulture);
                string sStartLng = startLng.ToString(CultureInfo.InvariantCulture);
                string sEndLat = endLat.ToString(CultureInfo.InvariantCulture);
                string sEndLng = endLng.ToString(CultureInfo.InvariantCulture);

                string url = $"https://api.openrouteservice.org/v2/directions/driving-hgv?api_key={_orsApiKey}&start={sStartLng},{sStartLat}&end={sEndLng},{sEndLat}";
                var response = await _httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);
                var distance = doc.RootElement.GetProperty("features")[0].GetProperty("properties").GetProperty("summary").GetProperty("distance").GetDouble();
                return $"Відстань до Києва (офіс): {Math.Round(distance / 1000, 1)} км";
            }
            catch (Exception)
            {
                return $"Маршрут: не вдалося прорахувати відстань";
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, HandleErrorSource source, CancellationToken ct)
        {
            _logger.LogError($"Помилка бота: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}