namespace Anticafe.Models.DTOs;

public class BookingDto
{
    public int Id { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int TableNumber { get; set; }
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public DateTime BookingDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string? EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateBookingDto
{
    public string GuestName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int TableNumber { get; set; }
    public int RoomId { get; set; } = 1;
    public DateTime BookingDate { get; set; } = DateTime.Now;
    public string StartTime { get; set; } = "19:00";
    public int DurationMinutes { get; set; } = 60;
}