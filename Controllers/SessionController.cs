using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Anticafe.Data;
using Anticafe.Models;

namespace Anticafe.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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
                s.IsActive
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

    [HttpGet("available-tables")]
    public async Task<IActionResult> GetAvailableTables()
    {
        var busy = await _context.Sessions.Where(s => s.IsActive).Select(s => s.TableNumber).ToListAsync();
        var booked = await _context.Bookings.Where(b => b.Status == "active" && b.BookingDate.Date >= DateTime.Now.Date).Select(b => b.TableNumber).ToListAsync();
        var allBusy = busy.Union(booked).Distinct().ToList();

        var available = await _context.Tables
            .Where(t => t.IsActive && !allBusy.Contains(t.TableNumber))
            .Select(t => new { t.Id, t.TableNumber, t.RoomId })
            .ToListAsync();
        return Ok(available);
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartSession([FromBody] StartSessionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.GuestName))
            return BadRequest(new { error = "Введите имя гостя" });

        if (request.DurationMinutes < 30)
            return BadRequest(new { error = "Минимум 30 минут" });

        var table = await _context.Tables
            .FirstOrDefaultAsync(t => t.TableNumber == request.TableNumber && t.RoomId == request.RoomId && t.IsActive);
        if (table == null) return BadRequest(new { error = "Стол не найден" });

        var isBusy = await _context.Sessions.AnyAsync(s => s.IsActive && s.TableNumber == request.TableNumber);
        if (isBusy) return BadRequest(new { error = "Стол занят" });

        var tariff = await _context.Tariffs.FirstOrDefaultAsync(t => t.IsActive);
        var price = tariff?.PricePerMinute ?? 3.5m;
        var totalCost = request.DurationMinutes * price;

        var session = new Session
        {
            GuestName = request.GuestName.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            TableNumber = request.TableNumber,
            RoomId = request.RoomId,
            StartTime = DateTime.Now,
            PlannedDurationMinutes = request.DurationMinutes,
            DurationMinutes = request.DurationMinutes,
            TariffRate = price,
            TotalCost = totalCost,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        return Ok(new { session.Id, session.GuestName, session.TableNumber, session.StartTime, session.TotalCost });
    }

    [HttpPost("end/{id}")]
    public async Task<IActionResult> EndSession(int id)
    {
        var session = await _context.Sessions.FindAsync(id);
        if (session == null) return NotFound(new { error = "Сеанс не найден" });
        if (!session.IsActive) return BadRequest(new { error = "Уже завершён" });

        var actualMinutes = (int)Math.Ceiling((DateTime.Now - session.StartTime).TotalMinutes);
        if (actualMinutes < 30) actualMinutes = 30;

        var tariff = await _context.Tariffs.FirstOrDefaultAsync(t => t.IsActive);
        var price = tariff?.PricePerMinute ?? 3.5m;

        session.EndTime = DateTime.Now;
        session.DurationMinutes = actualMinutes;
        session.TariffRate = price;
        session.TotalCost = actualMinutes * price;
        session.IsActive = false;

        await _context.SaveChangesAsync();

        return Ok(new { session.Id, session.GuestName, ActualDurationMinutes = actualMinutes, session.TotalCost });
    }
}

public class StartSessionRequest
{
    public string GuestName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public int TableNumber { get; set; }
    public int RoomId { get; set; } = 1;
    public int DurationMinutes { get; set; } = 30;
}