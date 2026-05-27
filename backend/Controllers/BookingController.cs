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
            .ThenBy(b => b.BookingTime)
            .ToListAsync();
        return Ok(bookings);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateBooking([FromBody] Booking booking)
    {
        booking.Status = "active";
        booking.CreatedAt = DateTime.Now;
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        return Ok(new { booking.Id, message = "Бронирование создано" });
    }

    [HttpDelete("cancel/{id}")]
    public async Task<IActionResult> CancelBooking(int id, [FromBody] CancelRequest request)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null) return NotFound();

        if (!request.Confirm) return BadRequest("Требуется подтверждение");

        booking.Status = "cancelled";
        await _context.SaveChangesAsync();
        return Ok(new { message = "Бронь отменена" });
    }
}

public class CancelRequest
{
    public bool Confirm { get; set; }
}