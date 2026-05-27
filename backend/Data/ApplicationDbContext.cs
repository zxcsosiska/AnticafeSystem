using Microsoft.EntityFrameworkCore;
using AnticafeBackend.Models;

namespace AnticafeBackend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Tariff> Tariffs { get; set; }
    public DbSet<Promotion> Promotions { get; set; }
    public DbSet<Drink> Drinks { get; set; }
    public DbSet<ActionLog> ActionLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Тариф по умолчанию
        modelBuilder.Entity<Tariff>().HasData(new Tariff
        {
            Id = 1,
            DayOfWeek = null,
            HourFrom = 0,
            HourTo = 23,
            PricePerMinute = 3.5m,
            IsActive = true
        });

        // Админ по умолчанию (пароль: admin123)
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918",
            FullName = "Администратор",
            Role = "admin",
            IsBlocked = false,
            FailedAttempts = 0
        });

        // Зал по умолчанию
        modelBuilder.Entity<Room>().HasData(new Room
        {
            Id = 1,
            Name = "Основной зал",
            Type = "usual",
            Tariff = 3.5m
        });
    }
}