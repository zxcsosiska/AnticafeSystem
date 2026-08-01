using Microsoft.EntityFrameworkCore;
using Anticafe.Data;
using Anticafe.Models;
using Anticafe.Models.DTOs;

namespace Anticafe.Services;

public class RoomService : IRoomService
{
    private readonly ApplicationDbContext _context;

    public RoomService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<RoomDto>> GetAllRoomsAsync()
    {
        return await _context.Rooms
            .OrderBy(r => r.Id)
            .Select(r => new RoomDto
            {
                Id = r.Id,
                Name = r.Name ?? string.Empty,
                Type = r.Type ?? string.Empty,
                Capacity = r.Capacity,
                TableCount = _context.Tables.Count(t => t.RoomId == r.Id),
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<RoomDto?> GetRoomByIdAsync(int id)
    {
        var room = await _context.Rooms
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room == null) return null;

        return new RoomDto
        {
            Id = room.Id,
            Name = room.Name ?? string.Empty,
            Type = room.Type ?? string.Empty,
            Capacity = room.Capacity,
            TableCount = await _context.Tables.CountAsync(t => t.RoomId == room.Id),
            IsActive = room.IsActive,
            CreatedAt = room.CreatedAt
        };
    }

    public async Task<(bool success, string error, RoomDto? room)> CreateRoomAsync(CreateRoomDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return (false, "Введите название зала", null);

            if (request.TableCount < 1 || request.TableCount > 50)
                return (false, "Количество столов должно быть от 1 до 50", null);

            // Проверка на дубликат названия
            var exists = await _context.Rooms.AnyAsync(r => r.Name == request.Name);
            if (exists)
                return (false, "Зал с таким названием уже существует", null);

            var room = new Room
            {
                Name = request.Name.Trim(),
                Type = string.IsNullOrWhiteSpace(request.Type) ? "usual" : request.Type,
                Capacity = request.TableCount * 4,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            // Создаём столы
            var maxTableNumber = await _context.Tables
                .Where(t => t.RoomId == room.Id)
                .MaxAsync(t => (int?)t.TableNumber) ?? 0;

            for (int i = 1; i <= request.TableCount; i++)
            {
                _context.Tables.Add(new Table
                {
                    RoomId = room.Id,
                    TableNumber = maxTableNumber + i,
                    Capacity = 4,
                    IsActive = true
                });
            }

            await _context.SaveChangesAsync();

            var dto = new RoomDto
            {
                Id = room.Id,
                Name = room.Name ?? string.Empty,
                Type = room.Type ?? string.Empty,
                Capacity = room.Capacity,
                TableCount = request.TableCount,
                IsActive = room.IsActive,
                CreatedAt = room.CreatedAt
            };

            return (true, string.Empty, dto);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    public async Task<(bool success, string error)> DeleteRoomAsync(int id)
    {
        try
        {
            var room = await _context.Rooms
                .Include(r => r.Tables)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
                return (false, "Зал не найден");

            if (id == 1)
                return (false, "Нельзя удалить основной зал");

            // Проверяем, есть ли активные сеансы в этом зале
            var hasActiveSessions = await _context.Sessions
                .AnyAsync(s => s.RoomId == id && s.IsActive);

            if (hasActiveSessions)
                return (false, "В зале есть активные сеансы");

            // Проверяем, есть ли активные бронирования
            var hasActiveBookings = await _context.Bookings
                .AnyAsync(b => b.RoomId == id && b.Status == "active");

            if (hasActiveBookings)
                return (false, "В зале есть активные бронирования");

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<(bool success, string error, TableDto? table)> AddTableAsync(CreateTableDto request)
    {
        try
        {
            if (request.RoomId <= 0)
                return (false, "Выберите зал", null);

            var room = await _context.Rooms.FindAsync(request.RoomId);
            if (room == null)
                return (false, "Зал не найден", null);

            var maxNumber = await _context.Tables
                .Where(t => t.RoomId == request.RoomId)
                .MaxAsync(t => (int?)t.TableNumber) ?? 0;

            var table = new Table
            {
                RoomId = request.RoomId,
                TableNumber = maxNumber + 1,
                Capacity = request.Capacity > 0 ? request.Capacity : 4,
                IsActive = true
            };

            _context.Tables.Add(table);
            await _context.SaveChangesAsync();

            var dto = new TableDto
            {
                Id = table.Id,
                RoomId = table.RoomId,
                RoomName = room.Name ?? string.Empty,
                TableNumber = table.TableNumber,
                Capacity = table.Capacity,
                IsActive = table.IsActive,
                IsAvailable = true
            };

            return (true, string.Empty, dto);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    public async Task<(bool success, string error)> DeleteTableAsync(int id)
    {
        try
        {
            var table = await _context.Tables.FindAsync(id);
            if (table == null)
                return (false, "Стол не найден");

            // Проверяем, есть ли активные сеансы за этим столом
            var hasActiveSessions = await _context.Sessions
                .AnyAsync(s => s.TableNumber == table.TableNumber && s.IsActive);

            if (hasActiveSessions)
                return (false, "За столом идёт активный сеанс");

            // Проверяем, есть ли активные бронирования
            var hasActiveBookings = await _context.Bookings
                .AnyAsync(b => b.TableNumber == table.TableNumber && b.Status == "active");

            if (hasActiveBookings)
                return (false, "На столе есть активные бронирования");

            _context.Tables.Remove(table);
            await _context.SaveChangesAsync();

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<List<TableDto>> GetAllTablesAsync()
    {
        return await _context.Tables
            .Where(t => t.IsActive)
            .Include(t => t.Room)
            .OrderBy(t => t.RoomId)
            .ThenBy(t => t.TableNumber)
            .Select(t => new TableDto
            {
                Id = t.Id,
                RoomId = t.RoomId,
                RoomName = t.Room != null ? t.Room.Name : string.Empty,
                TableNumber = t.TableNumber,
                Capacity = t.Capacity,
                IsActive = t.IsActive,
                IsAvailable = !_context.Sessions.Any(s => s.IsActive && s.TableNumber == t.TableNumber)
            })
            .ToListAsync();
    }
}