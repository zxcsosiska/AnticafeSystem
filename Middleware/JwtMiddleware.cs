using Anticafe.Services;

namespace Anticafe.Middleware;

public class JwtMiddleware
{
    private readonly RequestDelegate _next;

    public JwtMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, JwtService jwtService)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (token != null && jwtService.ValidateToken(token))
        {
            // Токен валиден, пользователь будет авторизован через JWT Bearer
        }

        await _next(context);
    }
}