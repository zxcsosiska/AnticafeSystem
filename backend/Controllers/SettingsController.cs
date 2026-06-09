using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AnticafeBackend.Data;
using AnticafeBackend.Models;
using System.Globalization;

namespace AnticafeBackend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public SettingsController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllSettings()
    {
        var dict = new Dictionary<string, string>();
        var tariff = await _context.Tariffs.FirstOrDefaultAsync();
        if (tariff != null)
        {
            dict["PricePerMinute"] = tariff.PricePerMinute.ToString(CultureInfo.InvariantCulture);
        }
        return Ok(dict);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSetting([FromBody] UpdateSettingRequest request)
    {
        try
        {
            if (request.Key == "PricePerMinute")
            {
                var price = decimal.Parse(request.Value, CultureInfo.InvariantCulture);
                var tariff = await _context.Tariffs.FirstOrDefaultAsync();
                if (tariff != null)
                {
                    tariff.PricePerMinute = price;
                    await _context.SaveChangesAsync();
                }
                return Ok(new { success = true });
            }
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpGet("rooms")]
    public async Task<IActionResult> GetRooms()
    {
        var rooms = await _context.Rooms.ToListAsync();
        return Ok(rooms);
    }

    [HttpPost("rooms")]
    public async Task<IActionResult> AddRoom([FromBody] RoomRequest request)
    {
        try
        {
            var room = new Room
            {
                Name = request.Name,
                Type = request.Type ?? "usual",
                IsActive = true,
                CreatedAt = DateTime.Now,
                Capacity = 20
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, room });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpDelete("rooms/{id}")]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        try
        {
            var room = await _context.Rooms.FindAsync(id);
            if (room == null)
                return BadRequest(new { success = false, error = "Зал не найден" });

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpGet("tables")]
    public async Task<IActionResult> GetTables()
    {
        var tables = await _context.Tables.ToListAsync();
        return Ok(tables);
    }

    [HttpPost("tables")]
    public async Task<IActionResult> AddTable([FromBody] TableRequest request)
    {
        try
        {
            var maxNumber = await _context.Tables
                .Where(t => t.RoomId == request.RoomId)
                .MaxAsync(t => (int?)t.TableNumber) ?? 0;

            var table = new Table
            {
                RoomId = request.RoomId,
                TableNumber = maxNumber + 1,
                Capacity = 4,
                IsActive = true
            };

            _context.Tables.Add(table);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, table });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpDelete("tables/{id}")]
    public async Task<IActionResult> DeleteTable(int id)
    {
        try
        {
            var table = await _context.Tables.FindAsync(id);
            if (table == null)
                return BadRequest(new { success = false, error = "Стол не найден" });

            _context.Tables.Remove(table);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}

public class UpdateSettingRequest
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class RoomRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "usual";
    public int TableCount { get; set; } = 1;
}

public class TableRequest
{
    public int RoomId { get; set; }
}