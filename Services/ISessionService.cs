using Anticafe.Models.DTOs;

namespace Anticafe.Services;

public interface ISessionService
{
    Task<List<SessionDto>> GetActiveSessionsAsync();
    Task<List<SessionDto>> GetCompletedSessionsAsync(DateTime date);
    Task<SessionDto?> GetSessionByIdAsync(int id);
    Task<(bool success, string error, SessionDto? session)> StartSessionAsync(StartSessionDto request);
    Task<(bool success, string error, EndSessionResultDto? result)> EndSessionAsync(int id);
    Task<List<TableDto>> GetAvailableTablesAsync(DateTime startTime, int durationMinutes, int roomId);
    Task<List<TableDto>> GetAllTablesAsync();
    Task<List<RoomDto>> GetAllRoomsAsync();
}