using Anticafe.Models.DTOs;

namespace Anticafe.Services;

public interface ISettingsService
{
    Task<Dictionary<string, string>> GetAllSettingsAsync();
    Task<(bool success, string error, object? result)> UpdateSettingAsync(string key, string value);
}