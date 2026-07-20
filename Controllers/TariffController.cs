using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Anticafe.Data;
using Anticafe.Models; // <-- ВАЖНО: добавить эту строку!
using Anticafe.Services;

namespace Anticafe.Controllers;

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
        var now = DateTime.Now;
        var currentPrice = await _pricing.GetCurrentTariffAsync(now);
        var minimumMinutes = await _pricing.GetMinimumMinutesAsync(now);

        var allTariffs = await _context.Tariffs
            .Where(t => t.IsActive)
            .OrderBy(t => t.Priority)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.PricePerMinute,
                t.DayOfWeek,
                DayName = t.DayOfWeek.HasValue ? ((DayOfWeek)t.DayOfWeek).ToString() : "Все дни",
                t.HourFrom,
                t.HourTo,
                TimeRange = t.HourFrom == 0 && t.HourTo == 0 ? "Круглосуточно" : $"{t.HourFrom:00}:00 - {t.HourTo:00}:00",
                t.MinimumMinutes,
                t.IsActive,
                t.Priority,
                IsActiveNow = IsTariffActiveNow(t, now)
            })
            .ToListAsync();

        return Ok(new
        {
            currentPricePerMinute = currentPrice,
            minimumMinutes = minimumMinutes,
            currentTime = now.ToString("HH:mm"),
            currentDay = now.DayOfWeek.ToString(),
            activeTariffsCount = allTariffs.Count(t => t.IsActive),
            activeTariffs = allTariffs,
            message = "Тарифы загружены успешно"
        });
    }

    [HttpGet("all")]
    public async Task<IActionResult> GetAllTariffs()
    {
        var tariffs = await _context.Tariffs
            .OrderByDescending(t => t.IsActive)
            .ThenBy(t => t.Priority)
            .Select(t => new
            {
                t.Id,
                t.Name,
                t.PricePerMinute,
                t.DayOfWeek,
                t.HourFrom,
                t.HourTo,
                t.MinimumMinutes,
                t.IsActive,
                t.Priority,
                t.CreatedAt
            })
            .ToListAsync();

        return Ok(tariffs);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTariff([FromBody] CreateTariffRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { success = false, error = "Введите название тарифа" });

            if (request.PricePerMinute <= 0)
                return BadRequest(new { success = false, error = "Цена должна быть больше 0" });

            if (request.MinimumMinutes < 30)
                return BadRequest(new { success = false, error = "Минимальная длительность должна быть не менее 30 минут" });

            if (request.HourFrom < 0 || request.HourFrom > 23 || request.HourTo < 0 || request.HourTo > 23)
                return BadRequest(new { success = false, error = "Некорректный диапазон часов" });

            var tariff = new Tariff
            {
                Name = request.Name.Trim(),
                PricePerMinute = Math.Round(request.PricePerMinute, 2),
                DayOfWeek = request.DayOfWeek,
                HourFrom = request.HourFrom,
                HourTo = request.HourTo,
                MinimumMinutes = request.MinimumMinutes,
                IsActive = request.IsActive,
                Priority = request.Priority,
                CreatedAt = DateTime.Now
            };

            _context.Tariffs.Add(tariff);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"Тариф '{tariff.Name}' создан",
                tariff = new
                {
                    tariff.Id,
                    tariff.Name,
                    tariff.PricePerMinute,
                    tariff.DayOfWeek,
                    tariff.HourFrom,
                    tariff.HourTo,
                    tariff.MinimumMinutes,
                    tariff.IsActive,
                    tariff.Priority
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateTariff(int id, [FromBody] CreateTariffRequest request)
    {
        try
        {
            var tariff = await _context.Tariffs.FindAsync(id);
            if (tariff == null)
                return NotFound(new { success = false, error = "Тариф не найден" });

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { success = false, error = "Введите название тарифа" });

            if (request.PricePerMinute <= 0)
                return BadRequest(new { success = false, error = "Цена должна быть больше 0" });

            if (request.MinimumMinutes < 30)
                return BadRequest(new { success = false, error = "Минимальная длительность должна быть не менее 30 минут" });

            tariff.Name = request.Name.Trim();
            tariff.PricePerMinute = Math.Round(request.PricePerMinute, 2);
            tariff.DayOfWeek = request.DayOfWeek;
            tariff.HourFrom = request.HourFrom;
            tariff.HourTo = request.HourTo;
            tariff.MinimumMinutes = request.MinimumMinutes;
            tariff.IsActive = request.IsActive;
            tariff.Priority = request.Priority;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"Тариф '{tariff.Name}' обновлён",
                tariff = new
                {
                    tariff.Id,
                    tariff.Name,
                    tariff.PricePerMinute,
                    tariff.DayOfWeek,
                    tariff.HourFrom,
                    tariff.HourTo,
                    tariff.MinimumMinutes,
                    tariff.IsActive,
                    tariff.Priority
                }
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTariff(int id)
    {
        try
        {
            var tariff = await _context.Tariffs.FindAsync(id);
            if (tariff == null)
                return NotFound(new { success = false, error = "Тариф не найден" });

            _context.Tariffs.Remove(tariff);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"Тариф '{tariff.Name}' удалён" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    private bool IsTariffActiveNow(Tariff tariff, DateTime time)
    {
        if (!tariff.IsActive) return false;
        if (tariff.DayOfWeek.HasValue && tariff.DayOfWeek != (int)time.DayOfWeek) return false;
        if (tariff.HourFrom == 0 && tariff.HourTo == 0) return true;
        return time.Hour >= tariff.HourFrom && time.Hour < tariff.HourTo;
    }
}

public class CreateTariffRequest
{
    public string Name { get; set; } = "Новый тариф";
    public decimal PricePerMinute { get; set; } = 3.5m;
    public int? DayOfWeek { get; set; }
    public int HourFrom { get; set; } = 0;
    public int HourTo { get; set; } = 0;
    public int MinimumMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;
    public int Priority { get; set; } = 0;
}