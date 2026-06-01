using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnticafeBackend.Data;

namespace AnticafeBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReportController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenueReport(DateTime from, DateTime to)
    {
        var sessions = await _context.Sessions
            .Where(s => !s.IsActive && s.EndTime != null)
            .ToListAsync();

        sessions = sessions.Where(s => s.EndTime!.Value.Date >= from.Date && s.EndTime.Value.Date <= to.Date).ToList();

        if (sessions.Count == 0)
        {
            return Ok(new
            {
                totalRevenue = 0,
                totalMinutes = 0,
                sessionsCount = 0
            });
        }

        decimal totalRevenue = 0;
        int totalMinutes = 0;

        foreach (var s in sessions)
        {
            totalRevenue += s.TotalCost;
            totalMinutes += s.DurationMinutes;
        }

        return Ok(new
        {
            totalRevenue = totalRevenue,
            totalMinutes = totalMinutes,
            sessionsCount = sessions.Count,
            averageCheck = totalRevenue / sessions.Count
        });
    }
}