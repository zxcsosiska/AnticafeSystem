using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Diagnostics;
using Anticafe.Data;
using Anticafe.Services;
using Anticafe.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ====== КОНФИГУРАЦИЯ ======
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

// ====== БАЗА ДАННЫХ ======
var dbPath = Path.Combine(AppContext.BaseDirectory, "anticafe.db");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// ====== СЕРВИСЫ ======
builder.Services.AddScoped<SecurityService>();
builder.Services.AddScoped<PricingService>();
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<BookingService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddHttpContextAccessor();

// ====== АУТЕНТИФИКАЦИЯ JWT ======
var key = Encoding.UTF8.GetBytes(
    builder.Configuration["Jwt:Key"] ?? "AnticafeSuperSecretKey2024!@#$%^&*()_+"
);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // Поддержка получения токена из localStorage через Blazor
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (string.IsNullOrEmpty(token))
                {
                    // Пытаемся получить из запроса (для Blazor)
                    token = context.Request.Query["access_token"];
                }
                context.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ====== CORS ======
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
});

// ====== API + Blazor ======
builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// ====== HttpClient для API ======
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri("http://localhost:5154")
});

// ====== СБОРКА ======
var app = builder.Build();

// ====== ИНИЦИАЛИЗАЦИЯ БД ======
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
    await DbInitializer.InitializeAsync(db);
}

// ====== MIDDLEWARE ======
app.UseCors("AllowAll");
app.UseStaticFiles();
app.UseRouting();

// ====== АУТЕНТИФИКАЦИЯ ======
app.UseAuthentication();
app.UseAuthorization();

// ====== ГЛОБАЛЬНАЯ ОБРАБОТКА ОШИБОК ======
app.UseMiddleware<ExceptionHandlingMiddleware>();

// ====== МАРШРУТЫ ======
app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// ====== ЗАПУСК БРАУЗЕРА ======
_ = Task.Run(async () =>
{
    await Task.Delay(1500);
    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "http://localhost:5154/login",
            UseShellExecute = true
        });
    }
    catch { }
});

Console.WriteLine("========================================");
Console.WriteLine("   🍵 АНТИ-КАФЕ");
Console.WriteLine("========================================");
Console.WriteLine($"🌐 http://localhost:5154/login");
Console.WriteLine($"📁 База данных: {dbPath}");
Console.WriteLine($"🔐 JWT аутентификация включена");
Console.WriteLine("========================================");
Console.WriteLine("🛑 Ctrl+C для выхода");
Console.WriteLine("========================================");

app.Run("http://*:5154");