using Microsoft.EntityFrameworkCore;
using Anticafe.Data;
using Anticafe.Models.DTOs;

namespace Anticafe.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RevenueReportDto> GetRevenueReportAsync(DateTime from, DateTime to)
    {
        var fromDate = from.Date;
        var toDate = to.Date.AddDays(1).AddSeconds(-1);

        var sessions = await _context.Sessions
            .Where(s => !s.IsActive && s.EndTime != null && s.EndTime.Value >= fromDate && s.EndTime.Value <= toDate)
            .ToListAsync();

        if (sessions.Count == 0)
        {
            return new RevenueReportDto
            {
                TotalRevenue = 0,
                TotalMinutes = 0,
                SessionsCount = 0,
                AverageCheck = 0,
                MaxCheck = 0,
                MinCheck = 0,
                From = fromDate.ToString("yyyy-MM-dd"),
                To = toDate.ToString("yyyy-MM-dd")
            };
        }

        var totalRevenue = sessions.Sum(s => s.TotalCost);
        var totalMinutes = sessions.Sum(s => s.DurationMinutes);
        var averageCheck = totalRevenue / sessions.Count;

        return new RevenueReportDto
        {
            TotalRevenue = Math.Round(totalRevenue, 2),
            TotalMinutes = totalMinutes,
            SessionsCount = sessions.Count,
            AverageCheck = Math.Round(averageCheck, 2),
            MaxCheck = Math.Round(sessions.Max(s => s.TotalCost), 2),
            MinCheck = Math.Round(sessions.Min(s => s.TotalCost), 2),
            From = fromDate.ToString("yyyy-MM-dd"),
            To = toDate.ToString("yyyy-MM-dd")
        };
    }

    public async Task<DetailedReportDto> GetDetailedReportAsync(DateTime from, DateTime to)
    {
        var fromDate = from.Date;
        var toDate = to.Date.AddDays(1).AddSeconds(-1);

        var sessions = await _context.Sessions
            .Where(s => !s.IsActive && s.EndTime != null && s.EndTime.Value >= fromDate && s.EndTime.Value <= toDate)
            .Include(s => s.Room)
            .OrderByDescending(s => s.EndTime)
            .Select(s => new SessionDto
            {
                Id = s.Id,
                GuestName = s.GuestName ?? string.Empty,
                Phone = s.Phone,
                TableNumber = s.TableNumber,
                RoomId = s.RoomId,
                RoomName = s.Room != null ? s.Room.Name : string.Empty,
                StartTime = s.StartTime,
                EndTime = s.EndTime,
                PlannedDurationMinutes = s.PlannedDurationMinutes,
                DurationMinutes = s.DurationMinutes,
                TariffRate = s.TariffRate,
                TotalCost = s.TotalCost,
                IsActive = s.IsActive,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return new DetailedReportDto
        {
            Total = sessions.Count,
            Sessions = sessions,
            TotalRevenue = sessions.Sum(s => s.TotalCost),
            TotalMinutes = sessions.Sum(s => s.DurationMinutes)
        };
    }

    public async Task<RevenueReportDto> GetTodayReportAsync()
    {
        var today = DateTime.Now.Date;
        return await GetRevenueReportAsync(today, today);
    }

    public async Task<RevenueReportDto> GetWeeklyReportAsync()
    {
        var end = DateTime.Now.Date;
        var start = end.AddDays(-7);
        return await GetRevenueReportAsync(start, end);
    }

    public async Task<RevenueReportDto> GetMonthlyReportAsync()
    {
        var end = DateTime.Now.Date;
        var start = new DateTime(end.Year, end.Month, 1);
        return await GetRevenueReportAsync(start, end);
    }
}