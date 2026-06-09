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

    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentTariff()
    {
        // Используем PricingService для получения актуальной цены
        var currentPrice = await _pricing.GetCurrentTariffAsync(DateTime.Now);

        // Также возвращаем все тарифы для отладки
        var allTariffs = await _context.Tariffs
            .Where(t => t.IsActive)
            .ToListAsync();

        return Ok(new
        {
            currentPricePerMinute = currentPrice,
            activeTariffs = allTariffs.Select(t => new
            {
                t.Id,
                t.Name,
                t.PricePerMinute,
                t.DayOfWeek,
                t.HourFrom,
                t.HourTo,
                t.IsActive
            })
        });
    }
}