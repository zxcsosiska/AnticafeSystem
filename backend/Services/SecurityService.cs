using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using AnticafeBackend.Data;
using AnticafeBackend.Models;

namespace AnticafeBackend.Services;

public class SecurityService
{
    private readonly ApplicationDbContext _context;
    private static readonly Dictionary<string, List<DateTime>> _bookingAttempts = new();

    public SecurityService(ApplicationDbContext context)
    {
        _context = context;
    }

    public string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToHexString(bytes).ToLower();
    }

    public async Task<(bool success, string message, User? user)> AuthenticateAsync(string username, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
            return (false, "Пользователь не найден", null);

        if (user.IsBlocked && user.BlockedUntil > DateTime.Now)
            return (false, $"Аккаунт заблокирован до {user.BlockedUntil:HH:mm}", null);

        string hashedInput = HashPassword(password);

        if (user.PasswordHash != hashedInput)
        {
            user.FailedAttempts++;

            if (user.FailedAttempts >= 3)
            {
                user.IsBlocked = true;
                user.BlockedUntil = DateTime.Now.AddMinutes(15);
            }

            await _context.SaveChangesAsync();
            return (false, $"Неверный пароль. Осталось попыток: {3 - user.FailedAttempts}", null);
        }

        user.FailedAttempts = 0;
        user.IsBlocked = false;
        user.BlockedUntil = null;
        await _context.SaveChangesAsync();

        return (true, "Успешный вход", user);
    }

    public Task<bool> CanBookAsync(string ipAddress)
    {
        if (string.IsNullOrEmpty(ipAddress))
            ipAddress = "unknown";

        if (!_bookingAttempts.ContainsKey(ipAddress))
            _bookingAttempts[ipAddress] = new List<DateTime>();

        var lastHourAttempts = _bookingAttempts[ipAddress]
            .Count(t => t > DateTime.Now.AddHours(-1));

        if (lastHourAttempts >= 3)
            return Task.FromResult(false);

        _bookingAttempts[ipAddress].Add(DateTime.Now);

        _bookingAttempts[ipAddress] = _bookingAttempts[ipAddress]
            .Where(t => t > DateTime.Now.AddHours(-1))
            .ToList();

        return Task.FromResult(true);
    }

    public Task LogActionAsync(string actionType, string data)
    {
        Console.WriteLine($"[LOG] {actionType}: {data}");
        return Task.CompletedTask;
    }
}