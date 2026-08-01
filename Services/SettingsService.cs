using Microsoft.EntityFrameworkCore;
using Anticafe.Data;
using System.Globalization;

namespace Anticafe.Services;

public class SettingsService : ISettingsService
{
    private readonly ApplicationDbContext _context;

    public SettingsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<string, string>> GetAllSettingsAsync()
    {
        var dict = new Dictionary<string, string>();

        var tariff = await _context.Tariffs.FirstOrDefaultAsync(t => t.IsActive);
        if (tariff != null)
        {
            dict["PricePerMinute"] = tariff.PricePerMinute.ToString(CultureInfo.InvariantCulture);
            dict["MinimumMinutes"] = tariff.MinimumMinutes.ToString();
            dict["TariffName"] = tariff.Name ?? "Стандартный";
        }

        return dict;
    }

    public async Task<(bool success, string error, object? result)> UpdateSettingAsync(string key, string value)
    {
        try
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                return (false, "Key и Value обязательны", null);

            if (key == "PricePerMinute")
            {
                if (!decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                    return (false, "Некорректный формат цены", null);

                if (price <= 0)
                    return (false, "Цена должна быть больше 0", null);

                var tariff = await _context.Tariffs.FirstOrDefaultAsync(t => t.IsActive);
                if (tariff != null)
                {
                    tariff.PricePerMinute = Math.Round(price, 2);
                    await _context.SaveChangesAsync();

                    return (true, "Цена обновлена", new { newPrice = tariff.PricePerMinute });
                }
                return (false, "Тариф не найден", null);
            }

            if (key == "MinimumMinutes")
            {
                if (!int.TryParse(value, out var minutes) || minutes < 30)
                    return (false, "Минимальная длительность должна быть не менее 30 минут", null);

                var tariff = await _context.Tariffs.FirstOrDefaultAsync(t => t.IsActive);
                if (tariff != null)
                {
                    tariff.MinimumMinutes = minutes;
                    await _context.SaveChangesAsync();

                    return (true, "Минимальная длительность обновлена", new { newMinimum = tariff.MinimumMinutes });
                }
                return (false, "Тариф не найден", null);
            }

            return (false, $"Настройка '{key}' не найдена", null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, null);
        }
    }
}