namespace AnticafeBackend.Models;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "usual";
    public int Capacity { get; set; } = 10;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public virtual ICollection<Table>? Tables { get; set; }
    public virtual ICollection<Session>? Sessions { get; set; }
    public virtual ICollection<Booking>? Bookings { get; set; }
}