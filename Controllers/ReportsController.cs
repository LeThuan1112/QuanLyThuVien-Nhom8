using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuVien.Data;
using ThuVien.Models;
using ThuVien.Services;
using ThuVien.ViewModels;

namespace ThuVien.Controllers;

// Báo cáo, thống kê theo khoảng thời gian và xuất CSV (mở được bằng Excel)
[Authorize(Roles = AppRoles.Staff)]
public class ReportsController : Controller
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db)
    {
        _db = db;
    }

    private static (DateTime From, DateTime ToInclusive, DateTime End) Range(DateTime? from, DateTime? to)
    {
        var toDate = (to ?? DateTime.Today).Date;
        var fromDate = (from ?? toDate.AddDays(-29)).Date;
        if (fromDate > toDate) (fromDate, toDate) = (toDate, fromDate);
        return (fromDate, toDate, toDate.AddDays(1));
    }

    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        var (fromDate, toDate, end) = Range(from, to);
        var vm = new ReportViewModel { From = fromDate, To = toDate };

        vm.LoanCount = await _db.Loans.CountAsync(l => l.LoanDate >= fromDate && l.LoanDate < end);
        vm.BorrowedCopies = await _db.LoanDetails.CountAsync(d => d.Loan!.LoanDate >= fromDate && d.Loan.LoanDate < end);
        vm.ReturnedCopies = await _db.LoanDetails.CountAsync(d => d.ReturnDate != null && d.ReturnDate >= fromDate && d.ReturnDate < end);
        vm.NewReaders = await _db.Readers.CountAsync(r => r.CardIssuedDate >= fromDate && r.CardIssuedDate < end);
        vm.OverdueNow = await _db.LoanDetails.CountAsync(d => d.ReturnDate == null && d.Loan!.DueDate < DateTime.Today);
        vm.TotalFines = await _db.LoanDetails
            .Where(d => d.ReturnDate != null && d.ReturnDate >= fromDate && d.ReturnDate < end)
            .SumAsync(d => (decimal?)d.FineAmount) ?? 0;
        vm.TotalRenewalFees = await _db.LoanRenewals
            .Where(r => r.RenewedAt >= fromDate && r.RenewedAt < end)
            .SumAsync(r => (decimal?)r.FeeAmount) ?? 0;

        var topBooks = await _db.LoanDetails.AsNoTracking()
            .Where(d => d.Loan!.LoanDate >= fromDate && d.Loan.LoanDate < end)
            .GroupBy(d => d.BookId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();
        var bookIds = topBooks.Select(t => t.Id).ToList();
        var bookTitles = await _db.Books.AsNoTracking().Where(b => bookIds.Contains(b.BookId))
            .ToDictionaryAsync(b => b.BookId, b => b.Title);
        vm.TopBooks = topBooks.Select(t => new TopBookItem
        {
            Title = bookTitles.TryGetValue(t.Id, out var title) ? title : "?",
            Count = t.Count
        }).ToList();

        var topReaders = await _db.LoanDetails.AsNoTracking()
            .Where(d => d.Loan!.LoanDate >= fromDate && d.Loan.LoanDate < end)
            .GroupBy(d => d.Loan!.ReaderId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();
        var readerIds = topReaders.Select(t => t.Id).ToList();
        var readerNames = await _db.Readers.AsNoTracking().Where(r => readerIds.Contains(r.ReaderId))
            .ToDictionaryAsync(r => r.ReaderId, r => r.ReaderCode + " - " + r.FullName);
        vm.TopReaders = topReaders.Select(t => new TopBookItem
        {
            Title = readerNames.TryGetValue(t.Id, out var name) ? name : "?",
            Count = t.Count
        }).ToList();

        vm.Loans = await _db.Loans.AsNoTracking()
            .Include(l => l.Reader)
            .Include(l => l.Details)
            .Where(l => l.LoanDate >= fromDate && l.LoanDate < end)
            .OrderByDescending(l => l.LoanDate)
            .Take(50)
            .ToListAsync();

        return View(vm);
    }

    // Xuất danh sách mượn/trả trong khoảng thời gian
    public async Task<IActionResult> ExportLoans(DateTime? from, DateTime? to)
    {
        var (fromDate, toDate, end) = Range(from, to);
        var now = DateTime.Now;

        var rows = await _db.LoanDetails.AsNoTracking()
            .Include(d => d.Book)
            .Include(d => d.Loan).ThenInclude(l => l!.Reader)
            .Include(d => d.Loan).ThenInclude(l => l!.Renewals)
            .Where(d => d.Loan!.LoanDate >= fromDate && d.Loan.LoanDate < end)
            .OrderBy(d => d.Loan!.LoanDate).ThenBy(d => d.LoanId)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Mã phiếu,Mã độc giả,Họ tên độc giả,Ngày mượn,Hạn trả,Mã sách,Tên sách,Ngày trả,Trạng thái,Phí phạt quá hạn,Tổng phí gia hạn cộng dồn phiếu");
        var renewalFeeWritten = new HashSet<int>();
        foreach (var d in rows)
        {
            var loan = d.Loan!;
            var renewalFee = renewalFeeWritten.Add(loan.LoanId) ? loan.Renewals.Sum(r => r.FeeAmount).ToString("0") : "";
            sb.AppendLine(string.Join(",",
                Csv(loan.LoanId.ToString()),
                Csv(loan.Reader?.ReaderCode),
                Csv(loan.Reader?.FullName),
                Csv(loan.LoanDate.ToString("dd/MM/yyyy HH:mm")),
                Csv(loan.DueDate.ToString("dd/MM/yyyy")),
                Csv(d.Book?.BookCode),
                Csv(d.Book?.Title),
                Csv(d.ReturnDate?.ToString("dd/MM/yyyy HH:mm")),
                Csv(LoanStatus.Of(d.ReturnDate, loan.DueDate, now)),
                Csv(d.FineAmount.ToString("0")),
                Csv(renewalFee)));
        }

        return CsvFile(sb.ToString(), $"phieu-muon_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.csv");
    }

    // Xuất riêng các lần gia hạn theo ngày phát sinh để phí không bị gán nhầm vào ngày mượn.
    public async Task<IActionResult> ExportRenewals(DateTime? from, DateTime? to)
    {
        var (fromDate, toDate, end) = Range(from, to);
        var rows = await _db.LoanRenewals.AsNoTracking()
            .Include(r => r.Loan).ThenInclude(l => l!.Reader)
            .Include(r => r.RenewedByUser)
            .Where(r => r.RenewedAt >= fromDate && r.RenewedAt < end)
            .OrderBy(r => r.RenewedAt)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Mã gia hạn,Mã phiếu,Mã độc giả,Họ tên độc giả,Ngày gia hạn,Lượt gia hạn,Số ngày cộng thêm,Số cuốn còn mượn,Người thực hiện,Phí phát sinh");
        foreach (var r in rows)
        {
            sb.AppendLine(string.Join(",",
                Csv(r.LoanRenewalId.ToString()),
                Csv(r.LoanId.ToString()),
                Csv(r.Loan?.Reader?.ReaderCode),
                Csv(r.Loan?.Reader?.FullName),
                Csv(r.RenewedAt.ToString("dd/MM/yyyy HH:mm")),
                Csv(r.RenewCount.ToString()),
                Csv(r.RenewDays.ToString()),
                Csv(r.OutstandingCopies.ToString()),
                Csv(r.RenewedByUser?.FullName),
                Csv(r.FeeAmount.ToString("0"))));
        }

        return CsvFile(sb.ToString(), $"lich-su-gia-han_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.csv");
    }

    // Xuất danh sách sách
    public async Task<IActionResult> ExportBooks()
    {
        var books = await _db.Books.AsNoTracking()
            .Include(b => b.Category)
            .OrderBy(b => b.BookCode)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Mã sách,Tên sách,Tác giả,Thể loại,Nhà xuất bản,Năm xuất bản,Tổng số bản,Còn lại,Vị trí kệ,Tình trạng");
        foreach (var b in books)
        {
            sb.AppendLine(string.Join(",",
                Csv(b.BookCode),
                Csv(b.Title),
                Csv(b.Author),
                Csv(b.Category?.Name),
                Csv(b.Publisher),
                Csv(b.PublishYear?.ToString()),
                Csv(b.TotalCopies.ToString()),
                Csv(b.AvailableCopies.ToString()),
                Csv(b.ShelfLocation),
                Csv(b.IsActive ? "Đang lưu hành" : "Ngừng lưu hành")));
        }

        return CsvFile(sb.ToString(), $"danh-sach-sach_{DateTime.Now:yyyyMMdd}.csv");
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        // Chống CSV injection: ô bắt đầu bằng = + - @ sẽ bị Excel hiểu là công thức
        if (value[0] is '=' or '+' or '-' or '@')
        {
            value = "'" + value;
        }
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private FileContentResult CsvFile(string content, string fileName)
    {
        // Thêm BOM UTF-8 để Excel đọc đúng tiếng Việt
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(content)).ToArray();
        return File(bytes, "text/csv; charset=utf-8", fileName);
    }
}
