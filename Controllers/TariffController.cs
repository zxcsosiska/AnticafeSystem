using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Anticafe.Data;
using Anticafe.Models;

namespace Anticafe.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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
        var tariff = await _context.Tariffs
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.Priority)
            .FirstOrDefaultAsync();

        if (tariff == null)
        {
            tariff = new Tariff
            {
                Name = "Стандартный",
                PricePerMinute = 3.5m,
                MinimumMinutes = 30,
                IsActive = true,
                Priority = 0,
                CreatedAt = DateTime.Now
            };
            _context.Tariffs.Add(tariff);
            await _context.SaveChangesAsync();
        }

        return Ok(new
        {
            currentPricePerMinute = tariff.PricePerMinute,
            minimumMinutes = tariff.MinimumMinutes,
            tariffName = tariff.Name
        });
    }
}