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
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.Extensions.Configuration;

namespace LogisticsGroup.Services
{
    public class TelegramBotService : BackgroundService
    {
        private readonly ILogger<TelegramBotService> _logger;
        private readonly IHubContext<LocationHub> _locationHub;
        private readonly HttpClient _httpClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly string _botToken;
        private readonly string _orsApiKey;
        private TelegramBotClient _botClient;

        public TelegramBotService(ILogger<TelegramBotService> logger, IHubContext<LocationHub> locationHub, HttpClient httpClient, IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _logger = logger;
            _locationHub = locationHub;
            _httpClient = httpClient;
            _serviceProvider = serviceProvider;

            _botToken = configuration["ApiKeys:TelegramBotToken"];
            _orsApiKey = configuration["ApiKeys:OrsApiKey"];
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
            try
            {
                // 1. ТЕКСТОВІ КОМАНДИ
                if (update.Type == UpdateType.Message && update.Message?.Text != null)
                {
                    var chatId = update.Message.Chat.Id;
                    string messageText = update.Message.Text.Trim();

                    if (messageText.StartsWith("/start"))
                    {
                        await bot.SendMessage(chatId, "👋 Вітаємо!\n\n🔑 Введіть **4-значний код авторизації**.", parseMode: ParseMode.Markdown, cancellationToken: cancellationToken);
                        return;
                    }

                    if (messageText.StartsWith("/status"))
                    {
                        var inlineKeyboard = new InlineKeyboardMarkup(new[] {
                            new [] { InlineKeyboardButton.WithCallbackData("🚗 Почати рейс", "status_start") },
                            new [] { InlineKeyboardButton.WithCallbackData("✅ Завершити рейс", "status_finish") }
                        });
                        await bot.SendMessage(chatId, "Оберіть дію:", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
                        return;
                    }

                    // АВТОРИЗАЦІЯ
                    if (messageText.Length == 4 && int.TryParse(messageText, out int authCode))
                    {
                        int newDriverId = authCode - 1000;
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                            var oldDrivers = await context.Drivers.Where(d => d.TelegramChatId == chatId).ToListAsync();
                            foreach (var oldDriver in oldDrivers) { oldDriver.TelegramChatId = null; }

                            var driver = await context.Drivers.FirstOrDefaultAsync(d => d.Id == newDriverId);
                            if (driver != null)
                            {
                                driver.TelegramChatId = chatId;
                                await context.SaveChangesAsync(cancellationToken);
                                await bot.SendMessage(chatId, $"✅ Авторизація успішна!\n👤 Водій: {driver.FullName}\n\nЩоб логіст бачив вас на карті, надішліть геолокацію.", cancellationToken: cancellationToken);
                                return;
                            }
                        }
                        await bot.SendMessage(chatId, "❌ Невірний код авторизації.", cancellationToken: cancellationToken);
                        return;
                    }
                }

                // 1.5. КНОПКИ
                if (update.Type == UpdateType.CallbackQuery && update.CallbackQuery != null)
                {
                    var callbackQuery = update.CallbackQuery;
                    var chatId = callbackQuery.Message.Chat.Id;
                    var action = callbackQuery.Data;

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var flight = await context.Flights.Include(f => f.Driver)
                            .OrderByDescending(f => f.Id) // БЕРЕМО НАЙНОВІШИЙ РЕЙС
                            .FirstOrDefaultAsync(f => f.Driver.TelegramChatId == chatId && (f.Status == "В дорозі" || f.Status == "Створено"));

                        if (flight != null)
                        {
                            if (action == "status_start") { flight.Status = "В дорозі"; }
                            else if (action == "status_finish") { flight.Status = "Завершено"; flight.ArrivalDate = DateTime.Now; }
                            await context.SaveChangesAsync(cancellationToken);
                            await bot.AnswerCallbackQuery(callbackQuery.Id, "Статус оновлено!");
                            await bot.SendMessage(chatId, $"✅ Статус рейсу змінено на '{flight.Status}'.", cancellationToken: cancellationToken);
                        }
                    }
                    return;
                }

                // === 2. ГЕОПОЗИЦІЯ (ОДНОРАЗОВА ТА LIVE) ===
                bool isLocationUpdate = false;
                double lat = 0, lng = 0;
                long chatIdLoc = 0;

                if (update.Type == UpdateType.Message && update.Message?.Location != null)
                {
                    isLocationUpdate = true;
                    lat = update.Message.Location.Latitude;
                    lng = update.Message.Location.Longitude;
                    chatIdLoc = update.Message.Chat.Id;
                    await bot.SendMessage(chatIdLoc, "⏳ Отримуємо дані маршруту...", cancellationToken: cancellationToken);
                }
                else if (update.Type == UpdateType.EditedMessage && update.EditedMessage?.Location != null)
                {
                    isLocationUpdate = true;
                    lat = update.EditedMessage.Location.Latitude;
                    lng = update.EditedMessage.Location.Longitude;
                    chatIdLoc = update.EditedMessage.Chat.Id;
                }

                if (isLocationUpdate)
                {
                    int driverId = 0;
                    double destLat = 0, destLng = 0;
                    string destName = "";
                    bool hasActiveFlight = false;

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var driver = await context.Drivers.FirstOrDefaultAsync(d => d.TelegramChatId == chatIdLoc);

                        if (driver != null)
                        {
                            driverId = driver.Id;

                            // БЕРЕМО НАЙНОВІШИЙ РЕЙС ВОДІЯ
                            var flight = await context.Flights
                                .Include(f => f.Route).ThenInclude(r => r.RoutePoints).ThenInclude(rp => rp.Branch).ThenInclude(b => b.City)
                                .OrderByDescending(f => f.Id)
                                .FirstOrDefaultAsync(f => f.DriverId == driverId && (f.Status == "В дорозі" || f.Status == "Створено"));

                            if (flight != null && flight.Route?.RoutePoints != null && flight.Route.RoutePoints.Any())
                            {
                                hasActiveFlight = true;
                                var points = flight.Route.RoutePoints.ToList();
                                var lastPoint = points.FirstOrDefault(rp => !string.IsNullOrEmpty(rp.OperationType) &&
                                    (rp.OperationType.Contains("Розвантаження") || rp.OperationType.Contains("Кінцев") || rp.OperationType.Contains("Прибуття")));

                                if (lastPoint == null) lastPoint = points.OrderByDescending(rp => rp.Sequence).ThenByDescending(rp => rp.Id).FirstOrDefault();

                                if (lastPoint?.Branch != null)
                                {
                                    destName = lastPoint.Branch.City?.Name ?? $"Відділення {lastPoint.Branch.Id}";
                                    object finalLat = lastPoint.Branch.GetType().GetProperty("Latitude")?.GetValue(lastPoint.Branch) ?? lastPoint.Branch.City?.GetType().GetProperty("Latitude")?.GetValue(lastPoint.Branch.City);
                                    object finalLng = lastPoint.Branch.GetType().GetProperty("Longitude")?.GetValue(lastPoint.Branch) ?? lastPoint.Branch.City?.GetType().GetProperty("Longitude")?.GetValue(lastPoint.Branch.City);

                                    if (finalLat != null && finalLng != null)
                                    {
                                        destLat = Convert.ToDouble(finalLat.ToString().Replace(",", "."), CultureInfo.InvariantCulture);
                                        destLng = Convert.ToDouble(finalLng.ToString().Replace(",", "."), CultureInfo.InvariantCulture);
                                    }
                                }
                            }

                            // ВІДПРАВЛЯЄМО ДАНІ НА САЙТ (передаємо і локацію водія, і локацію міста призначення!)
                            await _locationHub.Clients.All.SendAsync("UpdateLocation", driverId, lat, lng, destLat, destLng, destName, cancellationToken);
                        }
                    }

                    // Якщо це була відправка вручну (не Live Location), відповідаємо текстом
                    if (update.Type == UpdateType.Message)
                    {
                        if (hasActiveFlight && destLat != 0)
                        {
                            string weatherText = await GetWeatherAsync(destLat, destLng);
                            string routeText = await GetDistanceAsync(lat, lng, destLat, destLng, destName);
                            await bot.SendMessage(chatIdLoc, $"📊 Звіт:\n\n🌡 {weatherText}\n🛣 {routeText}", cancellationToken: cancellationToken);
                        }
                        else
                        {
                            await bot.SendMessage(chatIdLoc, "📍 Геопозицію передано на радар.", cancellationToken: cancellationToken);
                        }
                    }
                }
            }
            catch (Exception globalEx)
            {
                _logger.LogError("Глобальна помилка бота: " + globalEx.ToString());
            }
        }

        private async Task<string> GetWeatherAsync(double lat, double lng)
        {
            try
            {
                string sLat = lat.ToString(CultureInfo.InvariantCulture); string sLng = lng.ToString(CultureInfo.InvariantCulture);
                var response = await _httpClient.GetStringAsync($"https://api.open-meteo.com/v1/forecast?latitude={sLat}&longitude={sLng}&current_weather=true");
                using var doc = JsonDocument.Parse(response);
                var temp = doc.RootElement.GetProperty("current_weather").GetProperty("temperature").GetDouble();
                return $"Погода поруч: {temp}°C";
            }
            catch { return $"Погода тимчасово недоступна"; }
        }

        private async Task<string> GetDistanceAsync(double startLat, double startLng, double endLat, double endLng, string destinationName)
        {
            try
            {
                string sStartLat = startLat.ToString(CultureInfo.InvariantCulture); string sStartLng = startLng.ToString(CultureInfo.InvariantCulture);
                string sEndLat = endLat.ToString(CultureInfo.InvariantCulture); string sEndLng = endLng.ToString(CultureInfo.InvariantCulture);

                string url = $"https://api.openrouteservice.org/v2/directions/driving-hgv?api_key={_orsApiKey}&start={sStartLng},{sStartLat}&end={sEndLng},{sEndLat}";
                var response = await _httpClient.GetStringAsync(url);
                using var doc = JsonDocument.Parse(response);
                var distance = doc.RootElement.GetProperty("features")[0].GetProperty("properties").GetProperty("summary").GetProperty("distance").GetDouble();

                return $"Відстань до м. {destinationName}: {Math.Round(distance / 1000, 1)} км";
            }
            catch { return $"Маршрут: не вдалося прорахувати відстань"; }
        }

        private Task HandleErrorAsync(ITelegramBotClient bot, Exception ex, HandleErrorSource source, CancellationToken ct)
        {
            _logger.LogError($"Помилка бота: {ex.Message}");
            return Task.CompletedTask;
        }
    }
}