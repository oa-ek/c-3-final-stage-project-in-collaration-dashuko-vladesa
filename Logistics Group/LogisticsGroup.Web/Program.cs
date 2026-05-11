using LogisticsGroup.Infrastructure.Data;
using LogisticsGroup.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;
using LogisticsGroup.Web.Services;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

builder.Services.AddScoped<LogisticsGroup.Domain.Interfaces.IUnitOfWork, LogisticsGroup.Infrastructure.Repositories.UnitOfWork>();

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
});
// Додаємо підтримку кешування в пам'яті (Завдання 8)
builder.Services.AddMemoryCache();
// ЗАВДАННЯ 10: Реєстрація Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<IRouteApiService, OpenRouteApiService>(client =>
{
    client.BaseAddress = new Uri("https://api.openrouteservice.org/");
    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "eyJvcmciOiI1YjNjZTM1OTc4NTExMTAwMDFjZjYyNDgiLCJpZCI6IjZiMzQ0YzZhYTk1MDRlMDhiMGU4MTkwN2VlNzViMDIwIiwiaCI6Im11cm11cjY0In0=");
})
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

// Реєструємо сервіс геокодування із зовнішнім API та Polly (Завдання 4 та 7)
builder.Services.AddHttpClient<IGeocodingApiService, NominatimApiService>(client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/search");
    client.DefaultRequestHeaders.Add("User-Agent", "LogisticsGroupApp/1.0");
    client.Timeout = TimeSpan.FromSeconds(10); // Відключитись, якщо висне
})
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

builder.Services.AddRazorPages();
builder.Services.AddTransient<IEmailSender, EmailSender>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Вмикаємо Swagger UI
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseStaticFiles();
app.MapStaticAssets();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbSeeder.SeedRolesAndAdminAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Помилка під час сідування бази даних.");
    }
}

app.Run();