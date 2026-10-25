using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuVien.Data;
using ThuVien.Models;
using ThuVien.ViewModels;

namespace ThuVien.Controllers;

[Authorize(Roles = AppRoles.Staff)]
public class ReadersController : Controller
{
    private const int PageSize = 10;
    private readonly AppDbContext _db;

    public ReadersController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? keyword, string? status, int page = 1)
    {
        var today = DateTime.Today;
        var query = _db.Readers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(r => r.ReaderCode.Contains(k) || r.FullName.Contains(k)
                                     || (r.Phone != null && r.Phone.Contains(k))
                                     || (r.Email != null && r.Email.Contains(k)));
        }

        query = status switch
        {
            "active" => query.Where(r => r.IsActive && r.CardExpiryDate >= today),
            "locked" => query.Where(r => !r.IsActive),
            "expired" => query.Where(r => r.IsActive && r.CardExpiryDate < today),
            _ => query
        };

        var ordered = query.OrderBy(r => r.ReaderCode);
        var total = await ordered.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await ordered.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        return View(new ReaderListViewModel
        {
            Readers = items,
            Keyword = keyword,
            Status = status,
            Page = page,
            PageSize = PageSize,
            TotalItems = total
        });
    }

    public async Task<IActionResult> Details(int id)
    {
        var reader = await _db.Readers.AsNoTracking().FirstOrDefaultAsync(r => r.ReaderId == id);
        if (reader == null) return NotFound();

        var loans = await _db.Loans.AsNoTracking()
            .Include(l => l.Details)
            .Where(l => l.ReaderId == id)
            .OrderByDescending(l => l.LoanDate)
            .ToListAsync();

        return View(new ReaderDetailsViewModel { Reader = reader, Loans = loans });
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.ReaderCodePreview = await GetNextReaderCodeAsync();
        return View(new Reader());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("FullName,Phone,Email,Address,CardIssuedDate,CardExpiryDate,IsActive")] Reader reader)
    {
        // The code is generated on the server; do not trust form values for ReaderCode.
        ModelState.Remove(nameof(Reader.ReaderCode));

        reader.FullName = (reader.FullName ?? string.Empty).Trim();
        reader.Phone = string.IsNullOrWhiteSpace(reader.Phone) ? null : reader.Phone.Trim();
        reader.Email = string.IsNullOrWhiteSpace(reader.Email) ? null : reader.Email.Trim();
        reader.Address = string.IsNullOrWhiteSpace(reader.Address) ? null : reader.Address.Trim();

        if (reader.CardExpiryDate.Date < reader.CardIssuedDate.Date)
        {
            ModelState.AddModelError(nameof(Reader.CardExpiryDate), "Ngày hết hạn phải sau hoặc bằng ngày cấp thẻ.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.ReaderCodePreview = await GetNextReaderCodeAsync();
            return View(reader);
        }

        // The existing unique index on ReaderCode protects the database. If two requests
        // happen to pick the same value at nearly the same time, retry with a fresh value.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            reader.ReaderCode = await GetNextReaderCodeAsync();
            _db.Readers.Add(reader);

            try
            {
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã thêm độc giả \"{reader.FullName}\" với mã {reader.ReaderCode}.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                _db.Entry(reader).State = EntityState.Detached;

                if (attempt == 2)
                {
                    ModelState.AddModelError(string.Empty,
                        "Chưa thể lưu hồ sơ độc giả. Có thể mã vừa được cấp đồng thời; vui lòng thử lưu lại.");
                    ViewBag.ReaderCodePreview = await GetNextReaderCodeAsync();
                    return View(reader);
                }
            }
        }

        // Defensive fallback; each successful save returns above.
        ViewBag.ReaderCodePreview = await GetNextReaderCodeAsync();
        ModelState.AddModelError(string.Empty, "Không thể lưu hồ sơ độc giả. Vui lòng thử lại.");
        return View(reader);
    }

    private async Task<string> GetNextReaderCodeAsync()
    {
        var codes = await _db.Readers
            .AsNoTracking()
            .Select(r => r.ReaderCode)
            .ToListAsync();

        long maxNumber = 0;
        foreach (var code in codes)
        {
            if (string.IsNullOrWhiteSpace(code) ||
                !code.StartsWith("DB", StringComparison.OrdinalIgnoreCase) ||
                code.Length <= 2)
            {
                continue;
            }

            var suffix = code[2..];
            if (long.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out var number) &&
                number > maxNumber)
            {
                maxNumber = number;
            }
        }

        if (maxNumber == long.MaxValue)
        {
            throw new InvalidOperationException("Đã vượt giới hạn số thứ tự mã độc giả DB.");
        }

        return $"DB{maxNumber + 1:D2}";
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var reader = await _db.Readers.AsNoTracking().FirstOrDefaultAsync(r => r.ReaderId == id);
        if (reader == null) return NotFound();
        return View(reader);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id,
        [Bind("ReaderId,ReaderCode,FullName,Phone,Email,Address,CardIssuedDate,CardExpiryDate,IsActive")] Reader posted)
    {
        if (id != posted.ReaderId) return NotFound();

        var reader = await _db.Readers.FirstOrDefaultAsync(r => r.ReaderId == id);
        if (reader == null) return NotFound();

        posted.ReaderCode = (posted.ReaderCode ?? string.Empty).Trim();

        if (await _db.Readers.AnyAsync(r => r.ReaderCode == posted.ReaderCode && r.ReaderId != id))
        {
            ModelState.AddModelError(nameof(Reader.ReaderCode), "Mã độc giả đã tồn tại.");
        }
        if (posted.CardExpiryDate.Date < posted.CardIssuedDate.Date)
        {
            ModelState.AddModelError(nameof(Reader.CardExpiryDate), "Ngày hết hạn phải sau hoặc bằng ngày cấp thẻ.");
        }
        if (!ModelState.IsValid) return View(posted);

        reader.ReaderCode = posted.ReaderCode;
        reader.FullName = posted.FullName;
        reader.Phone = posted.Phone;
        reader.Email = posted.Email;
        reader.Address = posted.Address;
        reader.CardIssuedDate = posted.CardIssuedDate;
        reader.CardExpiryDate = posted.CardExpiryDate;
        reader.IsActive = posted.IsActive;

        await _db.SaveChangesAsync();
        TempData["Success"] = $"Đã cập nhật độc giả \"{reader.FullName}\".";
        return RedirectToAction(nameof(Index));
    }

    // Khóa / mở thẻ
    [HttpPost]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var reader = await _db.Readers.FirstOrDefaultAsync(r => r.ReaderId == id);
        if (reader == null) return NotFound();

        reader.IsActive = !reader.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = reader.IsActive
            ? $"Đã mở thẻ cho \"{reader.FullName}\"."
            : $"Đã khóa thẻ của \"{reader.FullName}\".";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var reader = await _db.Readers.FirstOrDefaultAsync(r => r.ReaderId == id);
        if (reader == null) return NotFound();

        // Giữ liên kết đăng nhập và lịch sử giao dịch: chỉ xóa hồ sơ chưa được sử dụng.
        if (reader.UserId.HasValue)
        {
            TempData["Error"] = "Không thể xóa: hồ sơ đang được liên kết với tài khoản đăng nhập. Hãy xử lý liên kết tài khoản trước.";
            return RedirectToAction(nameof(Index));
        }
        if (await _db.Loans.AnyAsync(l => l.ReaderId == id))
        {
            TempData["Error"] = "Không thể xóa: độc giả đã có lịch sử mượn. Hãy khóa thẻ thay vì xóa.";
            return RedirectToAction(nameof(Index));
        }
        if (await _db.BookReservations.AnyAsync(r => r.ReaderId == id))
        {
            TempData["Error"] = "Không thể xóa: độc giả đã có lịch sử đặt trước. Hãy khóa thẻ thay vì xóa.";
            return RedirectToAction(nameof(Index));
        }

        _db.Readers.Remove(reader);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Không thể xóa hồ sơ vì dữ liệu khác đang tham chiếu đến hồ sơ này.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = $"Đã xóa độc giả \"{reader.FullName}\".";
        return RedirectToAction(nameof(Index));
    }
}
