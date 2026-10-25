using System.Data;
using Microsoft.EntityFrameworkCore;
using ThuVien.Data;
using ThuVien.Models;

namespace ThuVien.Services;

public record ReservationResult(bool Success, string? Error = null, int? Id = null);

/// <summary>Quản lý hàng chờ đặt trước sách. Phí và thanh toán không nằm trong nghiệp vụ đặt trước.</summary>
public class ReservationService
{
    public const int PickupWindowDays = 3;
    private readonly AppDbContext _db;
    private readonly ILogger<ReservationService> _logger;

    public ReservationService(AppDbContext db, ILogger<ReservationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<ReservationResult> CreateWaitingAsync(int readerId, int bookId)
    {
        // Đồng bộ trạng thái hết thời hạn trước khi quyết định có thể đặt trước hay không.
        await PromoteAvailableAsync(bookId);

        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var reader = await _db.Readers.FirstOrDefaultAsync(r => r.ReaderId == readerId);
            if (reader == null || !reader.IsActive || reader.CardExpiryDate.Date < DateTime.Today)
            {
                await tx.RollbackAsync();
                return new ReservationResult(false, "Thẻ độc giả không hợp lệ, đang bị khóa hoặc đã hết hạn.");
            }

            var book = await _db.Books.FirstOrDefaultAsync(b => b.BookId == bookId && b.IsActive);
            if (book == null)
            {
                await tx.RollbackAsync();
                return new ReservationResult(false, "Sách không tồn tại hoặc đã ngừng lưu hành.");
            }
            var now = DateTime.Now;
            var activeReadyCount = await _db.BookReservations.CountAsync(r =>
                r.BookId == bookId && r.Status == ReservationStatuses.Ready &&
                r.ExpiresAt != null && r.ExpiresAt > now);
            // Chỉ từ chối khi còn ít nhất một bản chưa bị giữ cho người khác.
            // Nếu tất cả bản đang rảnh đã dành cho người trong hàng chờ, vẫn cho phép xếp hàng tiếp.
            if (book.AvailableCopies > activeReadyCount)
            {
                await tx.RollbackAsync();
                return new ReservationResult(false, "Sách hiện còn bản chưa được giữ chỗ. Vui lòng liên hệ thủ thư để mượn trực tiếp.");
            }

            var duplicate = await _db.BookReservations.AnyAsync(r =>
                r.ReaderId == readerId && r.BookId == bookId &&
                (r.Status == ReservationStatuses.Waiting ||
                 (r.Status == ReservationStatuses.Ready && r.ExpiresAt != null && r.ExpiresAt > now)));
            if (duplicate)
            {
                await tx.RollbackAsync();
                return new ReservationResult(false, "Bạn đã có yêu cầu đặt trước đang chờ cho cuốn sách này.");
            }

            var reservation = new BookReservation
            {
                ReaderId = readerId,
                BookId = bookId,
                RequestedAt = now,
                Status = ReservationStatuses.Waiting
            };
            _db.BookReservations.Add(reservation);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return new ReservationResult(true, null, reservation.BookReservationId);
        }
        catch (DbUpdateException ex)
        {
            await tx.RollbackAsync();
            _logger.LogWarning(ex, "Không thể tạo đặt trước sách {BookId} cho độc giả {ReaderId}", bookId, readerId);
            return new ReservationResult(false, "Không thể lưu yêu cầu đặt trước. Vui lòng thử lại.");
        }
    }

    /// <summary>
    /// Khi sách có bản rảnh, chuyển các yêu cầu chờ lâu nhất sang Ready theo số bản rảnh.
    /// Sách Ready vẫn được tính tồn kho vật lý, nhưng LoanService bảo vệ phần bản đã giữ chỗ.
    /// </summary>
    public async Task PromoteAvailableAsync(int bookId)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var now = DateTime.Now;
            await _db.BookReservations
                .Where(r => r.BookId == bookId && r.Status == ReservationStatuses.Ready
                            && r.ExpiresAt != null && r.ExpiresAt <= now)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Status, ReservationStatuses.Expired));

            var book = await _db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.BookId == bookId);
            if (book == null || !book.IsActive || book.AvailableCopies <= 0)
            {
                await tx.CommitAsync();
                return;
            }

            var readyCount = await _db.BookReservations.CountAsync(r =>
                r.BookId == bookId && r.Status == ReservationStatuses.Ready && r.ExpiresAt > now);
            var slots = Math.Max(0, book.AvailableCopies - readyCount);
            if (slots > 0)
            {
                var waiting = await _db.BookReservations
                    .Where(r => r.BookId == bookId && r.Status == ReservationStatuses.Waiting)
                    .OrderBy(r => r.RequestedAt)
                    .ThenBy(r => r.BookReservationId)
                    .Take(slots)
                    .ToListAsync();

                foreach (var reservation in waiting)
                {
                    reservation.Status = ReservationStatuses.Ready;
                    reservation.ReadyAt = now;
                    reservation.ExpiresAt = now.AddDays(PickupWindowDays);
                }
                await _db.SaveChangesAsync();
            }

            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }

    public async Task RefreshQueueAsync()
    {
        var expiredBookIds = await _db.BookReservations.AsNoTracking()
            .Where(r => r.Status == ReservationStatuses.Ready && r.ExpiresAt != null && r.ExpiresAt <= DateTime.Now)
            .Select(r => r.BookId).Distinct().ToListAsync();
        // Chỉ xét sách có hàng chờ và hiện còn bản; tránh mở transaction cho toàn bộ danh mục mỗi chu kỳ.
        var availableBookIds = await _db.Books.AsNoTracking()
            .Where(b => b.IsActive && b.AvailableCopies > 0
                        && _db.BookReservations.Any(r => r.BookId == b.BookId && r.Status == ReservationStatuses.Waiting))
            .Select(b => b.BookId).ToListAsync();

        foreach (var bookId in expiredBookIds.Concat(availableBookIds).Distinct())
        {
            try { await PromoteAvailableAsync(bookId); }
            catch (Exception ex) { _logger.LogWarning(ex, "Không thể làm mới hàng chờ đặt trước cho sách {BookId}", bookId); }
        }
    }

    public async Task<ReservationResult> CancelAsync(int readerId, int reservationId)
    {
        var reservation = await _db.BookReservations
            .FirstOrDefaultAsync(r => r.BookReservationId == reservationId && r.ReaderId == readerId);
        if (reservation == null)
            return new ReservationResult(false, "Không tìm thấy yêu cầu đặt trước của bạn.");
        if (reservation.Status != ReservationStatuses.Waiting && reservation.Status != ReservationStatuses.Ready)
            return new ReservationResult(false, "Yêu cầu này không còn có thể hủy.");

        var bookId = reservation.BookId;
        reservation.Status = ReservationStatuses.Cancelled;
        reservation.CancelledAt = DateTime.Now;
        await _db.SaveChangesAsync();

        try { await PromoteAvailableAsync(bookId); }
        catch (Exception ex) { _logger.LogWarning(ex, "Không thể thăng hạng hàng chờ sau khi hủy reservation {Id}", reservationId); }
        return new ReservationResult(true, null, reservationId);
    }
}
