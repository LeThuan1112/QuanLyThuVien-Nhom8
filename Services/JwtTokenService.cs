using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using ThuVien.Models;

namespace ThuVien.Services;

public sealed class JwtTokenService
{
    private readonly byte[] _key;
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _lifetimeMinutes;

    public JwtTokenService(byte[] key, string issuer, string audience, int lifetimeMinutes)
    {
        if (key.Length < 32) throw new ArgumentException("JWT signing key must be at least 256 bits.", nameof(key));
        _key = key;
        _issuer = issuer;
        _audience = audience;
        _lifetimeMinutes = Math.Clamp(lifetimeMinutes, 5, 1440);
    }

    public int LifetimeSeconds => _lifetimeMinutes * 60;

    public string CreateToken(User user)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new("FullName", user.FullName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(_lifetimeMinutes),
            signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
