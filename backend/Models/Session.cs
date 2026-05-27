namespace AnticafeBackend.Models;

public class Session
{
    public int Id { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int TableNumber { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int TotalMinutes { get; set; }
    public decimal TotalCost { get; set; }
    public bool IsActive { get; set; } = true;
    public decimal DrinksCost { get; set; }
}