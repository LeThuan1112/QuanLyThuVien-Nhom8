using System.ComponentModel.DataAnnotations;

namespace ThuVien.Models;

public class Reader
{
    public int ReaderId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã độc giả")]
    [StringLength(20)]
    [Display(Name = "Mã độc giả")]
    public string ReaderCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(100)]
    [Display(Name = "Họ tên")]
    public string FullName { get; set; } = string.Empty;

    [StringLength(20)]
    [RegularExpression(@"^[0-9+ ]{8,15}$", ErrorMessage = "Số điện thoại không hợp lệ")]
    [Display(Name = "Điện thoại")]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [StringLength(100)]
    public string? Email { get; set; }

    [StringLength(255)]
    [Display(Name = "Địa chỉ")]
    public string? Address { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Ngày cấp thẻ")]
    public DateTime CardIssuedDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Ngày hết hạn thẻ")]
    public DateTime CardExpiryDate { get; set; } = DateTime.Today.AddYears(1);

    // true = thẻ còn hiệu lực, false = bị khóa
    [Display(Name = "Thẻ còn hiệu lực")]
    public bool IsActive { get; set; } = true;

    // Tài khoản đăng nhập (có thể không có)
    public int? UserId { get; set; }
    public User? User { get; set; }
}
