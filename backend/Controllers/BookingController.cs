using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnticafeBackend.Data;
using AnticafeBackend.Models;

namespace AnticafeBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BookingController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveBookings()
    {
        var bookings = await _context.Bookings
            .Where(b => b.Status == "active" && b.BookingDate >= DateTime.Now.Date)
            .OrderBy(b => b.BookingDate)
            .ThenBy(b => b.StartTime)
            .ToListAsync();
        return Ok(bookings);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        if (string.IsNullOrEmpty(request.GuestName))
            return BadRequest(new { error = "Введите имя" });

        if (string.IsNullOrEmpty(request.Phone))
            return BadRequest(new { error = "Введите телефон" });

        if (request.DurationMinutes < 30)
            return BadRequest(new { error = "Минимальная длительность - 30 минут" });

        var endTime = TimeSpan.Parse(request.StartTime).Add(TimeSpan.FromMinutes(request.DurationMinutes));
        var endTimeStr = endTime.ToString(@"hh\:mm");

        var isBooked = await _context.Bookings
            .AnyAsync(b => b.TableNumber == request.TableNumber &&
                          b.BookingDate.Date == request.BookingDate.Date &&
                          b.Status == "active" &&
                          string.Compare(b.StartTime, endTimeStr) < 0 &&
                          (b.EndTime == null || string.Compare(b.EndTime, request.StartTime) > 0));

        if (isBooked)
            return BadRequest(new { error = "Стол уже забронирован" });

        var booking = new Booking
        {
            GuestName = request.GuestName,
            Phone = request.Phone,
            TableNumber = request.TableNumber,
            RoomId = request.RoomId,
            BookingDate = request.BookingDate,
            StartTime = request.StartTime,
            EndTime = endTimeStr,
            DurationMinutes = request.DurationMinutes,
            Status = "active",
            CreatedAt = DateTime.Now
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            booking.Id,
            booking.GuestName,
            booking.TableNumber,
            booking.StartTime,
            booking.EndTime,
            booking.DurationMinutes
        });
    }

    [HttpDelete("cancel/{id}")]
    public async Task<IActionResult> CancelBooking(int id)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null) return NotFound();

        booking.Status = "cancelled";
        await _context.SaveChangesAsync();

        return Ok(new { message = "Бронь отменена" });
    }
}

public class CreateBookingRequest
{
    public string GuestName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int TableNumber { get; set; }
    public int RoomId { get; set; } = 1;
    public DateTime BookingDate { get; set; }
    public string StartTime { get; set; } = "19:00";
    public int DurationMinutes { get; set; } = 60;
}