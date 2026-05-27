using Microsoft.AspNetCore.Mvc;
using AnticafeBackend.Services;

namespace AnticafeBackend.Controllers;

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
        var (success, message, user) = await _security.AuthenticateAsync(request.Username, request.Password);

        if (!success)
            return StatusCode(403, new { success, message });

        return Ok(new
        {
            success,
            message,
            user = new { user?.Id, user?.Username, user?.FullName, user?.Role }
        });
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}