using Anticafe.Models.DTOs;

namespace Anticafe.Services;

public interface IReportService
{
    Task<RevenueReportDto> GetRevenueReportAsync(DateTime from, DateTime to);
    Task<DetailedReportDto> GetDetailedReportAsync(DateTime from, DateTime to);
    Task<RevenueReportDto> GetTodayReportAsync();
    Task<RevenueReportDto> GetWeeklyReportAsync();
    Task<RevenueReportDto> GetMonthlyReportAsync();
}