namespace AnticafeBackend.Models;

public class Table
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int TableNumber { get; set; }
    public int Capacity { get; set; } = 4;
    public bool IsActive { get; set; } = true;
    public bool HasCharger { get; set; }
    public bool HasLamp { get; set; } = true;
    public bool HasPrivacy { get; set; }
    public string? Description { get; set; }
    public virtual Room? Room { get; set; }
    public virtual ICollection<Session>? Sessions { get; set; }
    public virtual ICollection<Booking>? Bookings { get; set; }
}