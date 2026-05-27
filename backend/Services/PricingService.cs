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

    public async Task<decimal> CalculateCostAsync(DateTime start, DateTime end)
    {
        var minutes = (int)Math.Ceiling((end - start).TotalMinutes);
        if (minutes <= 0) return 0;

        var tariff = await GetTariffForTimeAsync(start);
        decimal baseCost = minutes * tariff;

        decimal discount = await ApplyPromotionsAsync(start, minutes, baseCost);

        return Math.Max(0, baseCost - discount);
    }

    private async Task<decimal> GetTariffForTimeAsync(DateTime time)
    {
        var tariff = await _context.Tariffs
            .Where(t => t.IsActive &&
                   (t.DayOfWeek == null || t.DayOfWeek == (int)time.DayOfWeek) &&
                   time.Hour >= t.HourFrom && time.Hour < t.HourTo)
            .FirstOrDefaultAsync();

        return tariff?.PricePerMinute ?? 3.5m;
    }

    private async Task<decimal> ApplyPromotionsAsync(DateTime start, int minutes, decimal baseCost)
    {
        var promotions = await _context.Promotions
            .Where(p => p.IsActive && p.StartDate <= start && p.EndDate >= start)
            .ToListAsync();

        decimal totalDiscount = 0;
        decimal pricePerMinute = baseCost / minutes;

        foreach (var promo in promotions)
        {
            if (promo.Type == "first_hour_free")
            {
                int freeMinutes = Math.Min(60, minutes);
                totalDiscount += freeMinutes * pricePerMinute;
            }
            else if (promo.Type == "discount_percent" && promo.Value.HasValue)
            {
                totalDiscount += baseCost * (promo.Value.Value / 100);
            }
        }

        return totalDiscount;
    }
}