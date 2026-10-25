using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuVien.Data;
using ThuVien.Models;
using ThuVien.Services;
using ThuVien.ViewModels;

namespace ThuVien.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _hasher;

    private const int MaxLoginFailures = 5;
    private const int LoginLockMinutes = 15;

    public AccountController(AppDbContext db, IPasswordHasher<User> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectByRole(User.IsInRole(AppRoles.Reader));
        }
        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [EnableRateLimiting("Login")]
    public async Task<IActionResult> Login(LoginViewModel vm, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (!ModelState.IsValid) return View(vm);

        // Giới hạn đăng nhập sai: tối đa 5 lần trong 15 phút (đếm từ nhật ký hoạt động,
        // chỉ tính các lần sai sau lần đăng nhập thành công gần nhất)
        var windowStart = DateTime.Now.AddMinutes(-LoginLockMinutes);
        var lastOk = await _db.AuditLogs
            .Where(l => l.Action == AuditFilter.LoginAction && l.Success && l.Username == vm.Username)
            .MaxAsync(l => (DateTime?)l.CreatedAt);
        var countFrom = lastOk.HasValue && lastOk.Value > windowStart ? lastOk.Value : windowStart;
        var recentFails = await _db.AuditLogs.CountAsync(l =>
            l.Action == AuditFilter.LoginAction && !l.Success && l.Username == vm.Username && l.CreatedAt > countFrom);
        if (recentFails >= MaxLoginFailures)
        {
            ModelState.AddModelError(string.Empty,
                $"Đăng nhập sai quá {MaxLoginFailures} lần. Vui lòng thử lại sau {LoginLockMinutes} phút.");
            return View(vm);
        }

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == vm.Username && u.IsActive);
        var ok = user != null &&
                 _hasher.VerifyHashedPassword(user, user.PasswordHash, vm.Password) != PasswordVerificationResult.Failed;

        if (!ok || user == null)
        {
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
            return View(vm);
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("FullName", user.FullName),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var props = new AuthenticationProperties { IsPersistent = vm.RememberMe };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            props);

        // Session: lưu thông tin hiển thị của phiên đăng nhập
        HttpContext.Session.SetString("FullName", user.FullName);
        HttpContext.Session.SetString("Role", user.Role);

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectByRole(user.Role == AppRoles.Reader);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    // Đổi mật khẩu của chính mình
    [Authorize]
    [HttpGet]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return RedirectToAction(nameof(Login));

        if (_hasher.VerifyHashedPassword(user, user.PasswordHash, vm.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(nameof(vm.CurrentPassword), "Mật khẩu hiện tại không đúng.");
            return View(vm);
        }
        if (vm.NewPassword == vm.CurrentPassword)
        {
            ModelState.AddModelError(nameof(vm.NewPassword), "Mật khẩu mới phải khác mật khẩu hiện tại.");
            return View(vm);
        }

        user.PasswordHash = _hasher.HashPassword(user, vm.NewPassword);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Đã đổi mật khẩu thành công.";
        return RedirectByRole(User.IsInRole(AppRoles.Reader));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = 403;
        return View();
    }

    private IActionResult RedirectByRole(bool isReader)
    {
        return isReader
            ? RedirectToAction("ReaderHome", "Home")
            : RedirectToAction("Dashboard", "Home");
    }
}
