using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ThuVien.Models;
using ThuVien.Services;

namespace ThuVien.Controllers;

// Cài đặt hệ thống: chỉ Admin được thay đổi
[Authorize(Roles = AppRoles.Admin)]
public class SettingsController : Controller
{
    private readonly SettingsService _settings;

    public SettingsController(SettingsService settings)
    {
        _settings = settings;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await _settings.GetAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Index(LibrarySettings model)
    {
        if (model.MaxBooksAtOnce < model.MaxBooksPerLoan)
        {
            ModelState.AddModelError(nameof(model.MaxBooksAtOnce),
                "Số cuốn được mượn cùng lúc không được nhỏ hơn số đầu sách tối đa mỗi phiếu.");
        }
        if (!ModelState.IsValid) return View(model);

        await _settings.SaveAsync(model);
        TempData["Success"] = "Đã lưu cài đặt hệ thống. Áp dụng cho các phiếu mượn lập từ bây giờ.";
        return RedirectToAction(nameof(Index));
    }
}
