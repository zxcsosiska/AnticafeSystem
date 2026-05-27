using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnticafeBackend.Data;
using AnticafeBackend.Models;

namespace AnticafeBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TariffController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public TariffController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentTariff()
    {
        var now = DateTime.Now;
        var tariff = await _context.Tariffs
            .Where(t => t.IsActive &&
                   (t.DayOfWeek == null || t.DayOfWeek == (int)now.DayOfWeek) &&
                   now.Hour >= t.HourFrom && now.Hour < t.HourTo)
            .FirstOrDefaultAsync();

        return Ok(new { pricePerMinute = tariff?.PricePerMinute ?? 3.5m });
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllTariffs()
    {
        var tariffs = await _context.Tariffs.ToListAsync();
        return Ok(tariffs);
    }

    [HttpPost("update")]
    public async Task<IActionResult> UpdateTariff([FromBody] TariffUpdateRequest request)
    {
        var tariff = await _context.Tariffs.FindAsync(request.Id);
        if (tariff == null)
            return NotFound();

        tariff.PricePerMinute = request.PricePerMinute;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Тариф обновлён" });
    }
}

public class TariffUpdateRequest
{
    public int Id { get; set; }
    public decimal PricePerMinute { get; set; }
}