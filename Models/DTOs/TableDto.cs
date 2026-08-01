namespace Anticafe.Models.DTOs;

public class TableDto
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public int TableNumber { get; set; }
    public int Capacity { get; set; }
    public bool IsActive { get; set; }
    public bool IsAvailable { get; set; }
}

public class CreateTableDto
{
    public int RoomId { get; set; }
    public int Capacity { get; set; } = 4;
}