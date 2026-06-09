using Microsoft.EntityFrameworkCore;
using AnticafeBackend.Data;

namespace AnticafeBackend.Services;

public class PricingService
{
    private readonly ApplicationDbContext _context;

    public PricingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<decimal> GetCurrentTariffAsync(DateTime time)
    {
        // Сначала ищем тариф, который подходит по времени и дню недели
        var tariff = await _context.Tariffs
            .Where(t => t.IsActive &&
                   (t.DayOfWeek == null || t.DayOfWeek == (int)time.DayOfWeek) &&
                   (t.HourFrom == 0 && t.HourTo == 0 || // Если часы не заданы (0-0), то подходит для любого времени
                    time.Hour >= t.HourFrom && time.Hour < t.HourTo))
            .OrderByDescending(t => t.Priority) // Сортируем по приоритету
            .FirstOrDefaultAsync();

        // Если нашли тариф - возвращаем его цену
        if (tariff != null)
            return tariff.PricePerMinute;

        // Если нет активных тарифов с подходящими условиями, берем первый активный
        var defaultTariff = await _context.Tariffs
            .FirstOrDefaultAsync(t => t.IsActive);

        return defaultTariff?.PricePerMinute ?? 3.5m;
    }

    public async Task<int> GetMinimumMinutesAsync(DateTime time)
    {
        var tariff = await _context.Tariffs
            .Where(t => t.IsActive &&
                   (t.DayOfWeek == null || t.DayOfWeek == (int)time.DayOfWeek) &&
                   (t.HourFrom == 0 && t.HourTo == 0 ||
                    time.Hour >= t.HourFrom && time.Hour < t.HourTo))
            .OrderByDescending(t => t.Priority)
            .FirstOrDefaultAsync();

        if (tariff != null)
            return tariff.MinimumMinutes;

        var defaultTariff = await _context.Tariffs
            .FirstOrDefaultAsync(t => t.IsActive);

        return defaultTariff?.MinimumMinutes ?? 30;
    }

    public async Task<decimal> CalculateCostAsync(DateTime start, DateTime end)
    {
        var minutes = (int)Math.Ceiling((end - start).TotalMinutes);
        if (minutes <= 0) return 0;

        var minimumMinutes = await GetMinimumMinutesAsync(start);
        if (minutes < minimumMinutes) minutes = minimumMinutes;

        var tariff = await GetCurrentTariffAsync(start);
        decimal total = minutes * tariff;

        return Math.Round(total, 2);
    }
}