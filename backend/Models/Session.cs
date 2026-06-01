using System;

namespace AnticafeBackend.Models;

public class Session
{
    public int Id { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int TableNumber { get; set; }
    public int RoomId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public int PlannedDurationMinutes { get; set; } 
    public decimal TariffRate { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public Room? Room { get; set; }
    public Table? Table { get; set; }
}