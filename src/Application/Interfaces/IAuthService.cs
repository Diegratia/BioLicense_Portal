using System.Threading.Tasks;

namespace BioLicense_Portal.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> LoginAsync(LoginRequestDto request);
        Task<AuthResponseDto?> RefreshTokenAsync(string token, string refreshToken);
        Task SeedOwnerAsync();
    }

    public record LoginRequestDto(string Username, string Password);
    public record AuthResponseDto(string AccessToken, string RefreshToken, string Username, string Role);
}
