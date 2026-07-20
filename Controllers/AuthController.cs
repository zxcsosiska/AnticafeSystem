using Microsoft.AspNetCore.Authorization; // ДОБАВИТЬ ЭТУ СТРОКУ!
using Microsoft.AspNetCore.Mvc;
using Anticafe.Services;
using System.Security.Claims;

namespace Anticafe.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly SecurityService _security;

    public AuthController(SecurityService security)
    {
        _security = security;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            return BadRequest(new { success = false, message = "Введите логин и пароль" });

        var (success, message, token, user) = await _security.AuthenticateAsync(request.Username, request.Password);

        if (!success)
            return Unauthorized(new { success, message });

        return Ok(new
        {
            success,
            message,
            token,
            user = new { user?.Id, user?.Username, user?.FullName, user?.Role }
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(new { success = true, message = "Выход выполнен" });
    }

    [HttpGet("me")]
    [Authorize] // ТЕПЕРЬ РАБОТАЕТ!
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { success = false, message = "Не авторизован" });

        var user = await _security.GetUserByIdAsync(int.Parse(userId));
        if (user == null)
            return NotFound(new { success = false, message = "Пользователь не найден" });

        return Ok(new { success = true, user });
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}