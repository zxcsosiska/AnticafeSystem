using Anticafe.Models.DTOs;

namespace Anticafe.Services;

public interface ITariffService
{
    Task<TariffResponseDto> GetCurrentTariffAsync();
    Task<List<TariffDto>> GetAllTariffsAsync();
    Task<(bool success, string error, TariffDto? tariff)> CreateTariffAsync(CreateTariffDto request);
    Task<(bool success, string error, TariffDto? tariff)> UpdateTariffAsync(int id, CreateTariffDto request);
    Task<(bool success, string error)> DeleteTariffAsync(int id);
    Task<decimal> GetPricePerMinuteAsync(DateTime time);
    Task<int> GetMinimumMinutesAsync(DateTime time);
}