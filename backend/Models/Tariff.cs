namespace AnticafeBackend.Models;

public class Tariff
{
    public int Id { get; set; }
    public int? DayOfWeek { get; set; }
    public int HourFrom { get; set; }
    public int HourTo { get; set; }
    public decimal PricePerMinute { get; set; }
    public bool IsActive { get; set; } = true;
}