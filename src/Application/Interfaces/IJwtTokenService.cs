using BioLicense_Portal.Domain.Entities;
using System.Security.Claims;

namespace BioLicense_Portal.Application.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
