using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuVien.Data;
using ThuVien.Models;
using ThuVien.Services;

namespace ThuVien.Controllers;

[Authorize]
public class ReservationsController : Controller
{
    private readonly AppDbContext _db;
    private readonly ReservationService _reservations;

    public ReservationsController(AppDbContext db, ReservationService reservations)
    {
        _db = db;
        _reservations = reservations;
    }

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    [Authorize(Roles = AppRoles.Staff)]
    public async Task<IActionResult> Index()
    {
        await _reservations.RefreshQueueAsync();
        var items = await _db.BookReservations.AsNoTracking()
            .Include(r => r.Book)
            .Include(r => r.Reader)
            .Where(r => r.Status == ReservationStatuses.Waiting || r.Status == ReservationStatuses.Ready)
            .OrderBy(r => r.Book!.Title)
            .ThenBy(r => r.RequestedAt)
            .Take(200)
            .ToListAsync();
        return View(items);
    }

    [Authorize(Roles = AppRoles.Reader)]
    public async Task<IActionResult> My()
    {
        await _reservations.RefreshQueueAsync();
        if (!TryGetUserId(out var userId)) return Challenge();
        var reader = await _db.Readers.AsNoTracking().FirstOrDefaultAsync(r => r.UserId == userId);
        ViewBag.ReaderLinked = reader != null;
        var items = reader == null
            ? new List<BookReservation>()
            : await _db.BookReservations.AsNoTracking()
                .Include(r => r.Book)
                .Where(r => r.ReaderId == reader.ReaderId)
                .OrderByDescending(r => r.RequestedAt)
                .Take(100)
                .ToListAsync();

        var waitingBookIds = items.Where(r => r.Status == ReservationStatuses.Waiting)
            .Select(r => r.BookId).Distinct().ToList();
        var positions = new Dictionary<int, int>();
        if (waitingBookIds.Count > 0)
        {
            var queueRows = await _db.BookReservations.AsNoTracking()
                .Where(r => waitingBookIds.Contains(r.BookId) && r.Status == ReservationStatuses.Waiting)
                .OrderBy(r => r.BookId).ThenBy(r => r.RequestedAt).ThenBy(r => r.BookReservationId)
                .Select(r => new { r.BookReservationId, r.BookId })
                .ToListAsync();
            foreach (var group in queueRows.GroupBy(r => r.BookId))
            {
                var position = 0;
                foreach (var row in group)
                    positions[row.BookReservationId] = ++position;
            }
        }
        ViewBag.QueuePositions = positions;
        return View(items);
    }

    [Authorize(Roles = AppRoles.Reader)]
    [HttpPost]
    public async Task<IActionResult> Create(int bookId)
    {
        if (!TryGetUserId(out var userId)) return Challenge();
        var reader = await _db.Readers.AsNoTracking().FirstOrDefaultAsync(r => r.UserId == userId);
        if (reader == null)
        {
            TempData["Error"] = "Tài khoản của bạn chưa được liên kết với hồ sơ độc giả. Vui lòng liên hệ thủ thư.";
            return RedirectToAction("Details", "Books", new { id = bookId });
        }

        var result = await _reservations.CreateWaitingAsync(reader.ReaderId, bookId);
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "Đã thêm yêu cầu đặt trước. Hệ thống sẽ thông báo trạng thái khi sách có bản sẵn sàng."
            : result.Error;
        return RedirectToAction(nameof(My));
    }

    [Authorize(Roles = AppRoles.Reader)]
    [HttpPost]
    public async Task<IActionResult> Cancel(int id)
    {
        if (!TryGetUserId(out var userId)) return Challenge();
        var readerId = await _db.Readers.AsNoTracking()
            .Where(r => r.UserId == userId)
            .Select(r => (int?)r.ReaderId)
            .FirstOrDefaultAsync();
        if (!readerId.HasValue)
        {
            TempData["Error"] = "Tài khoản chưa được liên kết với hồ sơ độc giả.";
            return RedirectToAction(nameof(My));
        }

        var result = await _reservations.CancelAsync(readerId.Value, id);
        TempData[result.Success ? "Success" : "Error"] = result.Success
            ? "Đã hủy yêu cầu đặt trước."
            : result.Error;
        return RedirectToAction(nameof(My));
    }
}
