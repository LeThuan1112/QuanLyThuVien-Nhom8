using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ThuVien.Data;
using ThuVien.Models;

namespace ThuVien.Services;

// Bộ lọc toàn cục: ghi nhật ký cho mọi request POST (đăng nhập, thêm/sửa/xóa, mượn/trả...).
// Không ghi mật khẩu hay dữ liệu form; chỉ ghi người thực hiện, hành động, kết quả, IP.
public class AuditFilter : IAsyncActionFilter
{
    public const string LoginAction = "Đăng nhập";

    private static readonly Dictionary<string, string> Names = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Account/Login"] = LoginAction,
        ["Account/Logout"] = "Đăng xuất",
        ["Account/ChangePassword"] = "Đổi mật khẩu",
        ["Books/Create"] = "Thêm sách",
        ["Books/Edit"] = "Sửa sách",
        ["Books/Delete"] = "Xóa sách",
        ["Categories/Create"] = "Thêm thể loại",
        ["Categories/Edit"] = "Sửa thể loại",
        ["Categories/Delete"] = "Xóa thể loại",
        ["Readers/Create"] = "Thêm độc giả",
        ["Readers/Edit"] = "Sửa độc giả",
        ["Readers/ToggleStatus"] = "Khóa/mở thẻ độc giả",
        ["Readers/Delete"] = "Xóa độc giả",
        ["Loans/Create"] = "Lập phiếu mượn",
        ["Loans/Return"] = "Trả sách",
        ["Loans/ReturnAll"] = "Trả tất cả sách trong phiếu",
        ["Loans/Renew"] = "Gia hạn phiếu mượn",
        ["Users/Create"] = "Tạo tài khoản",
        ["Users/Edit"] = "Sửa tài khoản",
        ["Users/ToggleActive"] = "Khóa/mở tài khoản",
        ["Users/ResetPassword"] = "Đặt lại mật khẩu",
        ["Settings/Index"] = "Cập nhật cài đặt hệ thống",
        ["BooksApi/Create"] = "API: thêm sách"
    };

    private readonly AppDbContext _db;

    public AuditFilter(AppDbContext db)
    {
        _db = db;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var executed = await next();

        var http = context.HttpContext;
        if (!HttpMethods.IsPost(http.Request.Method)) return;
        if (executed.Exception != null) return;

        var controller = context.RouteData.Values["controller"]?.ToString() ?? "";
        var action = context.RouteData.Values["action"]?.ToString() ?? "";
        if (controller.Equals("Culture", StringComparison.OrdinalIgnoreCase)) return;

        var key = controller + "/" + action;
        var isLogin = key.Equals("Account/Login", StringComparison.OrdinalIgnoreCase);
        var result = executed.Result;

        // Form không hợp lệ (trả lại View) không ghi nhật ký, trừ đăng nhập
        if (result is ViewResult && !isLogin) return;

        var isRedirect = result is RedirectToActionResult or RedirectResult or LocalRedirectResult or RedirectToRouteResult;
        var isApiOk = result is ObjectResult o && (o.StatusCode ?? 200) < 400;

        string? tempOk = null;
        string? tempError = null;
        if (context.Controller is Controller c)
        {
            tempOk = c.TempData.Peek("Success") as string;
            tempError = c.TempData.Peek("Error") as string;
        }

        var success = (isRedirect || isApiOk) && tempError == null;
        var detail = tempError ?? tempOk;
        if (detail == null && context.RouteData.Values["id"] != null)
        {
            detail = "Mã: " + context.RouteData.Values["id"];
        }

        int? userId = null;
        string? username = null;
        string? role = null;
        var principal = http.User;
        if (principal.Identity?.IsAuthenticated == true)
        {
            if (int.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id)) userId = id;
            username = principal.Identity.Name;
            role = principal.FindFirstValue(ClaimTypes.Role);
        }

        if (isLogin)
        {
            username = http.Request.HasFormContentType ? http.Request.Form["Username"].ToString() : null;
            detail = success ? "Đăng nhập thành công" : "Đăng nhập thất bại";
        }

        try
        {
            _db.AuditLogs.Add(new AuditLog
            {
                CreatedAt = DateTime.Now,
                UserId = userId,
                Username = Trunc(username, 50),
                Role = Trunc(role, 20),
                Action = Trunc(Names.TryGetValue(key, out var display) ? display : key, 100)!,
                Detail = Trunc(detail, 500),
                Success = success,
                IpAddress = Trunc(http.Connection.RemoteIpAddress?.ToString(), 45)
            });
            await _db.SaveChangesAsync();
        }
        catch
        {
            // Lỗi ghi nhật ký không được làm hỏng nghiệp vụ chính
        }
    }

    private static string? Trunc(string? s, int max)
    {
        if (s == null) return null;
        return s.Length <= max ? s : s.Substring(0, max);
    }
}
