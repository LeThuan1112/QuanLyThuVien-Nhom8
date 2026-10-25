using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using ThuVien.Api;
using ThuVien.Data;
using ThuVien.Models;
using ThuVien.Services;

namespace ThuVien.Controllers.Api;

[ApiController]
[Route("api/auth")]
public class AuthApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _hasher;
    private readonly JwtTokenService _tokens;

    public AuthApiController(AppDbContext db, IPasswordHasher<User> hasher, JwtTokenService tokens)
    {
        _db = db;
        _hasher = hasher;
        _tokens = tokens;
    }

    /// <summary>POST /api/auth/token — lấy Bearer token để gọi các API dành cho nhân viên.</summary>
    [AllowAnonymous]
    [HttpPost("token")]
    [IgnoreAntiforgeryToken]
    [EnableRateLimiting("TokenIssue")]
    public async Task<IActionResult> CreateToken([FromBody] TokenRequest request)
    {
        var username = request.Username.Trim();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
        var valid = user != null && _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) != PasswordVerificationResult.Failed;
        if (!valid || user == null)
        {
            // Không tiết lộ tài khoản có tồn tại hay đang bị khóa.
            return Unauthorized(ApiResponse<object>.Fail("Tên đăng nhập hoặc mật khẩu không đúng, hoặc tài khoản không hoạt động."));
        }

        var token = _tokens.CreateToken(user);
        return Ok(ApiResponse<TokenResponse>.Ok(new TokenResponse
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresIn = _tokens.LifetimeSeconds,
            Username = user.Username,
            Role = user.Role
        }));
    }
}

public sealed class TokenRequest
{
    [Required, StringLength(50, MinimumLength = 1)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;
}

public sealed class TokenResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
