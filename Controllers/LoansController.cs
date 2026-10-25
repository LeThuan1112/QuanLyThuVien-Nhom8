using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThuVien.Data;
using ThuVien.Models;
using ThuVien.Services;
using ThuVien.ViewModels;

namespace ThuVien.Controllers;

[Authorize]
public class LoansController : Controller
{
    private const int PageSize = 10;
    private readonly AppDbContext _db;
    private readonly LoanService _loanService;
    private readonly SettingsService _settings;

    public LoansController(AppDbContext db, LoanService loanService, SettingsService settings)
    {
        _db = db;
        _loanService = loanService;
        _settings = settings;
    }

    private int CurrentUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Danh sách phiếu mượn / lịch sử giao dịch (có tìm kiếm, lọc, phân trang)
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<IActionResult> Index(string? keyword, string? status, int page = 1)
    {
        var query = _db.Loans.AsNoTracking()
            .Include(l => l.Reader)
            .Include(l => l.Details)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            var isNum = int.TryParse(k, out var num);
            query = query.Where(l => l.Reader!.ReaderCode.Contains(k)
                                     || l.Reader!.FullName.Contains(k)
                                     || (isNum && l.LoanId == num));
        }

        query = status switch
        {
            "borrowing" => query.Where(l => l.Details.Any(d => d.ReturnDate == null)),
            "overdue" => query.Where(l => l.DueDate < DateTime.Today && l.Details.Any(d => d.ReturnDate == null)),
            "returned" => query.Where(l => l.Details.All(d => d.ReturnDate != null)),
            _ => query
        };

