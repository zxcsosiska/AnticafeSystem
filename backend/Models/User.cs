namespace AnticafeBackend.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "admin";
    public string? Phone { get; set; }
    public bool IsBlocked { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime? BlockedUntil { get; set; }
}