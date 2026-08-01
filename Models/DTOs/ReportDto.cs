namespace Anticafe.Models.DTOs;

public class RevenueReportDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalMinutes { get; set; }
    public int SessionsCount { get; set; }
    public decimal AverageCheck { get; set; }
    public decimal MaxCheck { get; set; }
    public decimal MinCheck { get; set; }
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
}

public class DetailedReportDto
{
    public int Total { get; set; }
    public List<SessionDto> Sessions { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public int TotalMinutes { get; set; }
}