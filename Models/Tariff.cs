namespace Anticafe.Models;

public class Tariff
{
    public int Id { get; set; }
    public string Name { get; set; } = "Стандартный";
    public int? DayOfWeek { get; set; }
    public int HourFrom { get; set; }
    public int HourTo { get; set; }
    public decimal PricePerMinute { get; set; }
    public int MinimumMinutes { get; set; } = 30;
    public bool IsActive { get; set; }
    public int Priority { get; set; }
    public DateTime CreatedAt { get; set; }
}