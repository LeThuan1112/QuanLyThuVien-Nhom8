using Microsoft.EntityFrameworkCore;
using ThuVien.Data;
using ThuVien.Models;

namespace ThuVien.Services;

public record ServiceResult(bool Success, string? Error = null, int? Id = null, decimal Fine = 0, decimal RenewalFee = 0);

// Nghiệp vụ mượn/trả/gia hạn. Mọi thay đổi số lượng sách nằm trong transaction.
public class LoanService
{
    private readonly AppDbContext _db;
    private readonly SettingsService _settings;
    private readonly ReservationService _reservations;
    private readonly ILogger<LoanService> _logger;

    public LoanService(AppDbContext db, SettingsService settings, ReservationService reservations, ILogger<LoanService> logger)
    {
        _db = db;
        _settings = settings;
        _reservations = reservations;
        _logger = logger;
    }

    public async Task<ServiceResult> CreateLoanAsync(int readerId, IEnumerable<int> bookIds, int staffUserId, string? note)
    {
        var cfg = await _settings.GetAsync();
        var ids = bookIds.Distinct().ToList();
        if (ids.Count == 0)
            return new ServiceResult(false, "Vui lòng chọn ít nhất một cuốn sách.");
        if (ids.Count > cfg.MaxBooksPerLoan)
            return new ServiceResult(false, $"Mỗi phiếu mượn tối đa {cfg.MaxBooksPerLoan} đầu sách.");

        var reader = await _db.Readers.AsNoTracking().FirstOrDefaultAsync(r => r.ReaderId == readerId);
        if (reader == null)
            return new ServiceResult(false, "Vui lòng chọn độc giả hợp lệ.");
        if (!reader.IsActive)
            return new ServiceResult(false, "Thẻ độc giả đang bị khóa, không thể mượn sách.");
        if (reader.CardExpiryDate.Date < DateTime.Today)
            return new ServiceResult(false, "Thẻ độc giả đã hết hạn, không thể mượn sách.");

        var borrowing = await _db.LoanDetails.CountAsync(d => d.ReturnDate == null && d.Loan!.ReaderId == readerId);
        if (borrowing + ids.Count > cfg.MaxBooksAtOnce)
            return new ServiceResult(false,
                $"Độc giả đang mượn {borrowing} cuốn; tối đa được mượn cùng lúc {cfg.MaxBooksAtOnce} cuốn.");

        var books = await _db.Books.AsNoTracking().Where(b => ids.Contains(b.BookId)).ToListAsync();
        if (books.Count != ids.Count)
            return new ServiceResult(false, "Có sách không tồn tại trong hệ thống.");

        // Làm mới các lượt giữ chỗ hết hạn và bảo đảm hàng chờ được ưu tiên trước khi lập phiếu.
        foreach (var b in books)
        {
            try { await _reservations.PromoteAvailableAsync(b.BookId); }
            catch (Exception ex) { _logger.LogWarning(ex, "Không làm mới được hàng chờ của sách {BookId}", b.BookId); }
        }

        await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            // Kiểm tra lại điều kiện của độc giả bên trong transaction.
            // Kiểm tra nhanh phía trên giúp phản hồi sớm, nhưng không đủ chống hai yêu cầu
            // mượn đồng thời cùng vượt giới hạn MaxBooksAtOnce.
            var currentReader = await _db.Readers
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ReaderId == readerId);
            if (currentReader == null)
            {
                await tx.RollbackAsync();
                return new ServiceResult(false, "Vui lòng chọn độc giả hợp lệ.");
            }
            if (!currentReader.IsActive)
            {
                await tx.RollbackAsync();
                return new ServiceResult(false, "Thẻ độc giả đang bị khóa, không thể mượn sách.");
            }
            if (currentReader.CardExpiryDate.Date < DateTime.Today)
            {
                await tx.RollbackAsync();
                return new ServiceResult(false, "Thẻ độc giả đã hết hạn, không thể mượn sách.");
            }

            var borrowingInTransaction = await _db.LoanDetails.CountAsync(d =>
                d.ReturnDate == null && d.Loan != null && d.Loan.ReaderId == readerId);
            if (borrowingInTransaction + ids.Count > cfg.MaxBooksAtOnce)
            {
                await tx.RollbackAsync();
                return new ServiceResult(false,
                    $"Độc giả đang mượn {borrowingInTransaction} cuốn; tối đa được mượn cùng lúc {cfg.MaxBooksAtOnce} cuốn.");
            }

