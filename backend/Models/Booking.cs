namespace AnticafeBackend.Models;

public class Booking
{
    public int Id { get; set; }
    public DateTime BookingDate { get; set; }
    public TimeSpan BookingTime { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int TableNumber { get; set; }
    public string Status { get; set; } = "active";
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}