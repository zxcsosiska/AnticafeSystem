using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnticafeBackend.Data;
using AnticafeBackend.Services;

namespace AnticafeBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TariffController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly PricingService _pricing;

    public TariffController(ApplicationDbContext context, PricingService pricing)
    {
        _context = context;
        _pricing = pricing;
    }

    // GET: api/tariff/current
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentTariff()
    {
        var rate = await _pricing.GetCurrentTariffAsync(DateTime.Now);
        return Ok(new { pricePerMinute = (double)rate });
    }

    // GET: api/tariff/minimum
    [HttpGet("minimum")]
    public async Task<IActionResult> GetMinimumMinutes()
    {
        var now = DateTime.Now;
        var tariff = await _context.Tariffs
            .Where(t => t.IsActive &&
                   (t.DayOfWeek == null || t.DayOfWeek == (int)now.DayOfWeek) &&
                   now.Hour >= t.HourFrom && now.Hour < t.HourTo)
            .FirstOrDefaultAsync();

        return Ok(new { minimumMinutes = tariff?.MinimumMinutes ?? 30 });
    }

    // GET: api/tariff/all
    [HttpGet("all")]
    public async Task<IActionResult> GetAllTariffs()
    {
        var tariffs = await _context.Tariffs
            .Where(t => t.IsActive)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.DayOfWeek,
                t.HourFrom,
                t.HourTo,
                PricePerMinute = (double)t.PricePerMinute,
                t.MinimumMinutes
            })
            .ToListAsync();
        return Ok(tariffs);
    }

    // PUT: api/tariff/update
    [HttpPut("update")]
    public async Task<IActionResult> UpdateTariff([FromBody] TariffUpdateRequest request)
    {
        var tariff = await _context.Tariffs.FindAsync(request.Id);
        if (tariff == null)
            return NotFound();

        tariff.PricePerMinute = request.PricePerMinute;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Тариф обновлён", price = (double)tariff.PricePerMinute });
    }
}

public class TariffUpdateRequest
{
    public int Id { get; set; }
    public decimal PricePerMinute { get; set; }
}