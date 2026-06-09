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
    public DbSet<Setting> Settings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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

        // Уникальность в пределах зала
        modelBuilder.Entity<Table>()
            .HasIndex(t => new { t.RoomId, t.TableNumber })
            .IsUnique();

        modelBuilder.Entity<Session>()
            .Property(s => s.PlannedDurationMinutes)
            .HasDefaultValue(60);

        // Настройка каскадного удаления для Session и Booking
        modelBuilder.Entity<Session>()
            .HasOne(s => s.Table)
            .WithMany(t => t.Sessions)
            .HasForeignKey(s => s.TableNumber)
            .HasPrincipalKey(t => t.TableNumber)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.Table)
            .WithMany(t => t.Bookings)
            .HasForeignKey(b => b.TableNumber)
            .HasPrincipalKey(t => t.TableNumber)
            .OnDelete(DeleteBehavior.SetNull);
    }
}