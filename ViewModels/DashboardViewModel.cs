using ThuVien.Models;

namespace ThuVien.ViewModels;

public class DashboardViewModel
{
    public int BookTitles { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public int Readers { get; set; }
    public int Borrowing { get; set; }
    public int Overdue { get; set; }

    public List<string> DayLabels { get; set; } = new();
    public List<int> BorrowedPerDay { get; set; } = new();
    public List<int> ReturnedPerDay { get; set; } = new();

    public List<string> CategoryNames { get; set; } = new();
    public List<int> CategoryCounts { get; set; } = new();

    public List<TopBookItem> TopBooks { get; set; } = new();
    public List<Loan> RecentLoans { get; set; } = new();
    public List<LoanDetail> OverdueItems { get; set; } = new();

    // Chỉ dành cho Admin
    public int TotalUsers { get; set; }
    public int AdminCount { get; set; }
    public int LibrarianCount { get; set; }
    public int ReaderAccountCount { get; set; }
    public int LockedUsers { get; set; }
    public List<AuditLog> RecentLogs { get; set; } = new();

    // Chỉ dành cho Thủ thư
    public List<LoanDetail> DueToday { get; set; } = new();
    public List<Book> OutOfStock { get; set; } = new();
}
