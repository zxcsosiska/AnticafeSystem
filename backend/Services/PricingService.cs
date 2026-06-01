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
        var tariff = await _context.Tariffs
            .Where(t => t.IsActive &&
                   (t.DayOfWeek == null || t.DayOfWeek == (int)time.DayOfWeek) &&
                   time.Hour >= t.HourFrom && time.Hour < t.HourTo)
            .FirstOrDefaultAsync();

        return tariff?.PricePerMinute ?? 3.5m;
    }

    public async Task<int> GetMinimumMinutesAsync(DateTime time)
    {
        var tariff = await _context.Tariffs
            .Where(t => t.IsActive &&
                   (t.DayOfWeek == null || t.DayOfWeek == (int)time.DayOfWeek) &&
                   time.Hour >= t.HourFrom && time.Hour < t.HourTo)
            .FirstOrDefaultAsync();

        return tariff?.MinimumMinutes ?? 30;
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