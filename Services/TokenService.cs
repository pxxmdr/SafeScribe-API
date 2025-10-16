using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SafeScribe.Api.Auth;
using SafeScribe.Api.Data;
using SafeScribe.Api.Dtos;
using SafeScribe.Api.Models;

namespace SafeScribe.Api.Services;

public class TokenService : ITokenService
{
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;

    public TokenService(AppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // Registro com hash BCrypt
    public async Task<User> RegisterAsync(UserRegisterDto dto)
    {
        var exists = await _db.Users.AnyAsync(u => u.Username == dto.Username);
        if (exists) throw new InvalidOperationException("Username já existe.");

        var validRoles = new[] { Roles.Leitor, Roles.Editor, Roles.Admin };
        if (!validRoles.Contains(dto.Role))
            throw new InvalidOperationException("Role inválida. Use Leitor, Editor ou Admin.");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = dto.Username,
            PasswordHash = passwordHash,
            Role = dto.Role
        };

        await _db.Users.AddAsync(user);
        await _db.SaveChangesAsync();
        return user;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Username == dto.Username);
        if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Credenciais inválidas.");

        var issuer   = _config["Jwt:Issuer"];
        var audience = _config["Jwt:Audience"];
        var secret   = _config["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret não configurado.");

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var jti = Guid.NewGuid().ToString();
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, jti),
        };

        var now = DateTime.UtcNow;
        var expires = now.AddHours(1);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new LoginResponseDto
        {
            Token = tokenString,
            ExpiresAt = token.ValidTo,
            UserId = user.Id,
            Username = user.Username,
            Role = user.Role,
            Jti = jti
        };
    }
}
