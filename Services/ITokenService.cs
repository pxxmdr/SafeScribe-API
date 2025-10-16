using SafeScribe.Api.Dtos;
using SafeScribe.Api.Models;

namespace SafeScribe.Api.Services;

public interface ITokenService
{
    Task<User> RegisterAsync(UserRegisterDto dto);
    Task<LoginResponseDto> LoginAsync(LoginRequestDto dto);
}
