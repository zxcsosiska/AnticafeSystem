namespace AnticafeBackend.Models;

public class Promotion
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal? Value { get; set; }
    public int? MinimumMinutes { get; set; }
    public decimal? MinimumCost { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? DaysOfWeek { get; set; }
    public int? HoursFrom { get; set; }
    public int? HoursTo { get; set; }
    public bool IsActive { get; set; } = true;
    public int? UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public string? Description { get; set; }
}