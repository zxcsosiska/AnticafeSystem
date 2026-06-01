namespace AnticafeBackend.Models;

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
    public int GuestsCount { get; set; } = 1;
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; }
    public virtual Room? Room { get; set; }
    public virtual Table? Table { get; set; }
}