            foreach (var b in books)
            {
                // Nếu bản sách đã được giữ cho độc giả đứng đầu hàng chờ, người khác không thể lấy thay.
                var now = DateTime.Now;
                var activeReadyCount = await _db.BookReservations.CountAsync(r =>
                    r.BookId == b.BookId && r.Status == ReservationStatuses.Ready
                    && r.ExpiresAt != null && r.ExpiresAt > now);
                var readerHasReadyReservation = await _db.BookReservations.AnyAsync(r =>
                    r.BookId == b.BookId && r.ReaderId == readerId
                    && r.Status == ReservationStatuses.Ready
                    && r.ExpiresAt != null && r.ExpiresAt > now);
                var availableBeforeLoan = await _db.Books.AsNoTracking()
                    .Where(x => x.BookId == b.BookId && x.IsActive)
                    .Select(x => (int?)x.AvailableCopies)
                    .FirstOrDefaultAsync() ?? 0;
                if (availableBeforeLoan <= activeReadyCount && !readerHasReadyReservation)
                {
                    await tx.RollbackAsync();
                    return new ServiceResult(false, $"Sách \"{b.Title}\" đang được giữ cho độc giả trong hàng chờ đặt trước.");
                }

                var affected = await _db.Books
                    .Where(x => x.BookId == b.BookId && x.IsActive && x.AvailableCopies > 0)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.AvailableCopies, x => x.AvailableCopies - 1));

                if (affected == 0)
                {
                    await tx.RollbackAsync();
                    return new ServiceResult(false, $"Sách \"{b.Title}\" đã hết bản hoặc ngừng lưu hành.");
                }
            }

            var loan = new Loan
            {
                ReaderId = readerId,
                CreatedByUserId = staffUserId,
                LoanDate = DateTime.Now,
                DueDate = DateTime.Today.AddDays(cfg.LoanDays + 1).AddSeconds(-1),
                Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim()
            };
            foreach (var b in books)
            {
                loan.Details.Add(new LoanDetail { BookId = b.BookId });
            }

            _db.Loans.Add(loan);
            await _db.SaveChangesAsync();

            // Ghi nhận hoàn tất các lượt giữ chỗ của đúng độc giả khi phiếu mượn đã được tạo.
            var fulfilledAt = DateTime.Now;
            await _db.BookReservations
                .Where(r => ids.Contains(r.BookId) && r.ReaderId == readerId
                            && r.Status == ReservationStatuses.Ready
                            && r.ExpiresAt != null && r.ExpiresAt > fulfilledAt)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Status, ReservationStatuses.Fulfilled)
                    .SetProperty(r => r.FulfilledAt, (DateTime?)fulfilledAt)
                    .SetProperty(r => r.FulfilledLoanId, (int?)loan.LoanId));

            await tx.CommitAsync();
            foreach (var b in books)
            {
                try { await _reservations.PromoteAvailableAsync(b.BookId); }
                catch (Exception ex) { _logger.LogWarning(ex, "Không thăng hạng được hàng chờ sau phiếu mượn {LoanId}", loan.LoanId); }
            }
            return new ServiceResult(true, null, loan.LoanId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể lập phiếu mượn cho độc giả {ReaderId}", readerId);
            await tx.RollbackAsync();
            return new ServiceResult(false, "Không thể lập phiếu do lỗi hệ thống. Vui lòng thử lại.");
        }
    }

    public async Task<ServiceResult> ReturnAsync(int loanDetailId, int staffUserId)
    {
        var detail = await _db.LoanDetails.AsNoTracking()
            .Include(d => d.Loan)
            .FirstOrDefaultAsync(d => d.LoanDetailId == loanDetailId);
        if (detail == null)
            return new ServiceResult(false, "Không tìm thấy dòng chi tiết phiếu mượn.");

        var cfg = await _settings.GetAsync();

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var now = DateTime.Now;

            // Tính theo ngày lịch: trả trước hoặc trong ngày hạn trả không bị phạt;
            // từ ngày tiếp theo, mỗi ngày quá hạn tính một ngày phạt.
            var fine = detail.Loan == null
                ? 0m
                : LibraryRules.CalculateLateFine(detail.Loan.DueDate, now, cfg.FinePerDay);

            // BR06: chỉ đánh dấu trả nếu chưa trả; nếu đã trả rồi thì affected = 0
            var affected = await _db.LoanDetails
                .Where(d => d.LoanDetailId == loanDetailId && d.ReturnDate == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.ReturnDate, (DateTime?)now)
                    .SetProperty(d => d.ReceivedByUserId, (int?)staffUserId)
                    .SetProperty(d => d.FineAmount, fine));

            if (affected == 0)
            {
                await tx.RollbackAsync();
                return new ServiceResult(false, "Cuốn sách này đã được trả trước đó.");
            }

            await _db.Books
                .Where(b => b.BookId == detail.BookId)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.AvailableCopies, b => b.AvailableCopies + 1));

            await tx.CommitAsync();
            try { await _reservations.PromoteAvailableAsync(detail.BookId); }
            catch (Exception ex) { _logger.LogWarning(ex, "Không thể thăng hạng hàng chờ sau khi trả sách {BookId}", detail.BookId); }
            return new ServiceResult(true, null, detail.LoanId, fine);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể ghi nhận trả sách cho chi tiết phiếu {LoanDetailId}", loanDetailId);
            await tx.RollbackAsync();
            return new ServiceResult(false, "Không thể ghi nhận trả sách do lỗi hệ thống. Vui lòng thử lại.");
        }
    }

    public async Task<int> ReturnAllAsync(int loanId, int staffUserId)
    {
        var detailIds = await _db.LoanDetails
            .Where(d => d.LoanId == loanId && d.ReturnDate == null)
            .Select(d => d.LoanDetailId)
            .ToListAsync();

        var count = 0;
        foreach (var id in detailIds)
        {
            var r = await ReturnAsync(id, staffUserId);
            if (r.Success) count++;
        }
        return count;
    }

    // Gia hạn: cộng ngày cấu hình, lưu lịch sử và tính phí riêng theo số cuốn chưa trả.
    // FineAmount vẫn chỉ dùng cho phí phạt quá hạn khi trả sách.
    public async Task<ServiceResult> RenewAsync(int loanId, int renewedByUserId)
    {
        var cfg = await _settings.GetAsync();
        if (cfg.MaxRenewals <= 0)
            return new ServiceResult(false, "Hệ thống hiện không cho phép gia hạn.");

        await using var tx = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        try
        {
            var loan = await _db.Loans
                .Include(l => l.Reader)
                .Include(l => l.Details)
                .FirstOrDefaultAsync(l => l.LoanId == loanId);

            if (loan == null)
            {
                await tx.RollbackAsync();
                return new ServiceResult(false, "Không tìm thấy phiếu mượn.");
            }
            var outstandingCopies = loan.Details.Count(d => d.ReturnDate == null);
            if (outstandingCopies == 0)
            {
                await tx.RollbackAsync();
                return new ServiceResult(false, "Phiếu này đã trả hết sách, không thể gia hạn.");
            }
            if (loan.DueDate.Date < DateTime.Today)
            {
                await tx.RollbackAsync();
                return new ServiceResult(false, "Phiếu đã quá hạn, không thể gia hạn. Vui lòng trả sách.");
            }
            if (loan.RenewCount >= cfg.MaxRenewals)
            {
                await tx.RollbackAsync();
                return new ServiceResult(false, $"Phiếu đã gia hạn tối đa {cfg.MaxRenewals} lần.");
            }
            if (loan.Reader == null || !loan.Reader.IsActive || loan.Reader.CardExpiryDate.Date < DateTime.Today)
            {
                await tx.RollbackAsync();
                return new ServiceResult(false, "Thẻ độc giả đang bị khóa hoặc đã hết hạn.");
            }

            var currentRenewCount = loan.RenewCount;
            var newRenewCount = currentRenewCount + 1;
            var newDue = loan.DueDate.AddDays(cfg.RenewDays);
            var renewalFee = LibraryRules.CalculateRenewalFee(outstandingCopies, cfg.RenewalFeePerBook);

            // Điều kiện cập nhật giúp ngăn hai yêu cầu gia hạn cùng dùng chung một lượt.
            var affected = await _db.Loans
                .Where(l => l.LoanId == loanId
                            && l.RenewCount == currentRenewCount
                            && l.DueDate >= DateTime.Today
                            && l.Details.Any(d => d.ReturnDate == null))
                .ExecuteUpdateAsync(s => s
                    .SetProperty(l => l.RenewCount, newRenewCount)
                    .SetProperty(l => l.DueDate, newDue));

            if (affected == 0)
            {
                await tx.RollbackAsync();
                return new ServiceResult(false, "Phiếu vừa được thay đổi hoặc không còn đủ điều kiện gia hạn. Vui lòng tải lại trang.");
            }

            _db.LoanRenewals.Add(new LoanRenewal
            {
                LoanId = loanId,
                RenewedAt = DateTime.Now,
                RenewDays = cfg.RenewDays,
                RenewCount = newRenewCount,
                OutstandingCopies = outstandingCopies,
                FeeAmount = renewalFee,
                RenewedByUserId = renewedByUserId
            });
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return new ServiceResult(true, null, loanId, 0, renewalFee);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không thể gia hạn phiếu mượn {LoanId}", loanId);
            await tx.RollbackAsync();
            return new ServiceResult(false, "Không thể gia hạn do lỗi hệ thống. Vui lòng thử lại.");
        }
    }

}
