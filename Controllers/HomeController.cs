using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuVien.Data;
using ThuVien.Models;
using ThuVien.ViewModels;

namespace ThuVien.Controllers;

public class HomeController : Controller
{
    private readonly AppDbContext _db;

    public HomeController(AppDbContext db)
    {
        _db = db;
    }

    // Trang chủ công khai (nhân viên được chuyển thẳng tới Bảng điều khiển)
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        if (User.IsInRole(AppRoles.Admin) || User.IsInRole(AppRoles.Librarian))
        {
            return RedirectToAction(nameof(Dashboard));
        }

        var categories = await _db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new CategoryChip { Id = c.CategoryId, Name = c.Name, Count = c.Books.Count(b => b.IsActive) })
            .ToListAsync();

        var latest = await _db.Books.AsNoTracking()
            .Include(b => b.Category)
            .Where(b => b.IsActive)
            .OrderByDescending(b => b.BookId)
            .Take(6)
            .ToListAsync();

        // Thống kê sách phổ biến được tính từ số lượt ghi nhận trong chi tiết phiếu mượn.
        // Không dùng dữ liệu giả; khi thư viện chưa có lịch sử mượn, danh sách được để trống.
        var popularRows = await _db.LoanDetails.AsNoTracking()
            .GroupBy(d => d.BookId)
            .Select(g => new { BookId = g.Key, BorrowCount = g.Count() })
            .OrderByDescending(x => x.BorrowCount)
            .ThenBy(x => x.BookId)
            .Take(6)
            .ToListAsync();
        var popularIds = popularRows.Select(x => x.BookId).ToList();
        var popularEntities = await _db.Books.AsNoTracking()
            .Include(b => b.Category)
            .Where(b => b.IsActive && popularIds.Contains(b.BookId))
            .ToListAsync();
        var popular = popularIds
            .Select(id => popularEntities.FirstOrDefault(b => b.BookId == id))
            .Where(b => b != null)
            .Cast<Book>()
            .ToList();

        var vm = new HomeViewModel
        {
            BookTitles = await _db.Books.CountAsync(b => b.IsActive),
            Readers = await _db.Readers.CountAsync(r => r.IsActive),
            AvailableCopies = await GetAvailableToBorrowCountAsync(),
            Categories = categories,
            LatestBooks = latest,
            PopularBooks = popular
        };
        return View(vm);
    }


    private async Task<int> GetAvailableToBorrowCountAsync()
    {
        var now = DateTime.Now;
        var stocks = await _db.Books.AsNoTracking()
            .Where(b => b.IsActive)
            .Select(b => new { b.BookId, b.AvailableCopies })
            .ToListAsync();
        if (stocks.Count == 0) return 0;

        var ids = stocks.Select(b => b.BookId).ToList();
        var holds = await _db.BookReservations.AsNoTracking()
            .Where(r => ids.Contains(r.BookId)
                        && r.Status == ReservationStatuses.Ready
                        && r.ExpiresAt != null && r.ExpiresAt > now)
            .GroupBy(r => r.BookId)
            .Select(g => new { BookId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.BookId, x => x.Count);

        return stocks.Sum(b => Math.Max(0, b.AvailableCopies - (holds.TryGetValue(b.BookId, out var count) ? count : 0)));
    }

    // Bảng điều khiển cho Admin và Thủ thư
    [Authorize(Roles = AppRoles.Staff)]
    public async Task<IActionResult> Dashboard()
    {
        var today = DateTime.Today;
        var from = today.AddDays(-13);

        var vm = new DashboardViewModel
        {
            BookTitles = await _db.Books.CountAsync(),
            TotalCopies = await _db.Books.SumAsync(b => (int?)b.TotalCopies) ?? 0,
            AvailableCopies = await GetAvailableToBorrowCountAsync(),
            Readers = await _db.Readers.CountAsync(),
            Borrowing = await _db.LoanDetails.CountAsync(d => d.ReturnDate == null),
            Overdue = await _db.LoanDetails.CountAsync(d => d.ReturnDate == null && d.Loan!.DueDate < today)
        };

        // Biểu đồ mượn / trả 14 ngày gần đây (gom nhóm trong bộ nhớ vì dữ liệu nhỏ)
        var borrowedDates = await _db.LoanDetails.AsNoTracking()
            .Where(d => d.Loan!.LoanDate >= from)
            .Select(d => d.Loan!.LoanDate)
            .ToListAsync();
        var returnedDates = await _db.LoanDetails.AsNoTracking()
            .Where(d => d.ReturnDate != null && d.ReturnDate >= from)
            .Select(d => d.ReturnDate!.Value)
            .ToListAsync();

        for (var i = 0; i < 14; i++)
        {
            var day = from.AddDays(i);
            vm.DayLabels.Add(day.ToString("dd/MM"));
            vm.BorrowedPerDay.Add(borrowedDates.Count(x => x.Date == day));
            vm.ReturnedPerDay.Add(returnedDates.Count(x => x.Date == day));
        }

        // Sách theo thể loại
        var cats = await _db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new { c.Name, Count = c.Books.Count() })
            .ToListAsync();
        vm.CategoryNames = cats.Select(c => c.Name).ToList();
        vm.CategoryCounts = cats.Select(c => c.Count).ToList();

        // Top sách được mượn nhiều
        var top = await _db.LoanDetails.AsNoTracking()
            .GroupBy(d => d.BookId)
            .Select(g => new { BookId = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();
        var topIds = top.Select(t => t.BookId).ToList();
        var titles = await _db.Books.AsNoTracking()
            .Where(b => topIds.Contains(b.BookId))
            .ToDictionaryAsync(b => b.BookId, b => b.Title);
        vm.TopBooks = top.Select(t => new TopBookItem
        {
            Title = titles.TryGetValue(t.BookId, out var title) ? title : "?",
            Count = t.Count
        }).ToList();

        // Phiếu mượn gần đây và cảnh báo quá hạn
        vm.RecentLoans = await _db.Loans.AsNoTracking()
            .Include(l => l.Reader)
            .Include(l => l.Details)
            .OrderByDescending(l => l.LoanDate)
            .Take(5)
            .ToListAsync();

        vm.OverdueItems = await _db.LoanDetails.AsNoTracking()
            .Include(d => d.Book)
            .Include(d => d.Loan).ThenInclude(l => l!.Reader)
            .Where(d => d.ReturnDate == null && d.Loan!.DueDate < today)
            .OrderBy(d => d.Loan!.DueDate)
            .Take(5)
            .ToListAsync();

        if (User.IsInRole(AppRoles.Admin))
        {
            // Số liệu quản trị hệ thống
            vm.TotalUsers = await _db.Users.CountAsync();
            vm.AdminCount = await _db.Users.CountAsync(u => u.Role == AppRoles.Admin);
            vm.LibrarianCount = await _db.Users.CountAsync(u => u.Role == AppRoles.Librarian);
            vm.ReaderAccountCount = await _db.Users.CountAsync(u => u.Role == AppRoles.Reader);
            vm.LockedUsers = await _db.Users.CountAsync(u => !u.IsActive);
            vm.RecentLogs = await _db.AuditLogs.AsNoTracking()
                .OrderByDescending(l => l.CreatedAt)
                .Take(6)
                .ToListAsync();
        }
        else
        {
            // Việc cần làm hằng ngày của Thủ thư
            var endOfToday = today.AddDays(1);
            vm.DueToday = await _db.LoanDetails.AsNoTracking()
                .Include(d => d.Book)
                .Include(d => d.Loan).ThenInclude(l => l!.Reader)
                .Where(d => d.ReturnDate == null && d.Loan!.DueDate >= today && d.Loan.DueDate < endOfToday)
                .OrderBy(d => d.Loan!.DueDate)
                .Take(6)
                .ToListAsync();
            vm.OutOfStock = await _db.Books.AsNoTracking()
                .Where(b => b.IsActive && b.AvailableCopies == 0)
                .OrderBy(b => b.Title)
                .Take(6)
                .ToListAsync();
        }

        return View(vm);
    }

    // Trang chủ của Độc giả
    [Authorize(Roles = AppRoles.Reader)]
    public async Task<IActionResult> ReaderHome()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var reader = await _db.Readers.AsNoTracking().FirstOrDefaultAsync(r => r.UserId == userId);
        var vm = new ReaderHomeViewModel { Reader = reader };

        if (reader != null)
        {
            var today = DateTime.Today;
            var items = await _db.LoanDetails.AsNoTracking()
                .Include(d => d.Book)
                .Include(d => d.Loan)
                .Where(d => d.Loan!.ReaderId == reader.ReaderId)
                .ToListAsync();

            var current = items.Where(d => d.ReturnDate == null).OrderBy(d => d.Loan!.DueDate).ToList();
            vm.TotalBorrowed = items.Count;
            vm.Current = current;
            vm.Overdue = current.Count(d => d.Loan!.DueDate.Date < today);
            vm.DueSoon = current.Count(d => d.Loan!.DueDate.Date >= today && d.Loan.DueDate.Date <= today.AddDays(3));
        }
        return View(vm);
    }

    [AllowAnonymous]
    public IActionResult StatusPage(int code)
    {
        ViewBag.Code = code;
        return View();
    }

    [AllowAnonymous]
    public IActionResult Error()
    {
        return View();
    }
}
