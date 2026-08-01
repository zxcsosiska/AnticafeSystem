using Microsoft.EntityFrameworkCore;
using Anticafe.Data;
using Anticafe.Models;
using Anticafe.Models.DTOs;

namespace Anticafe.Services;

public class SessionService : ISessionService
{
    private readonly ApplicationDbContext _context;
    private readonly PricingService _pricing;

    public SessionService(ApplicationDbContext context, PricingService pricing)
    {
        _context = context;
        _pricing = pricing;
    }

    public async Task<List<SessionDto>> GetActiveSessionsAsync()
    {
        return await _context.Sessions
            .Where(s => s.IsActive)
            .Include(s => s.Room)
            .OrderBy(s => s.StartTime)
            .Select(s => new SessionDto
            {
                Id = s.Id,
                GuestName = s.GuestName ?? string.Empty,
                Phone = s.Phone,
                TableNumber = s.TableNumber,
                RoomId = s.RoomId,
                RoomName = s.Room != null ? s.Room.Name : string.Empty,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                PlannedDurationMinutes = s.PlannedDurationMinutes,
                DurationMinutes = s.DurationMinutes,
                TariffRate = s.TariffRate,
                TotalCost = s.TotalCost,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<SessionDto>> GetCompletedSessionsAsync(DateTime date)
    {
        var startDate = date.Date;
        var endDate = date.Date.AddDays(1);

        return await _context.Sessions
            .Where(s => !s.IsActive && s.EndTime.HasValue && s.EndTime.Value >= startDate && s.EndTime.Value < endDate)
            .Include(s => s.Room)
            .OrderByDescending(s => s.EndTime)
            .Select(s => new SessionDto
            {
                Id = s.Id,
                GuestName = s.GuestName ?? string.Empty,
                Phone = s.Phone,
                TableNumber = s.TableNumber,
                RoomId = s.RoomId,
                RoomName = s.Room != null ? s.Room.Name : string.Empty,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                PlannedDurationMinutes = s.PlannedDurationMinutes,
                DurationMinutes = s.DurationMinutes,
                TariffRate = s.TariffRate,
                TotalCost = s.TotalCost,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<SessionDto?> GetSessionByIdAsync(int id)
    {
        var session = await _context.Sessions
            .Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (session == null) return null;

        return new SessionDto
        {
            Id = session.Id,
            GuestName = session.GuestName ?? string.Empty,
            Phone = session.Phone,
            TableNumber = session.TableNumber,
            RoomId = session.RoomId,
            RoomName = session.Room != null ? session.Room.Name : string.Empty,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            PlannedDurationMinutes = session.PlannedDurationMinutes,
            DurationMinutes = session.DurationMinutes,
            TariffRate = session.TariffRate,
            TotalCost = session.TotalCost,
            IsActive = session.IsActive,
            CreatedAt = session.CreatedAt
        };
    }

    public async Task<(bool success, string error, SessionDto? session)> StartSessionAsync(StartSessionDto request)
    {
        // Валидация
        if (string.IsNullOrWhiteSpace(request.GuestName))
            return (false, "Введите имя гостя", null);

        if (request.TableNumber <= 0)
            return (false, "Выберите стол", null);

        if (request.DurationMinutes < 30)
            return (false, "Минимальная длительность - 30 минут", null);

        if (request.RoomId <= 0)
            return (false, "Выберите зал", null);

        // Проверка существования стола
        var table = await _context.Tables
            .FirstOrDefaultAsync(t => t.TableNumber == request.TableNumber && t.RoomId == request.RoomId && t.IsActive);

        if (table == null)
            return (false, "Стол не найден в выбранном зале", null);

        // Проверка занятости стола
        var isBusy = await _context.Sessions
            .AnyAsync(s => s.IsActive && s.TableNumber == request.TableNumber);

        if (isBusy)
            return (false, "Стол уже занят", null);

        // Определяем время начала
        var startTime = request.StartTime ?? DateTime.Now;
        if (startTime.Kind == DateTimeKind.Utc)
            startTime = startTime.ToLocalTime();

        // Получаем актуальную цену
        var currentPrice = await _pricing.GetCurrentTariffAsync(startTime);
        var totalCost = request.DurationMinutes * currentPrice;

        var session = new Session
        {
            GuestName = request.GuestName.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            TableNumber = request.TableNumber,
            RoomId = request.RoomId,
            StartTime = startTime,
            PlannedDurationMinutes = request.DurationMinutes,
            DurationMinutes = request.DurationMinutes,
            TariffRate = currentPrice,
            TotalCost = totalCost,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var dto = new SessionDto
        {
            Id = session.Id,
            GuestName = session.GuestName ?? string.Empty,
            Phone = session.Phone,
            TableNumber = session.TableNumber,
            RoomId = session.RoomId,
            RoomName = table.Room?.Name ?? string.Empty,
            StartTime = session.StartTime,
            EndTime = session.EndTime,
            PlannedDurationMinutes = session.PlannedDurationMinutes,
            DurationMinutes = session.DurationMinutes,
            TariffRate = session.TariffRate,
            TotalCost = session.TotalCost,
            IsActive = session.IsActive,
            CreatedAt = session.CreatedAt
        };

        return (true, string.Empty, dto);
    }

    public async Task<(bool success, string error, EndSessionResultDto? result)> EndSessionAsync(int id)
    {
        var session = await _context.Sessions.FindAsync(id);
        if (session == null)
            return (false, "Сеанс не найден", null);

        if (!session.IsActive)
            return (false, "Сеанс уже завершён", null);

        var endTime = DateTime.Now;
        var actualMinutes = (int)Math.Ceiling((endTime - session.StartTime).TotalMinutes);

        // Минимальная длительность 30 минут
        if (actualMinutes < 30)
            actualMinutes = 30;

        // Пересчитываем стоимость по актуальному тарифу на момент начала
        var currentPrice = await _pricing.GetCurrentTariffAsync(session.StartTime);
        var totalCost = actualMinutes * currentPrice;

        session.EndTime = endTime;
        session.DurationMinutes = actualMinutes;
        session.TariffRate = currentPrice;
        session.TotalCost = totalCost;
        session.IsActive = false;

        await _context.SaveChangesAsync();

        var result = new EndSessionResultDto
        {
            Id = session.Id,
            GuestName = session.GuestName ?? string.Empty,
            ActualDurationMinutes = actualMinutes,
            TotalCost = totalCost,
            Message = actualMinutes >= 120 ? "Спасибо за долгий визит! 😊" : "Спасибо за посещение! 🍵"
        };

        return (true, string.Empty, result);
    }

    public async Task<List<TableDto>> GetAvailableTablesAsync(DateTime startTime, int durationMinutes, int roomId)
    {
        if (durationMinutes < 30)
            return new List<TableDto>();

        var endTime = startTime.AddMinutes(durationMinutes);
        var date = startTime.Date;

        // Занятые столы (активные сеансы)
        var busyTables = await _context.Sessions
            .Where(s => s.IsActive)
            .Select(s => s.TableNumber)
            .ToListAsync();

        // Забронированные столы на это время
        var bookedTables = await _context.Bookings
            .Where(b => b.Status == "active" &&
                        b.BookingDate.Date == date &&
                        b.StartTime.CompareTo(endTime.ToString("HH:mm")) < 0 &&
                        (b.EndTime == null || b.EndTime.CompareTo(startTime.ToString("HH:mm")) > 0))
            .Select(b => b.TableNumber)
            .ToListAsync();

        var allBusy = busyTables.Union(bookedTables).Distinct().ToList();

        return await _context.Tables
            .Where(t => t.IsActive && t.RoomId == roomId && !allBusy.Contains(t.TableNumber))
            .Include(t => t.Room)
            .Select(t => new TableDto
            {
                Id = t.Id,
                RoomId = t.RoomId,
                RoomName = t.Room != null ? t.Room.Name : string.Empty,
                TableNumber = t.TableNumber,
                Capacity = t.Capacity,
                IsActive = t.IsActive,
                IsAvailable = true
            })
            .ToListAsync();
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

    public async Task<List<RoomDto>> GetAllRoomsAsync()
    {
        return await _context.Rooms
            .Where(r => r.IsActive)
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
}