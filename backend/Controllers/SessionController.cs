using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnticafeBackend.Data;
using AnticafeBackend.Models;

namespace AnticafeBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SessionController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveSessions()
    {
        var sessions = await _context.Sessions
            .Where(s => s.IsActive)
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        var result = sessions.Select(s => new
        {
            s.Id,
            s.GuestName,
            s.Phone,
            s.TableNumber,
            s.RoomId,
            s.StartTime,
            s.PlannedDurationMinutes,
            s.DurationMinutes,
            s.TariffRate,
            s.TotalCost,
            s.IsActive,
            s.CreatedAt
        });

        return Ok(result);
    }

    [HttpGet("tables")]
    public async Task<IActionResult> GetTables()
    {
        var tables = await _context.Tables
            .Where(t => t.IsActive)
            .Select(t => new { t.Id, t.TableNumber, t.RoomId })
            .ToListAsync();
        return Ok(tables);
    }

    [HttpGet("rooms")]
    public async Task<IActionResult> GetRooms()
    {
        var rooms = await _context.Rooms.ToListAsync();
        return Ok(rooms);
    }

    [HttpGet("available-tables")]
    public async Task<IActionResult> GetAvailableTables(DateTime startTime, int durationMinutes, int roomId)
    {
        var endTime = startTime.AddMinutes(durationMinutes);
        var startTimeStr = startTime.ToString("HH:mm");
        var endTimeStr = endTime.ToString("HH:mm");
        var date = startTime.Date;

        // Занятые столы (активные сеансы)
        var busyTables = await _context.Sessions
            .Where(s => s.IsActive)
            .Select(s => s.TableNumber)
            .ToListAsync();

        // Забронированные столы
        var bookedTables = await _context.Bookings
            .Where(b => b.Status == "active" &&
                        b.BookingDate.Date == date &&
                        string.Compare(b.StartTime, endTimeStr) < 0 &&
                        (b.EndTime == null || string.Compare(b.EndTime, startTimeStr) > 0))
            .Select(b => b.TableNumber)
            .ToListAsync();

        var allBusy = busyTables.Union(bookedTables).Distinct().ToList();

        // Доступные столы ТОЛЬКО из выбранного зала
        var availableTables = await _context.Tables
            .Where(t => t.IsActive && t.RoomId == roomId && !allBusy.Contains(t.TableNumber))
            .Select(t => new { t.Id, t.TableNumber, t.RoomId })
            .ToListAsync();

        return Ok(availableTables);
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartSession([FromBody] StartSessionRequest request)
    {
        // Проверки
        if (string.IsNullOrEmpty(request.GuestName))
            return BadRequest(new { error = "Введите имя гостя" });

        if (request.TableNumber <= 0)
            return BadRequest(new { error = "Выберите стол" });

        if (request.DurationMinutes < 30)
            return BadRequest(new { error = "Минимальная длительность - 30 минут" });

        // Проверка что стол существует и принадлежит выбранному залу
        var table = await _context.Tables
            .FirstOrDefaultAsync(t => t.TableNumber == request.TableNumber && t.RoomId == request.RoomId);

        if (table == null)
            return BadRequest(new { error = "Стол не найден в выбранном зале" });

        // Проверка что стол свободен
        var isBusy = await _context.Sessions
            .AnyAsync(s => s.IsActive && s.TableNumber == request.TableNumber);

        if (isBusy)
            return BadRequest(new { error = "Стол уже занят" });

        // Исправление часового пояса
        DateTime startTime;

        if (request.StartTime.HasValue)
        {
            if (request.StartTime.Value.Kind == DateTimeKind.Utc)
            {
                startTime = request.StartTime.Value.ToLocalTime();
            }
            else
            {
                startTime = request.StartTime.Value;
            }
        }
        else
        {
            startTime = DateTime.Now;
        }

        // Создаём сеанс
        var session = new Session
        {
            GuestName = request.GuestName,
            Phone = request.Phone,
            TableNumber = request.TableNumber,
            RoomId = request.RoomId,
            StartTime = startTime,
            PlannedDurationMinutes = request.DurationMinutes,
            DurationMinutes = request.DurationMinutes,
            TariffRate = 3.5m,
            TotalCost = request.DurationMinutes * 3.5m,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            session.Id,
            session.GuestName,
            session.Phone,
            session.TableNumber,
            session.RoomId,
            StartTime = startTime.ToString("yyyy-MM-dd HH:mm:ss"),
            PlannedDurationMinutes = session.PlannedDurationMinutes,
            DurationMinutes = session.DurationMinutes,
            session.TariffRate,
            session.TotalCost,
            session.IsActive
        });
    }

    [HttpPost("end/{id}")]
    public async Task<IActionResult> EndSession(int id)
    {
        var session = await _context.Sessions.FindAsync(id);
        if (session == null)
            return NotFound(new { error = "Сеанс не найден" });

        if (!session.IsActive)
            return BadRequest(new { error = "Сеанс уже завершён" });

        var endTime = DateTime.Now;
        var actualMinutes = (int)Math.Ceiling((endTime - session.StartTime).TotalMinutes);
        if (actualMinutes < 30) actualMinutes = 30;

        var plannedMinutes = session.PlannedDurationMinutes;

        session.EndTime = endTime;
        session.DurationMinutes = actualMinutes;
        session.TotalCost = actualMinutes * session.TariffRate;
        session.IsActive = false;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            session.Id,
            session.GuestName,
            session.Phone,
            session.TableNumber,
            StartTime = session.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
            EndTime = session.EndTime.Value.ToString("yyyy-MM-dd HH:mm:ss"),
            PlannedDurationMinutes = plannedMinutes,
            ActualDurationMinutes = actualMinutes,
            Hours = actualMinutes / 60,
            Minutes = actualMinutes % 60,
            session.TariffRate,
            session.TotalCost,
            Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
            Message = actualMinutes >= 120 ? "Спасибо за долгий визит!" : "Спасибо за посещение!"
        });
    }
}

public class StartSessionRequest
{
    public string GuestName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int TableNumber { get; set; }
    public int RoomId { get; set; } = 1;
    public DateTime? StartTime { get; set; }
    public int DurationMinutes { get; set; } = 60;
}