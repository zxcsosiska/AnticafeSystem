namespace AnticafeBackend.Models;

public class ActionLog
{
    public int Id { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string Data { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}