namespace ThuVien.Services;

// Các quy tắc nghiệp vụ (giả định thiết kế, có thể điều chỉnh)
public static class LibraryRules
{
    public const int LoanDays = 14;          // BR04: hạn mượn mặc định
    public const int MaxBooksPerLoan = 5;    // BR02: tối đa đầu sách mỗi phiếu
    public const int MaxBooksAtOnce = 5;     // BR02: tối đa cuốn đang mượn cùng lúc của một độc giả

    /// <summary>Late fine by calendar days. The due date remains valid through its calendar day.</summary>
    public static decimal CalculateLateFine(DateTime dueDate, DateTime returnedAt, decimal finePerDay)
    {
        if (finePerDay < 0) throw new ArgumentOutOfRangeException(nameof(finePerDay));
        var lateDays = Math.Max(0, (returnedAt.Date - dueDate.Date).Days);
        return lateDays * finePerDay;
    }

    /// <summary>Renewal fee is a recorded charge, not proof that payment was collected.</summary>
    public static decimal CalculateRenewalFee(int outstandingCopies, decimal feePerBook)
    {
        if (outstandingCopies < 0) throw new ArgumentOutOfRangeException(nameof(outstandingCopies));
        if (feePerBook < 0) throw new ArgumentOutOfRangeException(nameof(feePerBook));
        return outstandingCopies * feePerBook;
    }

}

public static class LoanStatus
{
    public const string Borrowing = "Đang mượn";
    public const string Overdue = "Quá hạn";
    public const string Returned = "Đã trả";

    // Trạng thái của một cuốn trong phiếu: suy ra từ ngày trả và hạn trả
    // Hạn trả có hiệu lực đến hết ngày ghi trên phiếu. Quá hạn từ 00:00 ngày kế tiếp.
    public static bool IsOverdue(DateTime dueDate, DateTime? today = null) =>
        dueDate.Date < (today ?? DateTime.Today).Date;

    public static string Of(DateTime? returnDate, DateTime dueDate, DateTime now)
    {
        if (returnDate != null) return Returned;
        return IsOverdue(dueDate, now.Date) ? Overdue : Borrowing;
    }

    public static string BadgeClass(string status) => status switch
    {
        Returned => "text-bg-success",
        Overdue => "text-bg-danger",
        _ => "text-bg-primary"
    };
}
