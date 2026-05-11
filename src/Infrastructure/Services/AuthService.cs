using BioLicense_Portal.Application.Interfaces;
using BioLicense_Portal.Domain.Entities;
using BioLicense_Portal.Domain.Enums;
using BioLicense_Portal.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using BioLicense_Portal.Infrastructure.Data;

namespace BioLicense_Portal.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserRepository _userRepository;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IJwtTokenService _jwtTokenService;
        private readonly BioLicenseDbContext _dbContext;

        public AuthService(
            UserRepository userRepository, 
            IPasswordHasher passwordHasher, 
            IJwtTokenService jwtTokenService,
            BioLicenseDbContext dbContext)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
            _jwtTokenService = jwtTokenService;
            _dbContext = dbContext;
        }

        public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto request)
        {
            var user = await _userRepository.GetByUsernameAsync(request.Username);
            if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
            {
                return null;
            }

            var accessToken = _jwtTokenService.GenerateAccessToken(user);
            var refreshToken = _jwtTokenService.GenerateRefreshToken();

            // Store refresh token
            var rt = new RefreshToken
            {
                UserId = user.Id,
                Token = refreshToken,
                ExpiryDate = DateTime.UtcNow.AddDays(7)
            };

            await _dbContext.RefreshTokens.AddAsync(rt);
            await _dbContext.SaveChangesAsync();

            return new AuthResponseDto(accessToken, refreshToken, user.Username, user.Role.ToString());
        }

        public async Task<AuthResponseDto?> RefreshTokenAsync(string token, string refreshToken)
        {
            var principal = _jwtTokenService.GetPrincipalFromExpiredToken(token);
            if (principal == null) return null;

            var username = principal.Identity?.Name;
            var user = await _userRepository.GetByUsernameAsync(username!);
            if (user == null) return null;

            var savedRefreshToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(x => x.Token == refreshToken && x.UserId == user.Id);

            if (savedRefreshToken == null || savedRefreshToken.ExpiryDate <= DateTime.UtcNow)
            {
                return null;
            }

            var newAccessToken = _jwtTokenService.GenerateAccessToken(user);
            return new AuthResponseDto(newAccessToken, refreshToken, user.Username, user.Role.ToString());
        }

        public async Task SeedOwnerAsync()
        {
            var existingOwner = await _userRepository.GetByUsernameAsync("owner");
            if (existingOwner != null) return;

            var owner = new User
            {
                Id = Guid.NewGuid(),
                Username = "owner",
                Email = "owner@biolicense.com",
                FullName = "System Owner",
                PasswordHash = _passwordHasher.HashPassword("P@ssw0rd123"), // Default password for seeding
                Role = UserRole.Owner,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(owner);
        }
    }
}
