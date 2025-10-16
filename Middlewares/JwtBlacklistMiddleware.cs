using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using SafeScribe.Api.Services;

namespace SafeScribe.Api.Middlewares;

public class JwtBlacklistMiddleware
{
    private readonly RequestDelegate _next;

    public JwtBlacklistMiddleware(RequestDelegate next) => _next = next;

    public async Task Invoke(HttpContext context, ITokenBlacklistService blacklist)
    {
        var jti = context.User?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

        if (!string.IsNullOrEmpty(jti) && blacklist.IsBlacklisted(jti))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Token revogado (logout)." });
            return;
        }

        await _next(context);
    }
}
