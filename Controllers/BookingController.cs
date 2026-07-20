using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Anticafe.Services;

namespace Anticafe.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingController : ControllerBase
{
    private readonly BookingService _bookingService;

    public BookingController(BookingService bookingService)
    {
        _bookingService = bookingService;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveBookings()
    {
        var bookings = await _bookingService.GetActiveBookingsAsync();
        return Ok(bookings);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        // Валидация
        if (string.IsNullOrWhiteSpace(request.GuestName))
            return BadRequest(new { error = "Введите имя гостя" });

        // Телефон НЕ ОБЯЗАТЕЛЕН - проверяем только если указан
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            // Базовая проверка формата телефона
            var cleaned = System.Text.RegularExpressions.Regex.Replace(request.Phone, @"[\s\-\(\)]", "");
            if (!System.Text.RegularExpressions.Regex.IsMatch(cleaned, @"^\+?[\d]+$"))
                return BadRequest(new { error = "Телефон должен содержать только цифры и +" });

            var digits = cleaned.Replace("+", "");
            if (digits.Length < 10 || digits.Length > 15)
                return BadRequest(new { error = "Введите корректный номер (10-15 цифр)" });
        }

        if (request.DurationMinutes < 30)
            return BadRequest(new { error = "Минимальная длительность - 30 минут" });

        if (request.TableNumber <= 0)
            return BadRequest(new { error = "Выберите стол" });

        if (request.RoomId <= 0)
            return BadRequest(new { error = "Выберите зал" });

        // Создаем запрос для сервиса
        var serviceRequest = new Anticafe.Services.CreateBookingRequest
        {
            GuestName = request.GuestName,
            Phone = request.Phone,
            TableNumber = request.TableNumber,
            RoomId = request.RoomId,
            BookingDate = request.BookingDate,
            StartTime = request.StartTime,
            DurationMinutes = request.DurationMinutes
        };

        var (success, error, booking) = await _bookingService.CreateBookingAsync(serviceRequest);

        if (!success)
            return BadRequest(new { error });

        // Проверяем, что booking не null перед использованием
        if (booking == null)
            return BadRequest(new { error = "Ошибка создания бронирования" });

        return Ok(new
        {
            booking,
            Message = $"Бронирование создано! Стол {booking.TableNumber} на {booking.DurationMinutes} минут"
        });
    }

    [HttpDelete("cancel/{id}")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var (success, error) = await _bookingService.CancelBookingAsync(id);

        if (!success)
            return BadRequest(new { error });

        return Ok(new
        {
            success = true,
            message = "Бронирование отменено",
            bookingId = id
        });
    }
}

// DTO для контроллера (входящий запрос)
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