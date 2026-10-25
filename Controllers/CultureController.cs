using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace ThuVien.Controllers;

public class CultureController : Controller
{
    private static readonly string[] Supported = { "vi", "en" };

    // Lưu ngôn ngữ đã chọn vào cookie rồi quay lại trang trước
    [AllowAnonymous]
    [HttpPost]
    public IActionResult Set(string culture, string? returnUrl)
    {
        if (!Supported.Contains(culture)) culture = "vi";

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                SameSite = SameSiteMode.Lax
            });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }
}