        var ordered = query.OrderByDescending(l => l.LoanDate).ThenByDescending(l => l.LoanId);
        var total = await ordered.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var items = await ordered.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        return View(new LoanListViewModel
        {
            Loans = items,
            Keyword = keyword,
            Status = status,
            Page = page,
            PageSize = PageSize,
            TotalItems = total
        });
    }

    [Authorize(Roles = AppRoles.Staff)]
    public async Task<IActionResult> Details(int id)
    {
        var loan = await _db.Loans.AsNoTracking()
            .Include(l => l.Reader)
            .Include(l => l.CreatedByUser)
            .Include(l => l.Details).ThenInclude(d => d.Book)
            .Include(l => l.Details).ThenInclude(d => d.ReceivedByUser)
            .Include(l => l.Renewals).ThenInclude(r => r.RenewedByUser)
            .FirstOrDefaultAsync(l => l.LoanId == id);
        if (loan == null) return NotFound();
        var detailCfg = await _settings.GetAsync();
        ViewBag.MaxRenewals = detailCfg.MaxRenewals;
        ViewBag.RenewDays = detailCfg.RenewDays;
        ViewBag.RenewalFeePerBook = detailCfg.RenewalFeePerBook;
        return View(loan);
    }

    // ----- Lập phiếu mượn -----
    [Authorize(Roles = AppRoles.Staff)]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PrepareCreateForm(null);
        return View();
    }

    [Authorize(Roles = AppRoles.Staff)]
    [HttpPost]
    public async Task<IActionResult> Create(int readerId, List<int>? bookIds, string? note)
    {
        var ids = bookIds ?? new List<int>();
        var result = await _loanService.CreateLoanAsync(readerId, ids, CurrentUserId(), note);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "Không thể lập phiếu mượn.");
            await PrepareCreateForm(readerId);

            // Giữ lại giỏ sách đã chọn để không phải chọn lại
            var kept = await _db.Books.AsNoTracking()
                .Where(b => ids.Contains(b.BookId))
                .Select(b => new { id = b.BookId, code = b.BookCode, title = b.Title })
                .ToListAsync();
            ViewBag.InitialCartJson = JsonSerializer.Serialize(kept);
            ViewBag.Note = note;
            return View();
        }

        TempData["Success"] = $"Đã lập phiếu mượn #{result.Id}.";
        return RedirectToAction(nameof(Details), new { id = result.Id });
    }

    private async Task PrepareCreateForm(int? selectedReaderId)
    {
        var today = DateTime.Today;
        var readers = await _db.Readers.AsNoTracking()
            .Where(r => r.IsActive && r.CardExpiryDate >= today)
            .OrderBy(r => r.ReaderCode)
            .Select(r => new { r.ReaderId, Text = r.ReaderCode + " - " + r.FullName })
            .ToListAsync();
        ViewBag.Readers = new SelectList(readers, "ReaderId", "Text", selectedReaderId);
        var cfg = await _settings.GetAsync();
        ViewBag.DueDate = DateTime.Today.AddDays(cfg.LoanDays);
        ViewBag.LoanDays = cfg.LoanDays;
        ViewBag.MaxBooks = cfg.MaxBooksPerLoan;
        ViewBag.InitialCartJson ??= "[]";
    }

    // AJAX: tìm sách còn có thể cho mượn
    [Authorize(Roles = AppRoles.Staff)]
    [HttpGet]
    public async Task<IActionResult> SearchBooks(string? term)
    {
        var query = _db.Books.AsNoTracking().Where(b => b.IsActive && b.AvailableCopies > 0);
        if (!string.IsNullOrWhiteSpace(term))
        {
            var k = term.Trim();
            query = query.Where(b => b.Title.Contains(k) || b.BookCode.Contains(k) || b.Author.Contains(k));
        }

        var list = await query.OrderBy(b => b.Title).Take(10)
            .Select(b => new { id = b.BookId, code = b.BookCode, title = b.Title, author = b.Author, available = b.AvailableCopies })
            .ToListAsync();
        return Json(list);
    }

    // ----- Trả sách -----
    [Authorize(Roles = AppRoles.Staff)]
    [HttpPost]
    public async Task<IActionResult> Return(int id, int loanId)
    {
        var result = await _loanService.ReturnAsync(id, CurrentUserId());
        if (result.Success) TempData["Success"] = result.Fine > 0
            ? $"Đã ghi nhận trả sách. Phí phạt quá hạn: {result.Fine:N0} đ."
            : "Đã ghi nhận trả sách.";
        else TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Details), new { id = loanId });
    }

    [Authorize(Roles = AppRoles.Staff)]
    [HttpPost]
    public async Task<IActionResult> ReturnAll(int id)
    {
        var count = await _loanService.ReturnAllAsync(id, CurrentUserId());
        if (count > 0) TempData["Success"] = $"Đã ghi nhận trả {count} cuốn.";
        else TempData["Error"] = "Không có cuốn nào cần trả trong phiếu này.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ----- Gia hạn phiếu mượn (nhân viên, hoặc chính độc giả sở hữu phiếu) -----
    [Authorize(Roles = AppRoles.Staff + "," + AppRoles.Reader)]
    [HttpPost]
    public async Task<IActionResult> Renew(int id)
    {
        var isStaff = User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Librarian);
        if (!isStaff)
        {
            var userId = CurrentUserId();
            var owns = await _db.Loans.AnyAsync(l => l.LoanId == id && l.Reader!.UserId == userId);
            if (!owns) return Forbid();
        }

        var result = await _loanService.RenewAsync(id, CurrentUserId());
        if (result.Success)
        {
            var cfg = await _settings.GetAsync();
            TempData["Success"] = $"Đã gia hạn phiếu #{id} thêm {cfg.RenewDays} ngày. Phí gia hạn phát sinh: {result.RenewalFee:N0} đ.";
        }
        else
        {
            TempData["Error"] = result.Error;
        }

        return isStaff
            ? RedirectToAction(nameof(Details), new { id })
            : RedirectToAction(nameof(MyLoans));
    }

    // ----- Sách quá hạn -----
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<IActionResult> Overdue()
    {
        var items = await _db.LoanDetails.AsNoTracking()
            .Include(d => d.Book)
            .Include(d => d.Loan).ThenInclude(l => l!.Reader)
            .Where(d => d.ReturnDate == null && d.Loan!.DueDate < DateTime.Today)
            .OrderBy(d => d.Loan!.DueDate)
            .ToListAsync();
        return View(items);
    }

    // ----- Độc giả xem phiếu mượn của chính mình -----
    [Authorize(Roles = AppRoles.Reader)]
    public async Task<IActionResult> MyLoans()
    {
        // Cấu hình luôn được gán trước khi trả về View, kể cả tài khoản chưa liên kết hồ sơ.
        var myCfg = await _settings.GetAsync();
        ViewBag.MaxRenewals = myCfg.MaxRenewals;
        ViewBag.RenewDays = myCfg.RenewDays;
        ViewBag.RenewalFeePerBook = myCfg.RenewalFeePerBook;

        var userId = CurrentUserId();
        var reader = await _db.Readers.AsNoTracking().FirstOrDefaultAsync(r => r.UserId == userId);
        if (reader == null) return View(new MyLoansViewModel());

        var items = await _db.LoanDetails.AsNoTracking()
            .Include(d => d.Book)
            .Include(d => d.Loan).ThenInclude(l => l!.Renewals)
            .Where(d => d.Loan!.ReaderId == reader.ReaderId)
            .OrderByDescending(d => d.Loan!.LoanDate)
            .ToListAsync();

        return View(new MyLoansViewModel { Reader = reader, Items = items });
    }
}
