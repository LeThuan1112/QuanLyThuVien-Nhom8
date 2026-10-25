using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThuVien.Data;
using ThuVien.Models;
using ThuVien.ViewModels;

namespace ThuVien.Controllers;

// Quản lý tài khoản: chỉ dành cho Admin
[Authorize(Roles = AppRoles.Admin)]
public class UsersController : Controller
{
    private const int PageSize = 10;
    private static readonly string[] AllRoles = { AppRoles.Admin, AppRoles.Librarian, AppRoles.Reader };

    private readonly AppDbContext _db;
    private readonly IPasswordHasher<User> _hasher;

    public UsersController(AppDbContext db, IPasswordHasher<User> hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private void LoadRoles(string? selected)
    {
        var items = AllRoles.Select(r => new { Value = r, Text = RoleDisplay.Name(r) }).ToList();
        ViewBag.Roles = new SelectList(items, "Value", "Text", selected);
    }

    private async Task LoadReaders(int? selected, int? currentUserId = null)
    {
        // Hiển thị hồ sơ chưa có tài khoản, đồng thời giữ hồ sơ đang gắn với tài khoản được sửa.
        var readers = await _db.Readers.AsNoTracking()
            .Where(r => r.UserId == null || (currentUserId.HasValue && r.UserId == currentUserId.Value))
            .OrderBy(r => r.ReaderCode)
            .Select(r => new { r.ReaderId, Text = r.ReaderCode + " - " + r.FullName })
            .ToListAsync();
        ViewBag.Readers = new SelectList(readers, "ReaderId", "Text", selected);
    }

    public async Task<IActionResult> Index(string? keyword, string? role, int page = 1)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(u => u.Username.Contains(k) || u.FullName.Contains(k)
                                     || (u.Email != null && u.Email.Contains(k)));
        }
        if (!string.IsNullOrEmpty(role) && AllRoles.Contains(role))
        {
            query = query.Where(u => u.Role == role);
        }

        var ordered = query.OrderBy(u => u.Role).ThenBy(u => u.Username);
        var total = await ordered.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var users = await ordered.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
        var readerCodes = await _db.Readers.AsNoTracking()
            .Where(r => r.UserId != null)
            .ToDictionaryAsync(r => r.UserId!.Value, r => r.ReaderCode);

