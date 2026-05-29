using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnticafeBackend.Data;
using AnticafeBackend.Models;
using AnticafeBackend.Services;

namespace AnticafeBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
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
            .ToListAsync();
        return Ok(sessions);
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartSession([FromBody] Session session)
    {
        session.StartTime = DateTime.Now;
        session.IsActive = true;
        session.TotalMinutes = 0;
        session.TotalCost = 0;
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();
        return Ok(new { session.Id, session.StartTime, session.GuestName });
    }

    [HttpPost("end/{id}")]
    public async Task<IActionResult> EndSession(int id)
    {
        var session = await _context.Sessions.FindAsync(id);
        if (session == null) return NotFound(new { error = "Сеанс не найден" });

        session.EndTime = DateTime.Now;
        session.TotalMinutes = (int)(session.EndTime.Value - session.StartTime).TotalMinutes;

        // Используем PricingService для расчета
        session.TotalCost = await _pricing.CalculateCostAsync(session.StartTime, session.EndTime.Value);
        session.IsActive = false;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            session.TotalMinutes,
            session.TotalCost,
            message = $"Сеанс завершен. К оплате: {session.TotalCost} ₽"
        });
    }
}