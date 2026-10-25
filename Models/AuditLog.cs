using System.ComponentModel.DataAnnotations;

namespace ThuVien.Models;

// Nhật ký hoạt động: ghi lại các thao tác thay đổi dữ liệu và đăng nhập
public class AuditLog
{
    public int AuditLogId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public int? UserId { get; set; }

    [StringLength(50)]
    public string? Username { get; set; }

    [StringLength(20)]
    public string? Role { get; set; }

    [Required, StringLength(100)]
    public string Action { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Detail { get; set; }

    public bool Success { get; set; } = true;

    [StringLength(45)]
    public string? IpAddress { get; set; }
}
