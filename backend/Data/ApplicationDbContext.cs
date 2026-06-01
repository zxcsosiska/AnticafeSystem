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
    public DbSet<Table> Tables { get; set; }
    public DbSet<Tariff> Tariffs { get; set; }
    public DbSet<Promotion> Promotions { get; set; }
    public DbSet<Drink> Drinks { get; set; }
    public DbSet<ActionLog> ActionLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ============================================
        // СВЯЗИ МЕЖДУ ТАБЛИЦАМИ
        // ============================================

        modelBuilder.Entity<Session>()
            .HasOne(s => s.Room)
            .WithMany(r => r.Sessions)
            .HasForeignKey(s => s.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Session>()
            .HasOne(s => s.Table)
            .WithMany(t => t.Sessions)
            .HasForeignKey(s => s.TableNumber)
            .HasPrincipalKey(t => t.TableNumber)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Room)
            .WithMany(r => r.Bookings)
            .HasForeignKey(b => b.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Table)
            .WithMany(t => t.Bookings)
            .HasForeignKey(b => b.TableNumber)
            .HasPrincipalKey(t => t.TableNumber)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Table>()
            .HasOne(t => t.Room)
            .WithMany(r => r.Tables)
            .HasForeignKey(t => t.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        // ============================================
        // НАСТРОЙКИ ПОЛЕЙ
        // ============================================

        modelBuilder.Entity<Session>()
            .Property(s => s.PlannedDurationMinutes)
            .HasDefaultValue(60);

        // ============================================
        // НАЧАЛЬНЫЕ ДАННЫЕ
        // ============================================

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

        modelBuilder.Entity<Room>().HasData(
            new Room { Id = 1, Name = "Основной зал", Type = "usual", Capacity = 20, IsActive = true, CreatedAt = DateTime.Now },
            new Room { Id = 2, Name = "VIP зал", Type = "vip", Capacity = 8, IsActive = true, CreatedAt = DateTime.Now }
        );

        // Столы (Основной зал 1-10)
        for (int i = 1; i <= 10; i++)
        {
            modelBuilder.Entity<Table>().HasData(new Table
            {
                Id = i,
                RoomId = 1,
                TableNumber = i,
                Capacity = i <= 5 ? 2 : 4,
                IsActive = true,
                HasCharger = i % 2 == 0,
                HasLamp = true,
                HasPrivacy = i >= 8
            });
        }

        // Столы (VIP зал 11-13)
        for (int i = 11; i <= 13; i++)
        {
            modelBuilder.Entity<Table>().HasData(new Table
            {
                Id = i,
                RoomId = 2,
                TableNumber = i,
                Capacity = i == 13 ? 8 : 6,
                IsActive = true,
                HasCharger = true,
                HasLamp = true,
                HasPrivacy = true
            });
        }

        modelBuilder.Entity<Tariff>().HasData(new Tariff
        {
            Id = 1,
            Name = "Стандартный",
            DayOfWeek = null,
            HourFrom = 0,
            HourTo = 23,
            PricePerMinute = 3.5m,
            MinimumMinutes = 30,
            IsActive = true,
            Priority = 0,
            CreatedAt = DateTime.Now
        });

        modelBuilder.Entity<Tariff>().HasData(new Tariff
        {
            Id = 2,
            Name = "VIP",
            DayOfWeek = null,
            HourFrom = 0,
            HourTo = 23,
            PricePerMinute = 5.0m,
            MinimumMinutes = 30,
            IsActive = true,
            Priority = 10,
            CreatedAt = DateTime.Now
        });
    }
}