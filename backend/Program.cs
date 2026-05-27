using Microsoft.EntityFrameworkCore;
using AnticafeBackend.Data;
using AnticafeBackend.Services;
using System.Diagnostics;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

// Путь к БД в папке программы
var dbPath = Path.Combine(AppContext.BaseDirectory, "anticafe.db");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();

// Регистрируем сервисы
builder.Services.AddScoped<SecurityService>();
builder.Services.AddScoped<PricingService>();

var app = builder.Build();

// Настройка CORS
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());

// Раздача статических файлов
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();
app.MapControllers();

// SPA fallback
app.MapFallbackToFile("index.html");

// Инициализация БД
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();
}

// Автоматически открываем браузер через 1.5 секунды
Task.Run(async () =>
{
    await Task.Delay(1500);
    try
    {
        var url = "http://localhost:5154";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
    }
    catch { }
});

Console.WriteLine("========================================");
Console.WriteLine("   🍵 АНТИ-КАФЕ СИСТЕМА УПРАВЛЕНИЯ");
Console.WriteLine("========================================");
Console.WriteLine($"📁 База данных: {dbPath}");
Console.WriteLine($"🌐 Сервер запущен: http://localhost:5154");
Console.WriteLine("========================================");
Console.WriteLine("🛑 Для остановки закройте это окно");
Console.WriteLine("========================================");

app.Run("http://*:5154");