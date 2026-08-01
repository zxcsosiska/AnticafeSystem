namespace Anticafe.Models.DTOs;

public class RoomDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int TableCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateRoomDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "usual";
    public int TableCount { get; set; } = 1;
}