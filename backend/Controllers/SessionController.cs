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
        return Ok(sessions);
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartSession([FromBody] Session session)
    {
        session.StartTime = DateTime.Now;
        session.IsActive = true;
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();
        return Ok(new { session.Id, session.StartTime, session.GuestName });
    }

    [HttpPost("end/{id}")]
    public async Task<IActionResult> EndSession(int id)
    {
        var session = await _context.Sessions.FindAsync(id);
        if (session == null) return NotFound();

        session.EndTime = DateTime.Now;
        session.TotalMinutes = (int)(session.EndTime.Value - session.StartTime).TotalMinutes;
        session.TotalCost = session.TotalMinutes * 3.5m;
        session.IsActive = false;
        await _context.SaveChangesAsync();

        return Ok(new { session.TotalMinutes, session.TotalCost });
    }
}