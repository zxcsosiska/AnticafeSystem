using Microsoft.EntityFrameworkCore;
using AnticafeBackend.Data;
using AnticafeBackend.Models;
using AnticafeBackend.Services;
using System.Diagnostics;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

var dbPath = Path.Combine(AppContext.BaseDirectory, "anticafe.db");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();

builder.Services.AddScoped<SecurityService>();
builder.Services.AddScoped<PricingService>();

var app = builder.Build();

app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    db.Database.EnsureDeleted();
    db.Database.EnsureCreated();

    // Админ
    db.Users.Add(new User
    {
        Username = "admin",
        PasswordHash = "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918",
        FullName = "Администратор",
        Role = "admin",
        IsBlocked = false,
        FailedAttempts = 0
    });

    // Основной зал
    var mainRoom = new Room
    {
        Name = "Основной зал",
        Type = "usual",
        IsActive = true,
        CreatedAt = DateTime.Now,
        Capacity = 60
    };
    db.Rooms.Add(mainRoom);
    db.SaveChanges();

    // Столы 1-15
    for (int i = 1; i <= 15; i++)
    {
        db.Tables.Add(new Table
        {
            RoomId = mainRoom.Id,
            TableNumber = i,
            Capacity = 4,
            IsActive = true,
            HasCharger = false,
            HasLamp = true,
            HasPrivacy = false
        });
    }

    // Тариф
    db.Tariffs.Add(new Tariff
    {
        Name = "Стандартный",
        PricePerMinute = 3.5m,
        MinimumMinutes = 30,
        IsActive = true,
        CreatedAt = DateTime.Now
    });

    // Настройки
    db.Settings.Add(new Setting { Key = "PricePerMinute", Value = "3.5", UpdatedAt = DateTime.Now });
    db.Settings.Add(new Setting { Key = "MinimumMinutes", Value = "30", UpdatedAt = DateTime.Now });

    db.SaveChanges();
}

_ = Task.Run(async () =>
{
    await Task.Delay(1000);
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