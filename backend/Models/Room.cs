namespace AnticafeBackend.Models;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "usual";
    public decimal Tariff { get; set; } = 3.5m;
}