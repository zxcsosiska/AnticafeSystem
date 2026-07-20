using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Anticafe.Data;
using Anticafe.Models;
using System.Globalization;

namespace Anticafe.Controllers;

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

        var tariff = await _context.Tariffs.FirstOrDefaultAsync(t => t.IsActive);
        if (tariff != null)
        {
            dict["PricePerMinute"] = tariff.PricePerMinute.ToString(CultureInfo.InvariantCulture);
            dict["MinimumMinutes"] = tariff.MinimumMinutes.ToString();
            dict["TariffName"] = tariff.Name;
        }

        return Ok(dict);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateSetting([FromBody] UpdateSettingRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Key) || string.IsNullOrEmpty(request.Value))
                return BadRequest(new { success = false, error = "Key и Value обязательны" });

            if (request.Key == "PricePerMinute")
            {
                if (!decimal.TryParse(request.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                    return BadRequest(new { success = false, error = "Некорректный формат цены" });

                if (price <= 0)
                    return BadRequest(new { success = false, error = "Цена должна быть больше 0" });

                var tariff = await _context.Tariffs.FirstOrDefaultAsync(t => t.IsActive);
                if (tariff != null)
                {
                    tariff.PricePerMinute = Math.Round(price, 2);
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        success = true,
                        message = "Цена обновлена",
                        newPrice = tariff.PricePerMinute
                    });
                }
                return BadRequest(new { success = false, error = "Тариф не найден" });
            }

            if (request.Key == "MinimumMinutes")
            {
                if (!int.TryParse(request.Value, out var minutes) || minutes < 30)
                    return BadRequest(new { success = false, error = "Минимальная длительность должна быть не менее 30 минут" });

                var tariff = await _context.Tariffs.FirstOrDefaultAsync(t => t.IsActive);
                if (tariff != null)
                {
                    tariff.MinimumMinutes = minutes;
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        success = true,
                        message = "Минимальная длительность обновлена",
                        newMinimum = tariff.MinimumMinutes
                    });
                }
                return BadRequest(new { success = false, error = "Тариф не найден" });
            }

            return BadRequest(new { success = false, error = $"Настройка '{request.Key}' не найдена" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpGet("rooms")]
    public async Task<IActionResult> GetRooms()
    {
        var rooms = await _context.Rooms
            .OrderBy(r => r.Id)
            .Select(r => new
            {
                r.Id,
                r.Name,
                r.Type,
                r.Capacity,
                r.IsActive,
                r.CreatedAt,
                TableCount = _context.Tables.Count(t => t.RoomId == r.Id)
            })
            .ToListAsync();
        return Ok(rooms);
    }

    [HttpPost("rooms")]
    public async Task<IActionResult> AddRoom([FromBody] RoomRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest(new { success = false, error = "Введите название зала" });

            if (request.TableCount < 1 || request.TableCount > 50)
                return BadRequest(new { success = false, error = "Количество столов должно быть от 1 до 50" });

            // Проверка на дубликат названия
            var exists = await _context.Rooms.AnyAsync(r => r.Name == request.Name);
            if (exists)
                return BadRequest(new { success = false, error = "Зал с таким названием уже существует" });

            var room = new Room
            {
                Name = request.Name.Trim(),
                Type = string.IsNullOrWhiteSpace(request.Type) ? "usual" : request.Type,
                Capacity = request.TableCount * 4,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _context.Rooms.Add(room);
            await _context.SaveChangesAsync();

            // Создаём столы
            var maxTableNumber = await _context.Tables
                .Where(t => t.RoomId == room.Id)
                .MaxAsync(t => (int?)t.TableNumber) ?? 0;

            for (int i = 1; i <= request.TableCount; i++)
            {
                _context.Tables.Add(new Table
                {
                    RoomId = room.Id,
                    TableNumber = maxTableNumber + i,
                    Capacity = 4,
                    IsActive = true
                });
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"Зал '{room.Name}' создан с {request.TableCount} столами",
                room = new { room.Id, room.Name, room.Type }
            });
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
            var room = await _context.Rooms
                .Include(r => r.Tables)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
                return NotFound(new { success = false, error = "Зал не найден" });

            if (id == 1)
                return BadRequest(new { success = false, error = "Нельзя удалить основной зал" });

            // Проверяем, есть ли активные сеансы в этом зале
            var hasActiveSessions = await _context.Sessions
                .AnyAsync(s => s.RoomId == id && s.IsActive);

            if (hasActiveSessions)
                return BadRequest(new { success = false, error = "В зале есть активные сеансы" });

            // Проверяем, есть ли активные бронирования
            var hasActiveBookings = await _context.Bookings
                .AnyAsync(b => b.RoomId == id && b.Status == "active");

            if (hasActiveBookings)
                return BadRequest(new { success = false, error = "В зале есть активные бронирования" });

            _context.Rooms.Remove(room);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"Зал '{room.Name}' удалён" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }

    [HttpGet("tables")]
    public async Task<IActionResult> GetTables()
    {
        var tables = await _context.Tables
            .OrderBy(t => t.RoomId)
            .ThenBy(t => t.TableNumber)
            .Select(t => new
            {
                t.Id,
                t.RoomId,
                t.TableNumber,
                t.Capacity,
                t.IsActive,
                RoomName = t.Room != null ? t.Room.Name : ""
            })
            .ToListAsync();
        return Ok(tables);
    }

    [HttpPost("tables")]
    public async Task<IActionResult> AddTable([FromBody] TableRequest request)
    {
        try
        {
            if (request.RoomId <= 0)
                return BadRequest(new { success = false, error = "Выберите зал" });

            var room = await _context.Rooms.FindAsync(request.RoomId);
            if (room == null)
                return BadRequest(new { success = false, error = "Зал не найден" });

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

            return Ok(new
            {
                success = true,
                message = $"Стол {table.TableNumber} добавлен в зал '{room.Name}'",
                table = new { table.Id, table.TableNumber, table.RoomId }
            });
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
                return NotFound(new { success = false, error = "Стол не найден" });

            // Проверяем, есть ли активные сеансы за этим столом
            var hasActiveSessions = await _context.Sessions
                .AnyAsync(s => s.TableNumber == table.TableNumber && s.IsActive);

            if (hasActiveSessions)
                return BadRequest(new { success = false, error = "За столом идёт активный сеанс" });

            // Проверяем, есть ли активные бронирования
            var hasActiveBookings = await _context.Bookings
                .AnyAsync(b => b.TableNumber == table.TableNumber && b.Status == "active");

            if (hasActiveBookings)
                return BadRequest(new { success = false, error = "На столе есть активные бронирования" });

            _context.Tables.Remove(table);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = $"Стол {table.TableNumber} удалён" });
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