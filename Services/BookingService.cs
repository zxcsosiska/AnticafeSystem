using Microsoft.EntityFrameworkCore;
using Anticafe.Data;
using Anticafe.Models;

namespace Anticafe.Services;

public class BookingService
{
    private readonly ApplicationDbContext _context;

    public BookingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<BookingDto>> GetActiveBookingsAsync()
    {
        return await _context.Bookings
            .Where(b => b.Status == "active" && b.BookingDate >= DateTime.Now.Date)
            .OrderBy(b => b.BookingDate)
            .ThenBy(b => b.StartTime)
            .Select(b => new BookingDto
            {
                Id = b.Id,
                GuestName = b.GuestName ?? string.Empty,
                Phone = b.Phone ?? string.Empty,
                TableNumber = b.TableNumber,
                RoomId = b.RoomId,
                BookingDate = b.BookingDate,
                StartTime = b.StartTime ?? string.Empty,
                EndTime = b.EndTime,
                DurationMinutes = b.DurationMinutes,
                Status = b.Status ?? string.Empty
            })
            .ToListAsync();
    }

    public async Task<(bool success, string error, BookingDto? booking)> CreateBookingAsync(CreateBookingRequest request)
    {
        // Валидация
        if (string.IsNullOrWhiteSpace(request.GuestName))
            return (false, "Введите имя гостя", null);

        // Телефон не обязательный - пропускаем если пустой
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            // Базовая проверка формата телефона (дублируем для безопасности)
            var cleaned = System.Text.RegularExpressions.Regex.Replace(request.Phone, @"[\s\-\(\)]", "");
            if (!System.Text.RegularExpressions.Regex.IsMatch(cleaned, @"^\+?[\d]+$"))
                return (false, "Телефон должен содержать только цифры и +", null);

            var digits = cleaned.Replace("+", "");
            if (digits.Length < 10 || digits.Length > 15)
                return (false, "Введите корректный номер (10-15 цифр)", null);
        }

        if (request.DurationMinutes < 30)
            return (false, "Минимальная длительность - 30 минут", null);

        if (request.TableNumber <= 0)
            return (false, "Выберите стол", null);

        if (request.RoomId <= 0)
            return (false, "Выберите зал", null);

        // Проверка существования стола
        var table = await _context.Tables
            .FirstOrDefaultAsync(t => t.TableNumber == request.TableNumber && t.RoomId == request.RoomId && t.IsActive);

        if (table == null)
            return (false, "Стол не найден в выбранном зале", null);

        // Проверка пересечения бронирований
        var endTime = TimeSpan.Parse(request.StartTime).Add(TimeSpan.FromMinutes(request.DurationMinutes));
        var endTimeStr = endTime.ToString(@"hh\:mm");

        var isBooked = await _context.Bookings
            .AnyAsync(b => b.TableNumber == request.TableNumber &&
                          b.BookingDate.Date == request.BookingDate.Date &&
                          b.Status == "active" &&
                          b.StartTime.CompareTo(endTimeStr) < 0 &&
                          (b.EndTime == null || b.EndTime.CompareTo(request.StartTime) > 0));

        if (isBooked)
            return (false, "Стол уже забронирован на это время", null);

        // Проверка занятости стола
        var isBusy = await _context.Sessions
            .AnyAsync(s => s.IsActive && s.TableNumber == request.TableNumber);

        if (isBusy)
            return (false, "Стол сейчас занят, бронирование невозможно", null);

        var booking = new Booking
        {
            GuestName = request.GuestName.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? "Не указан" : request.Phone.Trim(),
            TableNumber = request.TableNumber,
            RoomId = request.RoomId,
            BookingDate = request.BookingDate.Date,
            StartTime = request.StartTime,
            EndTime = endTimeStr,
            DurationMinutes = request.DurationMinutes,
            Status = "active",
            CreatedAt = DateTime.Now
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        var dto = new BookingDto
        {
            Id = booking.Id,
            GuestName = booking.GuestName ?? string.Empty,
            Phone = booking.Phone ?? string.Empty,
            TableNumber = booking.TableNumber,
            RoomId = booking.RoomId,
            BookingDate = booking.BookingDate,
            StartTime = booking.StartTime ?? string.Empty,
            EndTime = booking.EndTime,
            DurationMinutes = booking.DurationMinutes,
            Status = booking.Status ?? string.Empty
        };

        return (true, string.Empty, dto);
    }

    public async Task<(bool success, string error)> CancelBookingAsync(int id)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null)
            return (false, "Бронирование не найдено");

        if (booking.Status != "active")
            return (false, "Бронирование уже отменено");

        booking.Status = "cancelled";
        await _context.SaveChangesAsync();

        return (true, string.Empty);
    }
}

// DTO для сервиса (входящий запрос)
public class CreateBookingRequest
{
    public string GuestName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int TableNumber { get; set; }
    public int RoomId { get; set; } = 1;
    public DateTime BookingDate { get; set; } = DateTime.Now;
    public string StartTime { get; set; } = "19:00";
    public int DurationMinutes { get; set; } = 60;
}

// DTO для ответа
public class BookingDto
{
    public int Id { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int TableNumber { get; set; }
    public int RoomId { get; set; }
    public DateTime BookingDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string? EndTime { get; set; }
    public int DurationMinutes { get; set; }
    public string Status { get; set; } = string.Empty;
}