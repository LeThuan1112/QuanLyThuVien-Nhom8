using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThuVien.Data;
using ThuVien.Models;
using ThuVien.ViewModels;

namespace ThuVien.Controllers;

// Nhật ký hoạt động: chỉ Admin được xem
[Authorize(Roles = AppRoles.Admin)]
public class AuditLogsController : Controller
{
    private const int PageSize = 15;
    private readonly AppDbContext _db;

    public AuditLogsController(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IActionResult> Index(string? keyword, string? status, DateTime? from, DateTime? to, int page = 1)
    {
        var query = _db.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var k = keyword.Trim();
            query = query.Where(l => (l.Username != null && l.Username.Contains(k))
                                     || l.Action.Contains(k)
                                     || (l.Detail != null && l.Detail.Contains(k)));
        }
        if (status == "ok") query = query.Where(l => l.Success);
        else if (status == "fail") query = query.Where(l => !l.Success);

        if (from.HasValue)
        {
            var f = from.Value.Date;
            query = query.Where(l => l.CreatedAt >= f);
        }
        if (to.HasValue)
        {
            var end = to.Value.Date.AddDays(1);
            query = query.Where(l => l.CreatedAt < end);
        }

        var ordered = query.OrderByDescending(l => l.CreatedAt).ThenByDescending(l => l.AuditLogId);
        var total = await ordered.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var logs = await ordered.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync();

        return View(new AuditListViewModel
        {
            Logs = logs,
            Keyword = keyword,
            Status = status,
            From = from,
            To = to,
            Page = page,
            PageSize = PageSize,
            TotalItems = total
        });
    }
}
