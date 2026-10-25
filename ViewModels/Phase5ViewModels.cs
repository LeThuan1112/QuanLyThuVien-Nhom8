using System.ComponentModel.DataAnnotations;
using ThuVien.Models;

namespace ThuVien.ViewModels;

public static class RoleDisplay
{
    public static string Name(string role) => role switch
    {
        AppRoles.Admin => "Quản trị viên",
        AppRoles.Librarian => "Thủ thư",
        AppRoles.Reader => "Độc giả",
        _ => role
    };

    public static string Badge(string role) => role switch
    {
        AppRoles.Admin => "text-bg-danger",
        AppRoles.Librarian => "text-bg-primary",
        _ => "text-bg-secondary"
    };
}

public class TopBookItem
{
    public string Title { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class CategoryChip
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class HomeViewModel
{
    public int BookTitles { get; set; }
    public int Readers { get; set; }
    public int AvailableCopies { get; set; }
    public List<CategoryChip> Categories { get; set; } = new();
    public List<Book> LatestBooks { get; set; } = new();
    public List<Book> PopularBooks { get; set; } = new();
}

public class ReaderHomeViewModel
{
    public Reader? Reader { get; set; }
    public int TotalBorrowed { get; set; }
    public int DueSoon { get; set; }
    public int Overdue { get; set; }
    public List<LoanDetail> Current { get; set; } = new();
}

public class UserListViewModel
{
    public List<User> Users { get; set; } = new();
    public Dictionary<int, string> ReaderCodes { get; set; } = new();
    public string? Keyword { get; set; }
    public string? Role { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public int TotalItems { get; set; }
    public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
}

public class UserCreateViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự")]
    [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Chỉ dùng chữ, số, dấu chấm, gạch dưới, gạch ngang")]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(100)]
    [Display(Name = "Họ tên")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(100)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn vai trò")]
    [Display(Name = "Vai trò")]
    public string Role { get; set; } = AppRoles.Reader;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Mật khẩu phải có cả chữ và số")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp")]
    [DataType(DataType.Password)]
    [Display(Name = "Nhập lại mật khẩu")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Gắn với hồ sơ độc giả")]
    public int? ReaderId { get; set; }
}

public class UserEditViewModel
{
    public int UserId { get; set; }

    public string? Username { get; set; }

    [Display(Name = "Hồ sơ độc giả")]
    public int? ReaderId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(100)]
    [Display(Name = "Họ tên")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(100)]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn vai trò")]
    [Display(Name = "Vai trò")]
    public string Role { get; set; } = AppRoles.Reader;

    [Display(Name = "Tài khoản đang hoạt động")]
    public bool IsActive { get; set; } = true;
}

public class ResetPasswordViewModel
{
    public int UserId { get; set; }
    public string? Username { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Mật khẩu phải có cả chữ và số")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu nhập lại không khớp")]
    [DataType(DataType.Password)]
    [Display(Name = "Nhập lại mật khẩu mới")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu hiện tại")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
    [RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d).+$", ErrorMessage = "Mật khẩu phải có cả chữ và số")]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu mới")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu nhập lại không khớp")]
    [DataType(DataType.Password)]
    [Display(Name = "Nhập lại mật khẩu mới")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ReportViewModel
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int LoanCount { get; set; }
    public int BorrowedCopies { get; set; }
    public int ReturnedCopies { get; set; }
    public int NewReaders { get; set; }
    public int OverdueNow { get; set; }
    public decimal TotalFines { get; set; }
    public decimal TotalRenewalFees { get; set; }
    public List<TopBookItem> TopBooks { get; set; } = new();
    public List<TopBookItem> TopReaders { get; set; } = new();
    public List<Loan> Loans { get; set; } = new();
}
