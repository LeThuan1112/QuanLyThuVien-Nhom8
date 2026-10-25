using ThuVien.Models;

namespace ThuVien.ViewModels;

public class AuditListViewModel
{
    public List<AuditLog> Logs { get; set; } = new();
    public string? Keyword { get; set; }
    public string? Status { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 15;
    public int TotalItems { get; set; }
    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
}
