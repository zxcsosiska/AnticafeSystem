using Anticafe.Models.DTOs;

namespace Anticafe.Services;

public interface IRoomService
{
    Task<List<RoomDto>> GetAllRoomsAsync();
    Task<RoomDto?> GetRoomByIdAsync(int id);
    Task<(bool success, string error, RoomDto? room)> CreateRoomAsync(CreateRoomDto request);
    Task<(bool success, string error)> DeleteRoomAsync(int id);
    Task<(bool success, string error, TableDto? table)> AddTableAsync(CreateTableDto request);
    Task<(bool success, string error)> DeleteTableAsync(int id);
    Task<List<TableDto>> GetAllTablesAsync();
}