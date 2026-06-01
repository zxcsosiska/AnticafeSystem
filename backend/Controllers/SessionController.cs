using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnticafeBackend.Data;
using AnticafeBackend.Models;
using System.Timers;

namespace AnticafeBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SessionController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private static Dictionary<int, System.Timers.Timer> _timers = new();

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

        // Добавляем оставшееся время для каждого сеанса
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
            s.CreatedAt,
            RemainingMinutes = GetRemainingMinutes(s)
        });

        return Ok(result);
    }

    private int GetRemainingMinutes(Session session)
    {
        if (!session.IsActive) return 0;
        var endTime = session.StartTime.AddMinutes(session.PlannedDurationMinutes);
        var remaining = (int)(endTime - DateTime.Now).TotalMinutes;
        return remaining > 0 ? remaining : 0;
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
    public async Task<IActionResult> GetAvailableTables(DateTime startTime, int durationMinutes)
    {
        var endTime = startTime.AddMinutes(durationMinutes);
        var startTimeStr = startTime.ToString("HH:mm");
        var endTimeStr = endTime.ToString("HH:mm");
        var date = startTime.Date;

        var busyTables = await _context.Sessions
            .Where(s => s.IsActive)
            .Select(s => s.TableNumber)
            .ToListAsync();

        var bookedTables = await _context.Bookings
            .Where(b => b.Status == "active" &&
                        b.BookingDate.Date == date &&
                        string.Compare(b.StartTime, endTimeStr) < 0 &&
                        (b.EndTime == null || string.Compare(b.EndTime, startTimeStr) > 0))
            .Select(b => b.TableNumber)
            .ToListAsync();

        var allBusy = busyTables.Union(bookedTables).Distinct().ToList();

        var availableTables = await _context.Tables
            .Where(t => t.IsActive && !allBusy.Contains(t.TableNumber))
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

        // Рассчитываем время окончания
        var endTime = startTime.AddMinutes(request.DurationMinutes);

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

        // ЗАПУСКАЕМ ТАЙМЕР ДЛЯ АВТОМАТИЧЕСКОГО ЗАВЕРШЕНИЯ
        StartAutoEndTimer(session.Id, request.DurationMinutes);

        return Ok(new
        {
            session.Id,
            session.GuestName,
            session.Phone,
            session.TableNumber,
            StartTime = startTime.ToString("yyyy-MM-dd HH:mm:ss"),
            EndTime = endTime.ToString("yyyy-MM-dd HH:mm:ss"),
            PlannedDurationMinutes = session.PlannedDurationMinutes,
            DurationMinutes = session.DurationMinutes,
            session.TariffRate,
            session.TotalCost,
            session.IsActive
        });
    }

    private void StartAutoEndTimer(int sessionId, int durationMinutes)
    {
        var timer = new System.Timers.Timer(durationMinutes * 60 * 1000); // Переводим минуты в миллисекунды
        timer.Elapsed += async (sender, e) =>
        {
            timer.Stop();
            timer.Dispose();
            await AutoEndSession(sessionId);
        };
        timer.AutoReset = false;
        timer.Start();

        // Сохраняем таймер в словаре
        lock (_timers)
        {
            if (_timers.ContainsKey(sessionId))
                _timers[sessionId]?.Dispose();
            _timers[sessionId] = timer;
        }
    }

    private async Task AutoEndSession(int sessionId)
    {
        using var scope = _context.Database.GetDbConnection().ConnectionString != null ?
            new ServiceScopeFactory(_context).CreateScope() : null;

        var dbContext = scope != null ?
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>() :
            _context;

        var session = await dbContext.Sessions.FindAsync(sessionId);
        if (session == null || !session.IsActive) return;

        var endTime = DateTime.Now;
        var actualMinutes = session.PlannedDurationMinutes; // Используем запланированную длительность

        session.EndTime = endTime;
        session.DurationMinutes = actualMinutes;
        session.TotalCost = actualMinutes * session.TariffRate;
        session.IsActive = false;

        await dbContext.SaveChangesAsync();

        // Удаляем таймер из словаря
        lock (_timers)
        {
            if (_timers.ContainsKey(sessionId))
            {
                _timers.Remove(sessionId);
            }
        }

        Console.WriteLine($"[AUTO] Сеанс #{sessionId} автоматически завершён через {actualMinutes} минут");
    }

    [HttpPost("end/{id}")]
    public async Task<IActionResult> EndSession(int id)
    {
        var session = await _context.Sessions.FindAsync(id);
        if (session == null)
            return NotFound(new { error = "Сеанс не найден" });

        if (!session.IsActive)
            return BadRequest(new { error = "Сеанс уже завершён" });

        // Останавливаем таймер, если он есть
        lock (_timers)
        {
            if (_timers.ContainsKey(id))
            {
                _timers[id]?.Stop();
                _timers[id]?.Dispose();
                _timers.Remove(id);
            }
        }

        var endTime = DateTime.Now;
        var actualMinutes = session.PlannedDurationMinutes; // Используем запланированную длительность

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
            PlannedDurationMinutes = session.PlannedDurationMinutes,
            ActualDurationMinutes = actualMinutes,
            Hours = actualMinutes / 60,
            Minutes = actualMinutes % 60,
            session.TariffRate,
            session.TotalCost,
            Date = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
            Message = "Сеанс завершён"
        });
    }
}

// Вспомогательный класс для создания scope
public class ServiceScopeFactory
{
    private readonly DbContext _context;
    public ServiceScopeFactory(DbContext context) => _context = context;
    public IServiceScope CreateScope() => new ServiceScope(_context);
}

public class ServiceScope : IServiceScope
{
    private readonly DbContext _context;
    public ServiceScope(DbContext context) => _context = context;
    public IServiceProvider ServiceProvider => new ServiceProvider(_context);
    public void Dispose() { }
}

public class ServiceProvider : IServiceProvider
{
    private readonly DbContext _context;
    public ServiceProvider(DbContext context) => _context = context;
    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(ApplicationDbContext))
            return _context;
        return null;
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