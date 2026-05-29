using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnticafeBackend.Data;
using AnticafeBackend.Models;
using AnticafeBackend.Services;

namespace AnticafeBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BookingController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly SecurityService _security;

    public BookingController(ApplicationDbContext context, SecurityService security)
    {
        _context = context;
        _security = security;
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActiveBookings()
    {
        var bookings = await _context.Bookings
            .Where(b => b.Status == "active" && b.BookingDate >= DateTime.Now.Date)
            .OrderBy(b => b.BookingDate)
            .ThenBy(b => b.BookingTime)
            .ToListAsync();
        return Ok(bookings);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateBooking([FromBody] Booking booking)
    {
        // Защита от спама
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        if (!await _security.CanBookAsync(ip))
        {
            return StatusCode(429, new { error = "Слишком много попыток. Подождите час." });
        }

        // Проверка на двойное бронирование
        var existing = await _context.Bookings
            .AnyAsync(b => b.BookingDate == booking.BookingDate
                && b.BookingTime == booking.BookingTime
                && b.TableNumber == booking.TableNumber
                && b.Status == "active");

        if (existing)
        {
            return BadRequest(new { error = "Этот стол уже забронирован на это время" });
        }

        booking.Status = "active";
        booking.CreatedAt = DateTime.Now;
        booking.IpAddress = ip;
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();

        await _security.LogActionAsync("booking_created", $"Гость: {booking.GuestName}, Стол: {booking.TableNumber}");

        return Ok(new { booking.Id, message = "Бронирование создано" });
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