        return View(new UserListViewModel
        {
            Users = users,
            ReaderCodes = readerCodes,
            Keyword = keyword,
            Role = role,
            Page = page,
            PageSize = PageSize,
            TotalItems = total
        });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        LoadRoles(AppRoles.Librarian);
        await LoadReaders(null);
        return View(new UserCreateViewModel { Role = AppRoles.Librarian });
    }

    [HttpPost]
    public async Task<IActionResult> Create(UserCreateViewModel vm)
    {
        if (!AllRoles.Contains(vm.Role))
        {
            ModelState.AddModelError(nameof(vm.Role), "Vai trò không hợp lệ.");
        }

        var username = (vm.Username ?? string.Empty).Trim();
        if (await _db.Users.AnyAsync(u => u.Username == username))
        {
            ModelState.AddModelError(nameof(vm.Username), "Tên đăng nhập đã tồn tại.");
        }

        Reader? reader = null;
        if (vm.Role == AppRoles.Reader)
        {
            if (!vm.ReaderId.HasValue)
            {
                ModelState.AddModelError(nameof(vm.ReaderId), "Tài khoản Độc giả bắt buộc phải gắn với một hồ sơ độc giả.");
            }
            else
            {
                reader = await _db.Readers.FirstOrDefaultAsync(r => r.ReaderId == vm.ReaderId.Value && r.UserId == null);
                if (reader == null)
                {
                    ModelState.AddModelError(nameof(vm.ReaderId), "Hồ sơ độc giả không hợp lệ hoặc đã có tài khoản.");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            LoadRoles(vm.Role);
            await LoadReaders(vm.ReaderId);
            return View(vm);
        }

        var user = new User
        {
            Username = username,
            FullName = vm.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim(),
            Role = vm.Role,
            IsActive = true
        };
        user.PasswordHash = _hasher.HashPassword(user, vm.Password);

        if (reader != null) reader.User = user;
        _db.Users.Add(user);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Không thể lưu tài khoản. Vui lòng kiểm tra lại.");
            LoadRoles(vm.Role);
            await LoadReaders(vm.ReaderId);
            return View(vm);
        }

        TempData["Success"] = $"Đã tạo tài khoản \"{user.Username}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null) return NotFound();

        var linkedReaderId = await _db.Readers.AsNoTracking()
            .Where(r => r.UserId == id)
            .Select(r => (int?)r.ReaderId)
            .FirstOrDefaultAsync();

        LoadRoles(user.Role);
        await LoadReaders(linkedReaderId, user.UserId);
        ViewBag.IsSelf = user.UserId == CurrentUserId();
        return View(new UserEditViewModel
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            ReaderId = linkedReaderId
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UserEditViewModel vm)
    {
        if (id != vm.UserId) return NotFound();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null) return NotFound();

        var isSelf = user.UserId == CurrentUserId();
        vm.Username = user.Username;

        if (!AllRoles.Contains(vm.Role))
        {
            ModelState.AddModelError(nameof(vm.Role), "Vai trò không hợp lệ.");
        }

        // Không tự đổi vai trò hoặc tự khóa chính mình.
        if (isSelf)
        {
            vm.Role = user.Role;
            vm.IsActive = true;
        }

        // Luôn phải còn ít nhất một Admin đang hoạt động.
        if (user.Role == AppRoles.Admin && (vm.Role != AppRoles.Admin || !vm.IsActive))
        {
            var otherAdmins = await _db.Users.CountAsync(u => u.Role == AppRoles.Admin && u.IsActive && u.UserId != id);
            if (otherAdmins == 0)
            {
                ModelState.AddModelError(string.Empty, "Hệ thống phải còn ít nhất một Quản trị viên đang hoạt động.");
            }
        }

        Reader? selectedReader = null;
        if (vm.Role == AppRoles.Reader)
        {
            if (!vm.ReaderId.HasValue)
            {
                ModelState.AddModelError(nameof(vm.ReaderId), "Tài khoản Độc giả bắt buộc phải gắn với một hồ sơ độc giả.");
            }
            else
            {
                selectedReader = await _db.Readers.FirstOrDefaultAsync(r =>
                    r.ReaderId == vm.ReaderId.Value && (r.UserId == null || r.UserId == id));
                if (selectedReader == null)
                {
                    ModelState.AddModelError(nameof(vm.ReaderId), "Hồ sơ không tồn tại hoặc đã được gắn với tài khoản khác.");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            LoadRoles(vm.Role);
            await LoadReaders(vm.ReaderId, user.UserId);
            ViewBag.IsSelf = isSelf;
            return View(vm);
        }

        user.FullName = vm.FullName.Trim();
        user.Email = string.IsNullOrWhiteSpace(vm.Email) ? null : vm.Email.Trim();
        user.Role = vm.Role;
        user.IsActive = vm.IsActive;

        // Bảo đảm tài khoản chỉ gắn với một hồ sơ; không xóa hồ sơ hay lịch sử mượn.
        var previouslyLinkedReaders = await _db.Readers.Where(r => r.UserId == user.UserId).ToListAsync();
        foreach (var oldReader in previouslyLinkedReaders)
        {
            if (vm.Role != AppRoles.Reader || oldReader.ReaderId != selectedReader?.ReaderId)
            {
                oldReader.UserId = null;
                oldReader.User = null;
            }
        }

        if (vm.Role == AppRoles.Reader && selectedReader != null)
        {
            selectedReader.UserId = user.UserId;
            selectedReader.User = user;
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Không thể lưu tài khoản hoặc liên kết hồ sơ độc giả. Vui lòng kiểm tra dữ liệu.");
            LoadRoles(vm.Role);
            await LoadReaders(vm.ReaderId, user.UserId);
            ViewBag.IsSelf = isSelf;
            return View(vm);
        }

        TempData["Success"] = vm.Role == AppRoles.Reader
            ? $"Đã cập nhật tài khoản \"{user.Username}\" và liên kết hồ sơ độc giả."
            : $"Đã cập nhật tài khoản \"{user.Username}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null) return NotFound();

        if (user.UserId == CurrentUserId())
        {
            TempData["Error"] = "Bạn không thể tự khóa tài khoản của chính mình.";
            return RedirectToAction(nameof(Index));
        }

        if (user.IsActive && user.Role == AppRoles.Admin)
        {
            var otherAdmins = await _db.Users.CountAsync(u => u.Role == AppRoles.Admin && u.IsActive && u.UserId != id);
            if (otherAdmins == 0)
            {
                TempData["Error"] = "Không thể khóa: hệ thống phải còn ít nhất một Quản trị viên đang hoạt động.";
                return RedirectToAction(nameof(Index));
            }
        }

        user.IsActive = !user.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = user.IsActive
            ? $"Đã mở khóa tài khoản \"{user.Username}\"."
            : $"Đã khóa tài khoản \"{user.Username}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null) return NotFound();
        return View(new ResetPasswordViewModel { UserId = user.UserId, Username = user.Username });
    }

    [HttpPost]
    public async Task<IActionResult> ResetPassword(int id, ResetPasswordViewModel vm)
    {
        if (id != vm.UserId) return NotFound();

        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserId == id);
        if (user == null) return NotFound();

        vm.Username = user.Username;
        if (!ModelState.IsValid) return View(vm);

        user.PasswordHash = _hasher.HashPassword(user, vm.NewPassword);
        await _db.SaveChangesAsync();

        TempData["Success"] = $"Đã đặt lại mật khẩu cho \"{user.Username}\".";
        return RedirectToAction(nameof(Index));
    }
}
