using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Anticafe.Data;
using Anticafe.Models;

namespace Anticafe.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SettingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSetting([FromBody] UpdateSettingRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Key))
                return BadRequest(new { success = false, error = "Key обязателен" });

            var tariff = await _context.Tariffs.FirstOrDefaultAsync(t => t.IsActive);
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

            if (request.Key == "PricePerMinute")
            {
                if (!decimal.TryParse(request.Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price))
                    return BadRequest(new { success = false, error = "Неверный формат" });

                if (price <= 0)
                    return BadRequest(new { success = false, error = "Цена > 0" });

                tariff.PricePerMinute = Math.Round(price, 2);
                await _context.SaveChangesAsync();
                return Ok(new { success = true, newPrice = tariff.PricePerMinute });
            }

            if (request.Key == "MinimumMinutes")
            {
                if (!int.TryParse(request.Value, out var minutes) || minutes < 30)
                    return BadRequest(new { success = false, error = "Минимум 30 минут" });

                tariff.MinimumMinutes = minutes;
                await _context.SaveChangesAsync();
                return Ok(new { success = true, newMinimum = tariff.MinimumMinutes });
            }

            return BadRequest(new { success = false, error = "Настройка не найдена" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}

public class UpdateSettingRequest
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}