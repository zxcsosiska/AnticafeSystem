using Microsoft.EntityFrameworkCore;
using Anticafe.Models;

namespace Anticafe.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<Session> Sessions { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Room> Rooms { get; set; }
    public DbSet<Table> Tables { get; set; }
    public DbSet<Tariff> Tariffs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Table>()
            .HasIndex(t => new { t.RoomId, t.TableNumber })
            .IsUnique();
    }
}