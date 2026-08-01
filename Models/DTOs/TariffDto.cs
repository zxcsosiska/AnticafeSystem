namespace Anticafe.Models.DTOs;

public class TariffDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PricePerMinute { get; set; }
    public int? DayOfWeek { get; set; }
    public string? DayName { get; set; }
    public int HourFrom { get; set; }
    public int HourTo { get; set; }
    public string TimeRange { get; set; } = string.Empty;
    public int MinimumMinutes { get; set; }
    public bool IsActive { get; set; }
    public int Priority { get; set; }
    public bool IsActiveNow { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTariffDto
{
    public string Name { get; set; } = "Новый тариф";
    public decimal PricePerMinute { get; set; } = 3.5m;
    public int? DayOfWeek { get; set; }
    public int HourFrom { get; set; } = 0;
    public int HourTo { get; set; } = 0;
    public int MinimumMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 0;
}