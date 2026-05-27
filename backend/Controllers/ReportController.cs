using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnticafeBackend.Data;
using AnticafeBackend.Models;

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
            .Where(s => s.StartTime >= from && s.StartTime <= to && !s.IsActive)
            .ToListAsync();

        var totalRevenue = sessions.Sum(s => s.TotalCost);
        var totalMinutes = sessions.Sum(s => s.TotalMinutes);
        var averageCheck = sessions.Any() ? totalRevenue / sessions.Count : 0;

        var dailyRevenue = sessions
            .GroupBy(s => s.StartTime.Date)
            .Select(g => new { Date = g.Key.ToString("yyyy-MM-dd"), Revenue = g.Sum(s => s.TotalCost), Count = g.Count() })
            .OrderBy(x => x.Date)
            .ToList();

        return Ok(new
        {
            from = from.ToString("yyyy-MM-dd"),
            to = to.ToString("yyyy-MM-dd"),
            totalRevenue,
            totalMinutes,
            averageCheck,
            sessionsCount = sessions.Count,
            dailyRevenue
        });
    }

    [HttpGet("occupancy")]
    public async Task<IActionResult> GetOccupancyReport(DateTime date)
    {
        var sessions = await _context.Sessions
            .Where(s => s.StartTime.Date == date.Date)
            .ToListAsync();

        var hourlyOccupancy = new List<object>();
        for (int hour = 10; hour <= 23; hour++)
        {
            var count = sessions.Count(s => s.StartTime.Hour <= hour &&
                (s.EndTime == null || s.EndTime.Value.Hour >= hour));
            hourlyOccupancy.Add(new { Hour = hour, Guests = count });
        }

        return Ok(hourlyOccupancy);
    }

    [HttpGet("tariffs")]
    public async Task<IActionResult> GetTariffs()
    {
        var tariffs = await _context.Tariffs.Where(t => t.IsActive).ToListAsync();
        return Ok(tariffs);
    }

    [HttpPut("tariffs")]
    public async Task<IActionResult> UpdateTariff([FromBody] Tariff tariff)
    {
        var existing = await _context.Tariffs.FindAsync(tariff.Id);
        if (existing == null)
            return NotFound();

        existing.PricePerMinute = tariff.PricePerMinute;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Тариф обновлён", price = existing.PricePerMinute });
    }
}