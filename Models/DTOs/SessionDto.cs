namespace Anticafe.Models.DTOs;

public class SessionDto
{
    public int Id { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int TableNumber { get; set; }
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int PlannedDurationMinutes { get; set; }
    public int DurationMinutes { get; set; }
    public decimal TariffRate { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StartSessionDto
{
    public string GuestName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int TableNumber { get; set; }
    public int RoomId { get; set; } = 1;
    public DateTime? StartTime { get; set; }
    public int DurationMinutes { get; set; } = 30;
}

public class EndSessionResultDto
{
    public int Id { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public int ActualDurationMinutes { get; set; }
    public decimal TotalCost { get; set; }
    public string Message { get; set; } = string.Empty;
}