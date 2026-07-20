using Anticafe.Models;
using BCrypt.Net;

namespace Anticafe.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext db)
    {
        if (db.Users.Any()) return;

        // Админ с BCrypt паролем
        db.Users.Add(new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin", workFactor: 12),
            FullName = "Администратор",
            Role = "admin"
        });

        // Зал
        var room = new Room { Name = "Основной зал", Type = "usual", IsActive = true, Capacity = 60, CreatedAt = DateTime.Now };
        db.Rooms.Add(room);
        await db.SaveChangesAsync();

        // Столы 1-15
        for (int i = 1; i <= 15; i++)
        {
            db.Tables.Add(new Table { RoomId = room.Id, TableNumber = i, Capacity = 4, IsActive = true });
        }

        // Тариф
        db.Tariffs.Add(new Tariff
        {
            Name = "Стандартный",
            PricePerMinute = 3.5m,
            MinimumMinutes = 30,
            IsActive = true,
            Priority = 0,
            CreatedAt = DateTime.Now
        });

        await db.SaveChangesAsync();
    }
}