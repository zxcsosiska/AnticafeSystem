using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Anticafe.Data;
using Anticafe.Models;
using Anticafe.Services;

namespace Anticafe.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly PricingService _pricing;

    public SessionController(ApplicationDbContext context, PricingService pricing)
    {
        _context = context;
        _pricing = pricing;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveSessions()
    {
        var sessions = await _context.Sessions
            .Where(s => s.IsActive)
            .OrderBy(s => s.StartTime)
            .Select(s => new
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
            })
            .ToListAsync();

        return Ok(sessions);
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
        var rooms = await _context.Rooms
            .Where(r => r.IsActive)
            .Select(r => new { r.Id, r.Name, r.Type })
            .ToListAsync();
        return Ok(rooms);
    }

    [HttpGet("available-tables")]
    public async Task<IActionResult> GetAvailableTables(DateTime startTime, int durationMinutes, int roomId)
    {
        if (durationMinutes < 30)
            return BadRequest(new { error = "Минимальная длительность - 30 минут" });

        var endTime = startTime.AddMinutes(durationMinutes);
        var date = startTime.Date;

        var busyTables = await _context.Sessions
            .Where(s => s.IsActive)
            .Select(s => s.TableNumber)
            .ToListAsync();

        var bookedTables = await _context.Bookings
            .Where(b => b.Status == "active" &&
                        b.BookingDate.Date == date &&
                        b.StartTime.CompareTo(endTime.ToString("HH:mm")) < 0 &&
                        (b.EndTime == null || b.EndTime.CompareTo(startTime.ToString("HH:mm")) > 0))
            .Select(b => b.TableNumber)
            .ToListAsync();

        var allBusy = busyTables.Union(bookedTables).Distinct().ToList();

        var availableTables = await _context.Tables
            .Where(t => t.IsActive && t.RoomId == roomId && !allBusy.Contains(t.TableNumber))
            .Select(t => new { t.Id, t.TableNumber, t.RoomId })
            .ToListAsync();

        return Ok(availableTables);
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartSession([FromBody] StartSessionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GuestName))
            return BadRequest(new { error = "Введите имя гостя" });

        if (request.TableNumber <= 0)
            return BadRequest(new { error = "Выберите стол" });

        if (request.DurationMinutes < 30)
            return BadRequest(new { error = "Минимальная длительность - 30 минут" });

        if (request.RoomId <= 0)
            return BadRequest(new { error = "Выберите зал" });

        var table = await _context.Tables
            .FirstOrDefaultAsync(t => t.TableNumber == request.TableNumber && t.RoomId == request.RoomId && t.IsActive);

        if (table == null)
            return BadRequest(new { error = "Стол не найден в выбранном зале" });

        var isBusy = await _context.Sessions
            .AnyAsync(s => s.IsActive && s.TableNumber == request.TableNumber);

        if (isBusy)
            return BadRequest(new { error = "Стол уже занят" });

        var startTime = request.StartTime ?? DateTime.Now;
        if (startTime.Kind == DateTimeKind.Utc)
            startTime = startTime.ToLocalTime();

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

        return Ok(new
        {
            session.Id,
            session.GuestName,
            session.Phone,
            session.TableNumber,
            session.RoomId,
            StartTime = startTime.ToString("yyyy-MM-dd HH:mm:ss"),
            session.PlannedDurationMinutes,
            session.DurationMinutes,
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

        if (actualMinutes < 30)
            actualMinutes = 30;

        var currentPrice = await _pricing.GetCurrentTariffAsync(session.StartTime);
        var totalCost = actualMinutes * currentPrice;

        session.EndTime = endTime;
        session.DurationMinutes = actualMinutes;
        session.TariffRate = currentPrice;
        session.TotalCost = totalCost;
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
            Message = actualMinutes >= 120 ? "Спасибо за долгий визит! 😊" : "Спасибо за посещение! 🍵"
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
    public int DurationMinutes { get; set; } = 30;
}