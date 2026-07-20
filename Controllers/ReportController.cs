using Microsoft.AspNetCore.Authorization; // ДОБАВИТЬ!
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Anticafe.Data;

namespace Anticafe.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // ТРЕБУЕТ АВТОРИЗАЦИИ
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
        var fromDate = from.Date;
        var toDate = to.Date.AddDays(1).AddSeconds(-1);

        var sessions = await _context.Sessions
            .Where(s => !s.IsActive && s.EndTime != null && s.EndTime.Value >= fromDate && s.EndTime.Value <= toDate)
            .ToListAsync();

        if (sessions.Count == 0)
        {
            return Ok(new
            {
                totalRevenue = 0m,
                totalMinutes = 0,
                sessionsCount = 0,
                averageCheck = 0m,
                from = fromDate.ToString("yyyy-MM-dd"),
                to = toDate.ToString("yyyy-MM-dd")
            });
        }

        var totalRevenue = sessions.Sum(s => s.TotalCost);
        var totalMinutes = sessions.Sum(s => s.DurationMinutes);
        var averageCheck = totalRevenue / sessions.Count;

        return Ok(new
        {
            totalRevenue = Math.Round(totalRevenue, 2),
            totalMinutes = totalMinutes,
            sessionsCount = sessions.Count,
            averageCheck = Math.Round(averageCheck, 2),
            from = fromDate.ToString("yyyy-MM-dd"),
            to = toDate.ToString("yyyy-MM-dd"),
            maxCheck = Math.Round(sessions.Max(s => s.TotalCost), 2),
            minCheck = Math.Round(sessions.Min(s => s.TotalCost), 2)
        });
    }

    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailedReport(DateTime from, DateTime to)
    {
        var fromDate = from.Date;
        var toDate = to.Date.AddDays(1).AddSeconds(-1);

        var sessions = await _context.Sessions
            .Where(s => !s.IsActive && s.EndTime != null && s.EndTime.Value >= fromDate && s.EndTime.Value <= toDate)
            .OrderByDescending(s => s.EndTime)
            .Select(s => new
            {
                s.Id,
                s.GuestName,
                s.TableNumber,
                s.RoomId,
                s.StartTime,
                s.EndTime,
                s.DurationMinutes,
                s.TariffRate,
                s.TotalCost,
                s.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            total = sessions.Count,
            sessions = sessions,
            totalRevenue = sessions.Sum(s => s.TotalCost),
            totalMinutes = sessions.Sum(s => s.DurationMinutes)
        });
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetTodayReport()
    {
        var today = DateTime.Now.Date;
        return await GetRevenueReport(today, today);
    }

    [HttpGet("weekly")]
    public async Task<IActionResult> GetWeeklyReport()
    {
        var end = DateTime.Now.Date;
        var start = end.AddDays(-7);
        return await GetRevenueReport(start, end);
    }

    [HttpGet("monthly")]
    public async Task<IActionResult> GetMonthlyReport()
    {
        var end = DateTime.Now.Date;
        var start = new DateTime(end.Year, end.Month, 1);
        return await GetRevenueReport(start, end);
    }
}