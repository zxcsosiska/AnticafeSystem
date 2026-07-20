using Microsoft.EntityFrameworkCore;
using Anticafe.Data;
using Anticafe.Models;
using BCrypt.Net;

namespace Anticafe.Services;

public class SecurityService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtService _jwtService;
    private static readonly Dictionary<string, (int attempts, DateTime blockUntil)> _attempts = new();

    public SecurityService(ApplicationDbContext context, JwtService jwtService)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
    }

    public string HashPassword(string password)
    {
        // BCrypt с автоматической солью
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    public bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    public async Task<(bool success, string message, string? token, UserDto? user)> AuthenticateAsync(string username, string password)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        if (user == null)
            return (false, "Пользователь не найден", null, null);

        // Проверка блокировки
        if (_attempts.TryGetValue(username, out var data) && data.blockUntil > DateTime.Now)
            return (false, $"Аккаунт заблокирован до {data.blockUntil:HH:mm}", null, null);

        // Проверка пароля
        if (!VerifyPassword(password, user.PasswordHash))
        {
            var attempts = _attempts.GetValueOrDefault(username).attempts + 1;
            var blockUntil = attempts >= 3 ? DateTime.Now.AddMinutes(15) : DateTime.MinValue;
            _attempts[username] = (attempts, blockUntil);

            var remaining = 3 - attempts;
            return (false, remaining > 0 ? $"Неверный пароль. Осталось попыток: {remaining}" : "Аккаунт заблокирован на 15 минут", null, null);
        }

        _attempts.Remove(username);
        user.FailedAttempts = 0;
        user.IsBlocked = false;
        user.BlockedUntil = null;
        await _context.SaveChangesAsync();

        // Генерируем JWT токен
        var token = _jwtService.GenerateToken(user);

        var userDto = new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role
        };

        return (true, "Успешный вход", token, userDto);
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return null;

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            FullName = user.FullName,
            Role = user.Role
        };
    }
}

public class UserDto
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}