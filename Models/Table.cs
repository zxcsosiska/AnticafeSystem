namespace Anticafe.Models;

public class Table
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public int TableNumber { get; set; }
    public int Capacity { get; set; } = 4;
    public bool IsActive { get; set; } = true;

    public Room? Room { get; set; }
    public ICollection<Session>? Sessions { get; set; }
    public ICollection<Booking>? Bookings { get; set; }
}