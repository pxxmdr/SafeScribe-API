using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SafeScribe.Api.Dtos;
using SafeScribe.Api.Services;

namespace SafeScribe.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;

    public AuthController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpPost("registrar")]
    [AllowAnonymous]
    public async Task<IActionResult> Registrar([FromBody] UserRegisterDto dto)
    {
        try
        {
            var user = await _tokenService.RegisterAsync(dto);
            return CreatedAtAction(nameof(Registrar), new { id = user.Id }, new
            {
                id = user.Id,
                username = user.Username,
                role = user.Role
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }


    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto dto)
    {
        try
        {
            var response = await _tokenService.LoginAsync(dto);
            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromServices] ITokenBlacklistService blacklist)
    {
        var jti = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        if (string.IsNullOrEmpty(jti))
            return BadRequest(new { error = "Token sem JTI." });

        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);
        var expStr = User.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;
        if (long.TryParse(expStr, out var expUnix))
        {
            expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix);
        }

        await blacklist.AddAsync(jti, expiresAt);
        return Ok(new { message = "Logout efetuado. Token revogado até expirar.", jti, expiresAt });
    }
}
