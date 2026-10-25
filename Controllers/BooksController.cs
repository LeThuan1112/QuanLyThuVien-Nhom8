using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThuVien.Data;
using ThuVien.Models;
using ThuVien.ViewModels;

namespace ThuVien.Controllers;

public class BooksController : Controller
{
    // Để 5 cho dễ thấy phân trang với dữ liệu mẫu; có thể đổi thành 10
    private const int PageSize = 12;

    private readonly AppDbContext _db;

    public BooksController(AppDbContext db)
    {
        _db = db;
    }

    private bool IsStaff() => User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Librarian);

    private async Task LoadCategories(int? selectedId = null)
    {
        var cats = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        ViewBag.Categories = new SelectList(cats, "CategoryId", "Name", selectedId);
    }

    // Tra cứu: tìm kiếm + lọc + sắp xếp + phân trang. Gọi bằng AJAX sẽ trả về partial.
    [AllowAnonymous]
    public async Task<IActionResult> Index(string? keyword, int? categoryId, string? availability, string? sort, int page = 1)
    {
        var isStaff = IsStaff();
        var query = _db.Books.AsNoTracking().Include(b => b.Category).AsQueryable();

        // Người dùng thường chỉ thấy sách đang lưu hành
        if (!isStaff) query = query.Where(b => b.IsActive);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(b => b.Title.Contains(k) || b.BookCode.Contains(k) || b.Author.Contains(k) || (b.ISBN != null && b.ISBN.Contains(k)));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(b => b.CategoryId == categoryId.Value);
        }

        var now = DateTime.Now;
        if (availability == "available")
        {
            query = query.Where(b => b.IsActive && b.AvailableCopies > _db.BookReservations.Count(r =>
                r.BookId == b.BookId && r.Status == ReservationStatuses.Ready && r.ExpiresAt != null && r.ExpiresAt > now));
        }
        else if (availability == "unavailable")
        {
            query = query.Where(b => !b.IsActive || b.AvailableCopies <= _db.BookReservations.Count(r =>
                r.BookId == b.BookId && r.Status == ReservationStatuses.Ready && r.ExpiresAt != null && r.ExpiresAt > now));
        }

        IOrderedQueryable<Book> ordered = sort switch
        {
            "title_desc" => query.OrderByDescending(b => b.Title),
            "code" => query.OrderBy(b => b.BookCode),
            "code_desc" => query.OrderByDescending(b => b.BookCode),
            "year" => query.OrderBy(b => b.PublishYear),
            "year_desc" => query.OrderByDescending(b => b.PublishYear),
            "available_desc" => query.OrderByDescending(b => b.AvailableCopies),
            _ => query.OrderBy(b => b.Title)
        };
        var finalQuery = ordered.ThenBy(b => b.BookId);

        var total = await finalQuery.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var books = await finalQuery.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();
        var bookIds = books.Select(b => b.BookId).ToList();
        var heldCopies = bookIds.Count == 0
            ? new Dictionary<int, int>()
            : await _db.BookReservations.AsNoTracking()
                .Where(r => bookIds.Contains(r.BookId) && r.Status == ReservationStatuses.Ready
                            && r.ExpiresAt != null && r.ExpiresAt > DateTime.Now)
                .GroupBy(r => r.BookId)
                .Select(g => new { BookId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.BookId, x => x.Count);

        var vm = new BookListViewModel
        {
            Books = books,
            HeldCopies = heldCopies,
            Categories = await _db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(),
            Keyword = keyword,
            CategoryId = categoryId,
            Availability = availability,
            Sort = string.IsNullOrEmpty(sort) ? "title" : sort,
            Page = page,
            PageSize = PageSize,
            TotalItems = total
        };

        if (string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.Ordinal))
        {
            return PartialView("_BookList", vm);
        }
        return View(vm);
    }

    [AllowAnonymous]
    public async Task<IActionResult> Details(int id)
    {
        var book = await _db.Books.AsNoTracking().Include(b => b.Category).FirstOrDefaultAsync(b => b.BookId == id);
        if (book == null) return NotFound();
        if (!book.IsActive && !IsStaff()) return NotFound();
        var heldCopies = await _db.BookReservations.AsNoTracking()
            .CountAsync(r => r.BookId == book.BookId && r.Status == ReservationStatuses.Ready
                             && r.ExpiresAt != null && r.ExpiresAt > DateTime.Now);
        ViewBag.AvailableToBorrow = Math.Max(0, book.AvailableCopies - heldCopies);
        ViewBag.CanReserve = book.IsActive && Math.Max(0, book.AvailableCopies - heldCopies) == 0;
        return View(book);
    }

    [Authorize(Roles = AppRoles.Staff)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        ViewBag.BookCodePreview = await GetNextBookCodeAsync();
        await LoadCategories();
        return View(new Book { TotalCopies = 1 });
    }

    [Authorize(Roles = AppRoles.Staff)]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Title,ISBN,CategoryId,Author,Publisher,PublishYear,TotalCopies,ShelfLocation,CoverImageUrl,Description,IsActive")] Book book)
    {
        // Mã sách do server tự cấp; không dùng mã do trình duyệt gửi lên.
        ModelState.Remove(nameof(Book.BookCode));
        book.Title = (book.Title ?? string.Empty).Trim();
        book.Author = (book.Author ?? string.Empty).Trim();
        book.ISBN = string.IsNullOrWhiteSpace(book.ISBN) ? null : book.ISBN.Trim();
        book.Publisher = string.IsNullOrWhiteSpace(book.Publisher) ? null : book.Publisher.Trim();
        book.ShelfLocation = string.IsNullOrWhiteSpace(book.ShelfLocation) ? null : book.ShelfLocation.Trim();
        book.CoverImageUrl = string.IsNullOrWhiteSpace(book.CoverImageUrl) ? null : book.CoverImageUrl.Trim();

        if (!await _db.Categories.AnyAsync(c => c.CategoryId == book.CategoryId))
        {
            ModelState.AddModelError(nameof(Book.CategoryId), "Thể loại không hợp lệ.");
        }
        if (!IsSafeCoverPath(book.CoverImageUrl))
        {
            ModelState.AddModelError(nameof(Book.CoverImageUrl), "Ảnh bìa phải là URL HTTPS hoặc đường dẫn nội bộ bắt đầu bằng /.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.BookCodePreview = await GetNextBookCodeAsync();
            await LoadCategories(book.CategoryId);
            return View(book);
        }

        book.AvailableCopies = book.TotalCopies;

        // Bộ sinh mã S001, S002... chạy độc lập với mã độc giả DB01, DB02...
        // Unique index trên Books.BookCode vẫn là lớp bảo vệ cuối cùng.
        for (var attempt = 0; attempt < 3; attempt++)
        {
            book.BookCode = await GetNextBookCodeAsync();
            _db.Books.Add(book);

            try
            {
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Đã thêm sách \"{book.Title}\" với mã {book.BookCode}.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                var attemptedCode = book.BookCode;
                _db.Entry(book).State = EntityState.Detached;
                book.BookId = 0;
                book.RowVersion = null;

                // Chỉ thử lại khi mã vừa được một yêu cầu đồng thời khác chiếm.
                var codeWasTaken = await _db.Books.AsNoTracking()
                    .AnyAsync(b => b.BookCode == attemptedCode);
                if (codeWasTaken && attempt < 2)
                {
                    continue;
                }

                if (codeWasTaken)
                {
                    ModelState.AddModelError(string.Empty,
                        "Chưa thể cấp mã sách vì có yêu cầu tạo sách đồng thời. Vui lòng bấm Lưu lần nữa.");
                }
                else
                {
                    ModelState.AddModelError(string.Empty,
                        "Không thể lưu dữ liệu sách. Vui lòng kiểm tra thông tin và thử lại.");
                }

                ViewBag.BookCodePreview = await GetNextBookCodeAsync();
                await LoadCategories(book.CategoryId);
                return View(book);
            }
        }

        ViewBag.BookCodePreview = await GetNextBookCodeAsync();
        await LoadCategories(book.CategoryId);
        ModelState.AddModelError(string.Empty, "Không thể cấp mã sách tự động. Vui lòng thử lại.");
        return View(book);
    }

    private async Task<string> GetNextBookCodeAsync()
    {
        var codes = await _db.Books.AsNoTracking()
            .Select(b => b.BookCode)
            .ToListAsync();

        long maxNumber = 0;
        foreach (var code in codes)
        {
            if (string.IsNullOrWhiteSpace(code) ||
                !code.StartsWith("S", StringComparison.OrdinalIgnoreCase) ||
                code.Length <= 1)
            {
                continue;
            }

            var suffix = code[1..];
            if (long.TryParse(suffix, NumberStyles.None, CultureInfo.InvariantCulture, out var number) &&
                number > maxNumber)
            {
                maxNumber = number;
            }
        }

        if (maxNumber == long.MaxValue)
        {
            throw new InvalidOperationException("Đã vượt giới hạn số thứ tự mã sách S.");
        }

        return $"S{maxNumber + 1:D3}";
    }

    [Authorize(Roles = AppRoles.Staff)]
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.BookId == id);
        if (book == null) return NotFound();
        await LoadCategories(book.CategoryId);
        return View(book);
    }

    [Authorize(Roles = AppRoles.Staff)]
    [HttpPost]
    public async Task<IActionResult> Edit(int id,
        [Bind("BookId,BookCode,Title,ISBN,CategoryId,Author,Publisher,PublishYear,TotalCopies,ShelfLocation,CoverImageUrl,Description,IsActive,RowVersion")] Book posted)
    {
        if (id != posted.BookId) return NotFound();

        var book = await _db.Books.FirstOrDefaultAsync(b => b.BookId == id);
        if (book == null) return NotFound();

        posted.BookCode = (posted.BookCode ?? string.Empty).Trim();

        if (await _db.Books.AnyAsync(b => b.BookCode == posted.BookCode && b.BookId != id))
        {
            ModelState.AddModelError(nameof(Book.BookCode), "Mã sách đã tồn tại.");
        }
        if (!await _db.Categories.AnyAsync(c => c.CategoryId == posted.CategoryId))
        {
            ModelState.AddModelError(nameof(Book.CategoryId), "Thể loại không hợp lệ.");
        }
        if (!IsSafeCoverPath(posted.CoverImageUrl))
        {
            ModelState.AddModelError(nameof(Book.CoverImageUrl), "Ảnh bìa phải là URL HTTPS hoặc đường dẫn nội bộ bắt đầu bằng /.");
        }

        // Số bản đang được mượn = tổng - còn lại
        var borrowed = book.TotalCopies - book.AvailableCopies;
        if (posted.TotalCopies < borrowed)
        {
            ModelState.AddModelError(nameof(Book.TotalCopies),
                $"Tổng số bản không được nhỏ hơn số bản đang được mượn ({borrowed}).");
        }

        if (!ModelState.IsValid)
        {
            posted.AvailableCopies = book.AvailableCopies;
            await LoadCategories(posted.CategoryId);
            return View(posted);
        }

        book.BookCode = posted.BookCode;
        book.Title = posted.Title;
        book.ISBN = posted.ISBN;
        book.CategoryId = posted.CategoryId;
        book.Author = posted.Author;
        book.Publisher = posted.Publisher;
        book.PublishYear = posted.PublishYear;
        book.ShelfLocation = posted.ShelfLocation;
        book.CoverImageUrl = posted.CoverImageUrl;
        book.Description = posted.Description;
        book.IsActive = posted.IsActive;
        book.TotalCopies = posted.TotalCopies;
        book.AvailableCopies = posted.TotalCopies - borrowed;

        // Kiểm soát cập nhật đồng thời bằng RowVersion
        _db.Entry(book).Property(b => b.RowVersion).OriginalValue = posted.RowVersion;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            ModelState.AddModelError(string.Empty,
                "Dữ liệu vừa được người khác thay đổi. Hãy mở lại trang sửa và thử lần nữa.");
            posted.AvailableCopies = book.AvailableCopies;
            await LoadCategories(posted.CategoryId);
            return View(posted);
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Không thể lưu dữ liệu. Vui lòng kiểm tra lại.");
            posted.AvailableCopies = book.AvailableCopies;
            await LoadCategories(posted.CategoryId);
            return View(posted);
        }

        TempData["Success"] = $"Đã cập nhật sách \"{book.Title}\".";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = AppRoles.Staff)]
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var book = await _db.Books.FirstOrDefaultAsync(b => b.BookId == id);
        if (book == null) return NotFound();

        // Giữ lịch sử: sách đã có lượt mượn hoặc yêu cầu đặt trước không bị xóa vật lý.
        if (await _db.LoanDetails.AnyAsync(d => d.BookId == id))
        {
            TempData["Error"] = "Không thể xóa: sách đã có lịch sử mượn. Hãy chuyển sang trạng thái ngừng lưu hành.";
            return RedirectToAction(nameof(Index));
        }
        if (await _db.BookReservations.AnyAsync(r => r.BookId == id))
        {
            TempData["Error"] = "Không thể xóa: sách đã có lịch sử đặt trước. Hãy chuyển sang trạng thái ngừng lưu hành.";
            return RedirectToAction(nameof(Index));
        }

        _db.Books.Remove(book);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Không thể xóa sách vì dữ liệu khác đang tham chiếu đến sách này.";
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = $"Đã xóa sách \"{book.Title}\".";
        return RedirectToAction(nameof(Index));
    }
    private static bool IsSafeCoverPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return true;
        var v = value.Trim();
        if (v.StartsWith("/", StringComparison.Ordinal) && !v.StartsWith("//", StringComparison.Ordinal))
            return !v.Contains('\\') && !v.Any(char.IsControl);
        return Uri.TryCreate(v, UriKind.Absolute, out var uri)
               && uri.Scheme == Uri.UriSchemeHttps
               && !string.IsNullOrWhiteSpace(uri.Host);
    }
}
