using Microsoft.EntityFrameworkCore;
using Anticafe.Data;
using Anticafe.Models;
using Anticafe.Models.DTOs;

namespace Anticafe.Services;

public class TariffService : ITariffService
{
    private readonly ApplicationDbContext _context;
    private readonly PricingService _pricing;

    public TariffService(ApplicationDbContext context, PricingService pricing)
    {
        _context = context;
        _pricing = pricing;
    }

    public async Task<TariffResponseDto> GetCurrentTariffAsync()
    {
        var now = DateTime.Now;
        var currentPrice = await _pricing.GetCurrentTariffAsync(now);
        var minimumMinutes = await _pricing.GetMinimumMinutesAsync(now);

        var allTariffs = await _context.Tariffs
            .Where(t => t.IsActive)
            .OrderBy(t => t.Priority)
            .Select(t => new TariffDto
            {
                Id = t.Id,
                Name = t.Name ?? string.Empty,
                PricePerMinute = t.PricePerMinute,
                DayOfWeek = t.DayOfWeek,
                DayName = t.DayOfWeek.HasValue ? ((DayOfWeek)t.DayOfWeek).ToString() : "Все дни",
                HourFrom = t.HourFrom,
                HourTo = t.HourTo,
                TimeRange = t.HourFrom == 0 && t.HourTo == 0 ? "Круглосуточно" : $"{t.HourFrom:00}:00 - {t.HourTo:00}:00",
                MinimumMinutes = t.MinimumMinutes,
                IsActive = t.IsActive,
                Priority = t.Priority,
                IsActiveNow = IsTariffActiveNow(t, now),
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return new TariffResponseDto
        {
            CurrentPricePerMinute = currentPrice,
            MinimumMinutes = minimumMinutes,
            CurrentTime = now.ToString("HH:mm"),
            CurrentDay = now.DayOfWeek.ToString(),
            ActiveTariffsCount = allTariffs.Count(t => t.IsActive),
            ActiveTariffs = allTariffs
        };
    }

    public async Task<List<TariffDto>> GetAllTariffsAsync()
    {
        return await _context.Tariffs
            .OrderByDescending(t => t.IsActive)
            .ThenBy(t => t.Priority)
            .Select(t => new TariffDto
            {
                Id = t.Id,
                Name = t.Name ?? string.Empty,
                PricePerMinute = t.PricePerMinute,
                DayOfWeek = t.DayOfWeek,
                DayName = t.DayOfWeek.HasValue ? ((DayOfWeek)t.DayOfWeek).ToString() : "Все дни",
                HourFrom = t.HourFrom,
                HourTo = t.HourTo,
                TimeRange = t.HourFrom == 0 && t.HourTo == 0 ? "Круглосуточно" : $"{t.HourFrom:00}:00 - {t.HourTo:00}:00",
                MinimumMinutes = t.MinimumMinutes,
                IsActive = t.IsActive,
                Priority = t.Priority,
                IsActiveNow = false,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<(bool success, string error, TariffDto? tariff)> CreateTariffAsync(CreateTariffDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return (false, "Введите название тарифа", null);

            if (request.PricePerMinute <= 0)
                return (false, "Цена должна быть больше 0", null);

            if (request.MinimumMinutes < 30)
                return (false, "Минимальная длительность должна быть не менее 30 минут", null);

            if (request.HourFrom < 0 || request.HourFrom > 23 || request.HourTo < 0 || request.HourTo > 23)
                return (false, "Некорректный диапазон часов", null);

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

            var dto = new TariffDto
            {
                Id = tariff.Id,
                Name = tariff.Name ?? string.Empty,
                PricePerMinute = tariff.PricePerMinute,
                DayOfWeek = tariff.DayOfWeek,
                DayName = tariff.DayOfWeek.HasValue ? ((DayOfWeek)tariff.DayOfWeek).ToString() : "Все дни",
                HourFrom = tariff.HourFrom,
                HourTo = tariff.HourTo,
                TimeRange = tariff.HourFrom == 0 && tariff.HourTo == 0 ? "Круглосуточно" : $"{tariff.HourFrom:00}:00 - {tariff.HourTo:00}:00",
                MinimumMinutes = tariff.MinimumMinutes,
                IsActive = tariff.IsActive,
                Priority = tariff.Priority,
                IsActiveNow = false,
                CreatedAt = tariff.CreatedAt
            };

            return (true, string.Empty, dto);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    public async Task<(bool success, string error, TariffDto? tariff)> UpdateTariffAsync(int id, CreateTariffDto request)
    {
        try
        {
            var tariff = await _context.Tariffs.FindAsync(id);
            if (tariff == null)
                return (false, "Тариф не найден", null);

            if (string.IsNullOrWhiteSpace(request.Name))
                return (false, "Введите название тарифа", null);

            if (request.PricePerMinute <= 0)
                return (false, "Цена должна быть больше 0", null);

            if (request.MinimumMinutes < 30)
                return (false, "Минимальная длительность должна быть не менее 30 минут", null);

            tariff.Name = request.Name.Trim();
            tariff.PricePerMinute = Math.Round(request.PricePerMinute, 2);
            tariff.DayOfWeek = request.DayOfWeek;
            tariff.HourFrom = request.HourFrom;
            tariff.HourTo = request.HourTo;
            tariff.MinimumMinutes = request.MinimumMinutes;
            tariff.IsActive = request.IsActive;
            tariff.Priority = request.Priority;

            await _context.SaveChangesAsync();

            var dto = new TariffDto
            {
                Id = tariff.Id,
                Name = tariff.Name ?? string.Empty,
                PricePerMinute = tariff.PricePerMinute,
                DayOfWeek = tariff.DayOfWeek,
                DayName = tariff.DayOfWeek.HasValue ? ((DayOfWeek)tariff.DayOfWeek).ToString() : "Все дни",
                HourFrom = tariff.HourFrom,
                HourTo = tariff.HourTo,
                TimeRange = tariff.HourFrom == 0 && tariff.HourTo == 0 ? "Круглосуточно" : $"{tariff.HourFrom:00}:00 - {tariff.HourTo:00}:00",
                MinimumMinutes = tariff.MinimumMinutes,
                IsActive = tariff.IsActive,
                Priority = tariff.Priority,
                IsActiveNow = false,
                CreatedAt = tariff.CreatedAt
            };

            return (true, string.Empty, dto);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }

    public async Task<(bool success, string error)> DeleteTariffAsync(int id)
    {
        try
        {
            var tariff = await _context.Tariffs.FindAsync(id);
            if (tariff == null)
                return (false, "Тариф не найден");

            _context.Tariffs.Remove(tariff);
            await _context.SaveChangesAsync();

            return (true, string.Empty);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public async Task<decimal> GetPricePerMinuteAsync(DateTime time)
    {
        return await _pricing.GetCurrentTariffAsync(time);
    }

    public async Task<int> GetMinimumMinutesAsync(DateTime time)
    {
        return await _pricing.GetMinimumMinutesAsync(time);
    }

    private bool IsTariffActiveNow(Tariff tariff, DateTime time)
    {
        if (!tariff.IsActive) return false;
        if (tariff.DayOfWeek.HasValue && tariff.DayOfWeek != (int)time.DayOfWeek) return false;
        if (tariff.HourFrom == 0 && tariff.HourTo == 0) return true;
        return time.Hour >= tariff.HourFrom && time.Hour < tariff.HourTo;
    }
}

public class TariffResponseDto
{
    public decimal CurrentPricePerMinute { get; set; }
    public int MinimumMinutes { get; set; }
    public string CurrentTime { get; set; } = string.Empty;
    public string CurrentDay { get; set; } = string.Empty;
    public int ActiveTariffsCount { get; set; }
    public List<TariffDto> ActiveTariffs { get; set; } = new();
}