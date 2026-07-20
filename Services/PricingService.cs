using Microsoft.EntityFrameworkCore;
using Anticafe.Data;

namespace Anticafe.Services;

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
                (t.HourFrom == 0 && t.HourTo == 0 || time.Hour >= t.HourFrom && time.Hour < t.HourTo))
            .OrderByDescending(t => t.Priority)
            .FirstOrDefaultAsync();

        return tariff?.PricePerMinute ?? 3.5m;
    }

    public async Task<int> GetMinimumMinutesAsync(DateTime time)
    {
        var tariff = await _context.Tariffs
            .Where(t => t.IsActive &&
                (t.DayOfWeek == null || t.DayOfWeek == (int)time.DayOfWeek) &&
                (t.HourFrom == 0 && t.HourTo == 0 || time.Hour >= t.HourFrom && time.Hour < t.HourTo))
            .OrderByDescending(t => t.Priority)
            .FirstOrDefaultAsync();

        return tariff?.MinimumMinutes ?? 30;
    }
}