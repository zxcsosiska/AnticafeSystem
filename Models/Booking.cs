namespace Anticafe.Models;

public class Booking
{
    public int Id { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int TableNumber { get; set; }
    public int RoomId { get; set; }
    public DateTime BookingDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string? EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; }

    public Room? Room { get; set; }
    public Table? Table { get; set; }
}