using System.ComponentModel.DataAnnotations;

namespace ThuVien.Models;

public class User
{
    public int UserId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
    [StringLength(50)]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(256)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(100)]
    [Display(Name = "Họ tên")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(100)]
    public string? Email { get; set; }

    // Admin / Librarian / Reader
    [Required, StringLength(20)]
    public string Role { get; set; } = AppRoles.Reader;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
