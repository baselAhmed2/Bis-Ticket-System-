using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using TicketsDomain.Models;
using TicketsServiesAbstraction.IServices;
using TicketsShared.DTO.Auth;
using TicketsShared.Settings;

namespace TicketsServies
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtSettings _jwtSettings;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            JwtSettings jwtSettings)
        {
            _userManager = userManager;
            _jwtSettings = jwtSettings;
        }

        public async Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginRequest)
        {
            // Find user by Username (ID الكلية)
            var user = await _userManager.FindByNameAsync(loginRequest.Username);
            if (user == null) return null;

            // Check password (SSN)
            var isPasswordValid = await _userManager.CheckPasswordAsync(
                user, loginRequest.Password);

            if (!isPasswordValid) return null;

            // Get user role
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Student";

            // Generate JWT Token
            var token = GenerateJwtToken(user, role);

            return new LoginResponseDto
            {
                Token = token,
                UserId = user.Id,
                Name = user.Name,
                Role = role
            };
        }

        public async Task<string?> GetUserRoleAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            return roles.FirstOrDefault();
        }

        // =====================
        // Helper Methods
        // =====================
        private string GenerateJwtToken(ApplicationUser user, string role)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, role)
            };

            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtSettings.Secret));

            var creds = new SigningCredentials(
                key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryInMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
