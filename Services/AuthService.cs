using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaskTracker.Web.Auth;
using TaskTracker.Web.Data;
using TaskTracker.Web.Dtos.Auth;
using TaskTracker.Web.Exceptions;
using TaskTracker.Web.Models;

namespace TaskTracker.Web.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly JwtOptions _jwtOptions;

    public AuthService(ApplicationDbContext dbContext, IOptions<JwtOptions> jwtOptions)
    {
        _dbContext = dbContext;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existingUser = await _dbContext.Users
            .AnyAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (existingUser)
        {
            throw new ConflictException("A megadott email cimmel mar letezik felhasznalo.");
        }

        var now = DateTime.UtcNow;
        var refreshToken = GenerateSecureToken();
        var refreshTokenExpiresAt = now.AddDays(_jwtOptions.RefreshTokenExpirationDays);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            FullName = request.FullName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            RefreshTokenHash = HashToken(refreshToken),
            RefreshTokenExpiresAt = refreshTokenExpiresAt,
            CreatedAt = now,
            UpdatedAt = now
        };

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateAuthResponse(user, refreshToken);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Hibas email vagy jelszo.");
        }

        var now = DateTime.UtcNow;
        var refreshToken = GenerateSecureToken();
        var refreshTokenExpiresAt = now.AddDays(_jwtOptions.RefreshTokenExpirationDays);

        user.RefreshTokenHash = HashToken(refreshToken);
        user.RefreshTokenExpiresAt = refreshTokenExpiresAt;
        user.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateAuthResponse(user, refreshToken);
    }

    public async Task<AuthResponseDto> RefreshAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var now = DateTime.UtcNow;

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(
                x => x.RefreshTokenHash == tokenHash,
                cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAccessException("Ervenytelen refresh token.");
        }

        if (user.RefreshTokenExpiresAt < now)
        {
            throw new UnauthorizedAccessException("A refresh token lejart.");
        }

        var newRefreshToken = GenerateSecureToken();
        var newRefreshTokenExpiresAt = now.AddDays(_jwtOptions.RefreshTokenExpirationDays);

        user.RefreshTokenHash = HashToken(newRefreshToken);
        user.RefreshTokenExpiresAt = newRefreshTokenExpiresAt;
        user.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreateAuthResponse(user, newRefreshToken);
    }

    public async Task LogoutAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAccessException("Felhasznalo nem talalhato.");
        }

        user.RefreshTokenHash = null;
        user.RefreshTokenExpiresAt = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthUserDto> GetMeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            throw new UnauthorizedAccessException("Felhasznalo nem talalhato.");
        }

        return new AuthUserDto
        {
            Id = user.Id,
            Email = user.Email,
            FullName = user.FullName
        };
    }

    private AuthResponseDto CreateAuthResponse(User user, string refreshToken)
    {
        var accessTokenExpiresAt = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes);

        return new AuthResponseDto
        {
            AccessToken = GenerateAccessToken(user, accessTokenExpiresAt),
            AccessTokenExpiresAt = accessTokenExpiresAt,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = user.RefreshTokenExpiresAt ?? DateTime.UtcNow,
            User = new AuthUserDto
            {
                Id = user.Id,
                Email = user.Email,
                FullName = user.FullName
            }
        };
    }

    private string GenerateAccessToken(User user, DateTime expiresAt)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.FullName),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateSecureToken()
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(tokenBytes);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
