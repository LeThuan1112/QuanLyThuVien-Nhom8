using ThuVien.Models;

namespace ThuVien.ViewModels;

public class PagerViewModel
{
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public string Action { get; set; } = "Index";
    public string Controller { get; set; } = string.Empty;
    public Dictionary<string, object?> RouteValues { get; set; } = new();
}

public class LoanListViewModel
{
    public List<Loan> Loans { get; set; } = new();
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
}

public class ReaderListViewModel
{
    public List<Reader> Readers { get; set; } = new();
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
}

public class ReaderDetailsViewModel
{
    public Reader Reader { get; set; } = new();
    public List<Loan> Loans { get; set; } = new();
}

public class MyLoansViewModel
{
    public Reader? Reader { get; set; }
    public List<LoanDetail> Items { get; set; } = new();
}
